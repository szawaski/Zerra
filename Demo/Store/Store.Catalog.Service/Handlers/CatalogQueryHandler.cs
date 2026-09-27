using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Catalog.Service.Data;
using Store.Common.Data;
using Store.Common.Messaging;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using Zerra;
using Zerra.Repository;

namespace Store.Catalog.Service.Handlers
{
    public sealed class CatalogQueryHandler : BaseHandlerWithRepo, ICatalogQueryHandler
    {
        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync<CategoryDataModel>(QueryOrder<CategoryDataModel>.Create(x => x.Name));
            return items.Select(x => new CategoryModel() { ID = x.ID, Name = x.Name }).ToArray();
        }

        public async Task<ProductModel[]> GetProducts(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(QueryOrder<ProductDataModel>.Create(x => x.Name), WithCategory());
            return items.Select(ToModel).ToArray();
        }

        public async Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(x => x.CategoryID == categoryID, QueryOrder<ProductDataModel>.Create(x => x.Name), WithCategory());
            return items.Select(ToModel).ToArray();
        }

        public async Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken)
        {
            if (productIDs.Length == 0)
                return [];
            var items = await Repo.ManyAsync(x => productIDs.Contains(x.ID), WithCategory());
            return items.Select(ToModel).ToArray();
        }

        public async Task<Stream> ExportProductsCsv(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(QueryOrder<ProductDataModel>.Create(x => x.Name), WithCategory());

            //the CSV is written as the caller reads it, the pipe only holds what hasn't been read yet
            var pipe = new Pipe();
            _ = WriteProductsCsv(items, pipe.Writer);
            return pipe.Reader.AsStream();
        }

        private static async Task WriteProductsCsv(IEnumerable<ProductDataModel> items, PipeWriter pipeWriter)
        {
            try
            {
                await using (var writer = new StreamWriter(pipeWriter.AsStream(true), new UTF8Encoding(false)) { NewLine = "\r\n" })
                {
                    await writer.WriteLineAsync("Sku,Name,Category,Price,Status");
                    foreach (var item in items)
                        await writer.WriteLineAsync($"{CsvField(item.Sku)},{CsvField(item.Name)},{CsvField(item.Category?.Name)},{item.Price.ToString(CultureInfo.InvariantCulture)},{item.Status}");
                }
                await pipeWriter.CompleteAsync();
            }
            catch (Exception ex)
            {
                //the reader gets the error instead of a CSV that looks complete
                await pipeWriter.CompleteAsync(ex);
            }
        }

        public async Task<ProductImportPreviewModel> PreviewProductImport(string fileName, Stream csv, decimal maxPriceChangePercent, CancellationToken cancellationToken)
        {
            const int maxRows = 500;

            var existing = (await Repo.ManyAsync<ProductDataModel>()).Where(x => x.Sku is not null).ToDictionary(x => x.Sku!, StringComparer.OrdinalIgnoreCase);

            //reads the upload as it arrives, a query only looks, importing would be commands
            var preview = new ProductImportPreviewModel() { FileName = fileName };
            var rows = new List<ProductImportRowModel>();
            using var reader = new StreamReader(csv);
            var line = 0;
            string? text;
            while ((text = await reader.ReadLineAsync(cancellationToken)) is not null)
            {
                line++;
                if (String.IsNullOrWhiteSpace(text) || (line == 1 && text.StartsWith("Sku,", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fields = ParseCsvLine(text);
                var row = new ProductImportRowModel() { Line = line, Sku = fields.ElementAtOrDefault(0)?.Trim(), Name = fields.ElementAtOrDefault(1)?.Trim() };
                if (String.IsNullOrWhiteSpace(row.Sku) || String.IsNullOrWhiteSpace(row.Name))
                {
                    row.Change = ProductImportChange.Error;
                    row.Error = "A SKU and a name are required";
                }
                else if (!Decimal.TryParse(fields.ElementAtOrDefault(3), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
                {
                    row.Change = ProductImportChange.Error;
                    row.Error = "The price must be a number above zero";
                }
                else
                {
                    row.Price = price;
                    if (!existing.TryGetValue(row.Sku, out var product))
                    {
                        row.Change = ProductImportChange.New;
                    }
                    else
                    {
                        row.CurrentPrice = product.Price;
                        var changePercent = Math.Abs(price - product.Price) / product.Price * 100;
                        if (changePercent > maxPriceChangePercent)
                        {
                            row.Change = ProductImportChange.Error;
                            row.Error = String.Create(CultureInfo.InvariantCulture, $"The price changes {changePercent:0.#}%, more than {maxPriceChangePercent:0.#}%");
                        }
                        else
                        {
                            row.Change = product.Price == price ? ProductImportChange.Unchanged : ProductImportChange.PriceChange;
                        }
                    }
                }

                switch (row.Change)
                {
                    case ProductImportChange.New: preview.NewCount++; break;
                    case ProductImportChange.PriceChange: preview.PriceChangeCount++; break;
                    case ProductImportChange.Unchanged: preview.UnchangedCount++; break;
                    case ProductImportChange.Error: preview.ErrorCount++; break;
                }
                if (rows.Count < maxRows)
                    rows.Add(row);
            }

            preview.Rows = rows.ToArray();
            return preview;
        }

        private static string CsvField(string? value)
        {
            if (value is null)
                return String.Empty;
            if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
                return value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static List<string> ParseCsvLine(string text)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        _ = field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        _ = field.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    _ = field.Clear();
                }
                else
                {
                    _ = field.Append(c);
                }
            }
            fields.Add(field.ToString());
            return fields;
        }

        //all product columns plus the related category for its name
        private static Graph<ProductDataModel> WithCategory() => new(true, x => x.Category);

        private static ProductModel ToModel(ProductDataModel item) => new()
        {
            ID = item.ID,
            CategoryID = item.CategoryID,
            CategoryName = item.Category?.Name,
            Sku = item.Sku,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            IsActive = item.Status == nameof(ProductStatus.Active)
        };
    }
}
