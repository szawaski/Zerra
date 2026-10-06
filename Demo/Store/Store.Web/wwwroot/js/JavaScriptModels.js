const CartModelType =
{
    CustomerID: "string",
    Items: "CartItemModel[]",
    ItemCount: "number",
    Total: "number",
    LastEventNumber: "number",
    UpdatedOn: "Date",
    LastOrderNumber: "string",
}

const CartItemModelType =
{
    ProductID: "string",
    ProductName: "string",
    UnitPrice: "number",
    Quantity: "number",
    Total: "number",
}

const CartHistoryModelType =
{
    EventNumber: "number",
    EventName: "string",
    OccurredOn: "Date",
    ItemCount: "number",
    Total: "number",
}

const CategoryModelType =
{
    ID: "string",
    Name: "string",
}

const ProductModelType =
{
    ID: "string",
    CategoryID: "string",
    CategoryName: "string",
    Sku: "string",
    Name: "string",
    Description: "string",
    Price: "number",
    IsActive: "boolean",
}

const ProductImportPreviewModelType =
{
    FileName: "string",
    NewCount: "number",
    PriceChangeCount: "number",
    UnchangedCount: "number",
    ErrorCount: "number",
    Rows: "ProductImportRowModel[]",
}

const ProductImportRowModelType =
{
    Line: "number",
    Sku: "string",
    Name: "string",
    Price: "number",
    CurrentPrice: "number",
    Change: "string",
    Error: "string",
}

const StockLevelModelType =
{
    ProductID: "string",
    OnHand: "number",
    Reserved: "number",
    Available: "number",
}

const StockMovementModelType =
{
    ProductID: "string",
    Kind: "string",
    Quantity: "number",
    OrderNumber: "string",
    OccurredOn: "Date",
}

const CustomerModelType =
{
    ID: "string",
    Name: "string",
    Email: "string",
}

const OrderModelType =
{
    ID: "string",
    OrderNumber: "string",
    CustomerID: "string",
    CustomerName: "string",
    PlacedOn: "Date",
    Status: "string",
    Total: "number",
    Items: "OrderItemModel[]",
}

const OrderItemModelType =
{
    ProductID: "string",
    ProductName: "string",
    UnitPrice: "number",
    Quantity: "number",
    Total: "number",
}

const ReviewModelType =
{
    ID: "string",
    ProductID: "string",
    ProductName: "string",
    CustomerID: "string",
    CustomerName: "string",
    Rating: "number",
    Comment: "string",
    VerifiedPurchase: "boolean",
    CreatedOn: "Date",
}

const ProductRatingModelType =
{
    ProductID: "string",
    AverageRating: "number",
    ReviewCount: "number",
}

const ShipmentModelType =
{
    OrderID: "string",
    OrderNumber: "string",
    Carrier: "string",
    TrackingNumber: "string",
    Status: "string",
    ShippedOn: "Date",
    DeliveredOn: "Date",
}

const CheckoutCartResultType =
{
    OrderID: "string",
    OrderNumber: "string",
    Total: "number",
}

const AddProductResultType =
{
    ProductID: "string",
}

const OrderItemRequestType =
{
    ProductID: "string",
    Quantity: "number",
}

const PlaceOrderResultType =
{
    OrderID: "string",
    OrderNumber: "string",
    Total: "number",
}

const SubmitReviewResultType =
{
    ReviewID: "string",
    VerifiedPurchase: "boolean",
}

const ModelTypeDictionary =
{
    CartModel: CartModelType,
    CartItemModel: CartItemModelType,
    CartHistoryModel: CartHistoryModelType,
    CategoryModel: CategoryModelType,
    ProductModel: ProductModelType,
    ProductImportPreviewModel: ProductImportPreviewModelType,
    ProductImportRowModel: ProductImportRowModelType,
    StockLevelModel: StockLevelModelType,
    StockMovementModel: StockMovementModelType,
    CustomerModel: CustomerModelType,
    OrderModel: OrderModelType,
    OrderItemModel: OrderItemModelType,
    ReviewModel: ReviewModelType,
    ProductRatingModel: ProductRatingModelType,
    ShipmentModel: ShipmentModelType,
    CheckoutCartResult: CheckoutCartResultType,
    AddProductResult: AddProductResultType,
    OrderItemRequest: OrderItemRequestType,
    PlaceOrderResult: PlaceOrderResultType,
    SubmitReviewResult: SubmitReviewResultType,
}

const ICartsQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("ICartsQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("ICartsQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetCart: function(customerID, onComplete, onFail) {
        Bus.Call("ICartsQueryHandler", "GetCart", [customerID, null], CartModelType, false, onComplete, onFail);
    },
    GetCartHistory: function(customerID, onComplete, onFail) {
        Bus.Call("ICartsQueryHandler", "GetCartHistory", [customerID, null], CartHistoryModelType, true, onComplete, onFail);
    },
}

const ICatalogQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetCategories: function(onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "GetCategories", [null], CategoryModelType, true, onComplete, onFail);
    },
    GetProducts: function(onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "GetProducts", [null], ProductModelType, true, onComplete, onFail);
    },
    GetProductsByCategory: function(categoryID, onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "GetProductsByCategory", [categoryID, null], ProductModelType, true, onComplete, onFail);
    },
    GetProductsByIDs: function(productIDs, onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "GetProductsByIDs", [productIDs, null], ProductModelType, true, onComplete, onFail);
    },
    ExportProductsCsv: function(onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "ExportProductsCsv", [null], "Blob", false, onComplete, onFail);
    },
    PreviewProductImport: function(fileName, csv, maxPriceChangePercent, onComplete, onFail) {
        Bus.Call("ICatalogQueryHandler", "PreviewProductImport", [fileName, csv, maxPriceChangePercent, null], ProductImportPreviewModelType, false, onComplete, onFail);
    },
}

const IInventoryQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("IInventoryQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("IInventoryQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetStockLevels: function(onComplete, onFail) {
        Bus.Call("IInventoryQueryHandler", "GetStockLevels", [null], StockLevelModelType, true, onComplete, onFail);
    },
    GetRecentMovements: function(count, onComplete, onFail) {
        Bus.Call("IInventoryQueryHandler", "GetRecentMovements", [count, null], StockMovementModelType, true, onComplete, onFail);
    },
}

const IOrdersQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("IOrdersQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("IOrdersQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetCustomers: function(onComplete, onFail) {
        Bus.Call("IOrdersQueryHandler", "GetCustomers", [null], CustomerModelType, true, onComplete, onFail);
    },
    GetOrders: function(onComplete, onFail) {
        Bus.Call("IOrdersQueryHandler", "GetOrders", [null], OrderModelType, true, onComplete, onFail);
    },
    GetOrder: function(orderID, onComplete, onFail) {
        Bus.Call("IOrdersQueryHandler", "GetOrder", [orderID, null], OrderModelType, false, onComplete, onFail);
    },
    HasPurchased: function(customerID, productID, onComplete, onFail) {
        Bus.Call("IOrdersQueryHandler", "HasPurchased", [customerID, productID, null], null, false, onComplete, onFail);
    },
}

const IReviewsQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("IReviewsQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("IReviewsQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetRecentReviews: function(count, onComplete, onFail) {
        Bus.Call("IReviewsQueryHandler", "GetRecentReviews", [count, null], ReviewModelType, true, onComplete, onFail);
    },
    GetReviewsForProduct: function(productID, onComplete, onFail) {
        Bus.Call("IReviewsQueryHandler", "GetReviewsForProduct", [productID, null], ReviewModelType, true, onComplete, onFail);
    },
    GetProductRatings: function(onComplete, onFail) {
        Bus.Call("IReviewsQueryHandler", "GetProductRatings", [null], ProductRatingModelType, true, onComplete, onFail);
    },
}

const IShippingQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("IShippingQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("IShippingQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetShipments: function(onComplete, onFail) {
        Bus.Call("IShippingQueryHandler", "GetShipments", [null], ShipmentModelType, true, onComplete, onFail);
    },
}

const AddToCartCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Quantity = (properties === undefined || properties.Quantity === undefined) ? null : properties.Quantity;
    this.CommandType = "AddToCartCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const CheckoutCartCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.CommandType = "CheckoutCartCommand";
    this.CommandWithResult = true;
    this.ResultType = CheckoutCartResultType;
    this.ResultTypeHasMany = false;
}

const EmptyCartCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.CommandType = "EmptyCartCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const RemoveFromCartCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.CommandType = "RemoveFromCartCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const AddProductCommand = function(properties) {
    this.CategoryID = (properties === undefined || properties.CategoryID === undefined) ? null : properties.CategoryID;
    this.Sku = (properties === undefined || properties.Sku === undefined) ? null : properties.Sku;
    this.Name = (properties === undefined || properties.Name === undefined) ? null : properties.Name;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.Price = (properties === undefined || properties.Price === undefined) ? null : properties.Price;
    this.CommandType = "AddProductCommand";
    this.CommandWithResult = true;
    this.ResultType = AddProductResultType;
    this.ResultTypeHasMany = false;
}

const ChangeProductPriceCommand = function(properties) {
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Price = (properties === undefined || properties.Price === undefined) ? null : properties.Price;
    this.CommandType = "ChangeProductPriceCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const DiscontinueProductCommand = function(properties) {
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.CommandType = "DiscontinueProductCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const RestockProductCommand = function(properties) {
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Quantity = (properties === undefined || properties.Quantity === undefined) ? null : properties.Quantity;
    this.CommandType = "RestockProductCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const CancelOrderCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.CommandType = "CancelOrderCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const PlaceOrderCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.Items = (properties === undefined || properties.Items === undefined) ? null : properties.Items;
    this.CommandType = "PlaceOrderCommand";
    this.CommandWithResult = true;
    this.ResultType = PlaceOrderResultType;
    this.ResultTypeHasMany = false;
}

const ShipOrderCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.CommandType = "ShipOrderCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const SubmitReviewCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Rating = (properties === undefined || properties.Rating === undefined) ? null : properties.Rating;
    this.Comment = (properties === undefined || properties.Comment === undefined) ? null : properties.Comment;
    this.CommandType = "SubmitReviewCommand";
    this.CommandWithResult = true;
    this.ResultType = SubmitReviewResultType;
    this.ResultTypeHasMany = false;
}

const MarkDeliveredCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.CommandType = "MarkDeliveredCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

