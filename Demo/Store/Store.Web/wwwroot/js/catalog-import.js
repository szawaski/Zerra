$(function () {

    const changes = {
        New: ["New", "success"],
        PriceChange: ["Price change", "warning"],
        Unchanged: ["Unchanged", "neutral"],
        Error: ["Error", "danger"]
    };

    //a Stream result arrives as a Blob
    $("#export-csv").on("click", function () {
        Store.busy(this, Store.query(ICatalogQueryHandler.ExportProductsCsv))
            .then(function (blob) {
                const url = URL.createObjectURL(blob);
                $("<a>").attr({ href: url, download: "products.csv" })[0].click();
                URL.revokeObjectURL(url);
            })
            .catch(function () { });
    });

    $("#import-file").on("change", function () {
        $("#preview-import").prop("disabled", this.files.length === 0);
    });

    $("#preview-import").on("click", function () {
        //a File is a Blob, Bus.js uploads it as the query's Stream argument
        const file = $("#import-file")[0].files[0];
        const maxPriceChangePercent = parseFloat($("#max-price-change").val()) || 0;
        Store.busy(this, Store.query(ICatalogQueryHandler.PreviewProductImport, file.name, file, maxPriceChangePercent))
            .then(function (preview) {
                $("#import-summary").text(preview.FileName + ": " + preview.NewCount + " new, " + preview.PriceChangeCount + " price changes, " + preview.UnchangedCount + " unchanged, " + preview.ErrorCount + " errors");
                const $rows = $("#import-rows").empty();
                for (const row of preview.Rows) {
                    const change = changes[row.Change];
                    $("<tr>").append(
                        $("<td>").addClass("num").text(row.Line),
                        $("<td>").append($("<code>").text(row.Sku || "")),
                        $("<td>").append($("<div>").text(row.Name || ""), $("<div>").addClass("subtle").text(row.Error || "")),
                        $("<td>").addClass("num").text(row.CurrentPrice === null ? "" : Store.money(row.CurrentPrice)),
                        $("<td>").addClass("num").text(row.Price === null ? "" : Store.money(row.Price)),
                        $("<td>").append(Store.badge(change[0], change[1]))
                    ).appendTo($rows);
                }
                $("#import-table").prop("hidden", preview.Rows.length === 0);
            })
            .catch(function () { });
    });
});
