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
    Lines: "OrderLineModel[]",
}

const OrderLineModelType =
{
    ProductID: "string",
    ProductName: "string",
    UnitPrice: "number",
    Quantity: "number",
    LineTotal: "number",
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

const AddProductResultType =
{
    ProductID: "string",
}

const StockReservationLineType =
{
    ProductID: "string",
    ProductName: "string",
    Quantity: "number",
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
    CategoryModel: CategoryModelType,
    ProductModel: ProductModelType,
    StockLevelModel: StockLevelModelType,
    StockMovementModel: StockMovementModelType,
    CustomerModel: CustomerModelType,
    OrderModel: OrderModelType,
    OrderLineModel: OrderLineModelType,
    ReviewModel: ReviewModelType,
    ProductRatingModel: ProductRatingModelType,
    ShipmentModel: ShipmentModelType,
    AddProductResult: AddProductResultType,
    StockReservationLine: StockReservationLineType,
    OrderItemRequest: OrderItemRequestType,
    PlaceOrderResult: PlaceOrderResultType,
    SubmitReviewResult: SubmitReviewResultType,
}

const ICatalogQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("Store.Catalog.Domain.ICatalogQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("Store.Catalog.Domain.ICatalogQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetCategories: function(onComplete, onFail) {
        Bus.Call("Store.Catalog.Domain.ICatalogQueryHandler", "GetCategories", [null], CategoryModelType, true, onComplete, onFail);
    },
    GetProducts: function(onComplete, onFail) {
        Bus.Call("Store.Catalog.Domain.ICatalogQueryHandler", "GetProducts", [null], ProductModelType, true, onComplete, onFail);
    },
    GetProductsByCategory: function(categoryID, onComplete, onFail) {
        Bus.Call("Store.Catalog.Domain.ICatalogQueryHandler", "GetProductsByCategory", [categoryID, null], ProductModelType, true, onComplete, onFail);
    },
    GetProductsByIDs: function(productIDs, onComplete, onFail) {
        Bus.Call("Store.Catalog.Domain.ICatalogQueryHandler", "GetProductsByIDs", [productIDs, null], ProductModelType, true, onComplete, onFail);
    },
}

const IInventoryQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("Store.Inventory.Domain.IInventoryQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("Store.Inventory.Domain.IInventoryQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetStockLevels: function(onComplete, onFail) {
        Bus.Call("Store.Inventory.Domain.IInventoryQueryHandler", "GetStockLevels", [null], StockLevelModelType, true, onComplete, onFail);
    },
    GetRecentMovements: function(count, onComplete, onFail) {
        Bus.Call("Store.Inventory.Domain.IInventoryQueryHandler", "GetRecentMovements", [count, null], StockMovementModelType, true, onComplete, onFail);
    },
}

const IOrdersQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("Store.Orders.Domain.IOrdersQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("Store.Orders.Domain.IOrdersQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetCustomers: function(onComplete, onFail) {
        Bus.Call("Store.Orders.Domain.IOrdersQueryHandler", "GetCustomers", [null], CustomerModelType, true, onComplete, onFail);
    },
    GetOrders: function(onComplete, onFail) {
        Bus.Call("Store.Orders.Domain.IOrdersQueryHandler", "GetOrders", [null], OrderModelType, true, onComplete, onFail);
    },
    GetOrder: function(orderID, onComplete, onFail) {
        Bus.Call("Store.Orders.Domain.IOrdersQueryHandler", "GetOrder", [orderID, null], OrderModelType, false, onComplete, onFail);
    },
    HasPurchased: function(customerID, productID, onComplete, onFail) {
        Bus.Call("Store.Orders.Domain.IOrdersQueryHandler", "HasPurchased", [customerID, productID, null], null, false, onComplete, onFail);
    },
}

const IReviewsQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("Store.Reviews.Domain.IReviewsQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("Store.Reviews.Domain.IReviewsQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetRecentReviews: function(count, onComplete, onFail) {
        Bus.Call("Store.Reviews.Domain.IReviewsQueryHandler", "GetRecentReviews", [count, null], ReviewModelType, true, onComplete, onFail);
    },
    GetReviewsForProduct: function(productID, onComplete, onFail) {
        Bus.Call("Store.Reviews.Domain.IReviewsQueryHandler", "GetReviewsForProduct", [productID, null], ReviewModelType, true, onComplete, onFail);
    },
    GetProductRatings: function(onComplete, onFail) {
        Bus.Call("Store.Reviews.Domain.IReviewsQueryHandler", "GetProductRatings", [null], ProductRatingModelType, true, onComplete, onFail);
    },
}

const IShippingQueryHandler = {
    GetDataStoreName: function(onComplete, onFail) {
        Bus.Call("Store.Shipping.Domain.IShippingQueryHandler", "GetDataStoreName", [null], null, false, onComplete, onFail);
    },
    GetMessagingName: function(onComplete, onFail) {
        Bus.Call("Store.Shipping.Domain.IShippingQueryHandler", "GetMessagingName", [null], null, false, onComplete, onFail);
    },
    GetShipments: function(onComplete, onFail) {
        Bus.Call("Store.Shipping.Domain.IShippingQueryHandler", "GetShipments", [null], ShipmentModelType, true, onComplete, onFail);
    },
}

const AddProductCommand = function(properties) {
    this.CategoryID = (properties === undefined || properties.CategoryID === undefined) ? null : properties.CategoryID;
    this.Sku = (properties === undefined || properties.Sku === undefined) ? null : properties.Sku;
    this.Name = (properties === undefined || properties.Name === undefined) ? null : properties.Name;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.Price = (properties === undefined || properties.Price === undefined) ? null : properties.Price;
    this.CommandType = "Store.Catalog.Domain.Commands.AddProductCommand";
    this.CommandWithResult = true;
    this.ResultType = AddProductResultType;
    this.ResultTypeHasMany = false;
}

const ChangeProductPriceCommand = function(properties) {
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Price = (properties === undefined || properties.Price === undefined) ? null : properties.Price;
    this.CommandType = "Store.Catalog.Domain.Commands.ChangeProductPriceCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const DiscontinueProductCommand = function(properties) {
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.CommandType = "Store.Catalog.Domain.Commands.DiscontinueProductCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const ReserveStockCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.OrderNumber = (properties === undefined || properties.OrderNumber === undefined) ? null : properties.OrderNumber;
    this.Lines = (properties === undefined || properties.Lines === undefined) ? null : properties.Lines;
    this.CommandType = "Store.Inventory.Domain.Commands.ReserveStockCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const RestockProductCommand = function(properties) {
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Quantity = (properties === undefined || properties.Quantity === undefined) ? null : properties.Quantity;
    this.CommandType = "Store.Inventory.Domain.Commands.RestockProductCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const CancelOrderCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.CommandType = "Store.Orders.Domain.Commands.CancelOrderCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const PlaceOrderCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.Items = (properties === undefined || properties.Items === undefined) ? null : properties.Items;
    this.CommandType = "Store.Orders.Domain.Commands.PlaceOrderCommand";
    this.CommandWithResult = true;
    this.ResultType = PlaceOrderResultType;
    this.ResultTypeHasMany = false;
}

const ShipOrderCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.CommandType = "Store.Orders.Domain.Commands.ShipOrderCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const SubmitReviewCommand = function(properties) {
    this.CustomerID = (properties === undefined || properties.CustomerID === undefined) ? null : properties.CustomerID;
    this.ProductID = (properties === undefined || properties.ProductID === undefined) ? null : properties.ProductID;
    this.Rating = (properties === undefined || properties.Rating === undefined) ? null : properties.Rating;
    this.Comment = (properties === undefined || properties.Comment === undefined) ? null : properties.Comment;
    this.CommandType = "Store.Reviews.Domain.Commands.SubmitReviewCommand";
    this.CommandWithResult = true;
    this.ResultType = SubmitReviewResultType;
    this.ResultTypeHasMany = false;
}

const MarkDeliveredCommand = function(properties) {
    this.OrderID = (properties === undefined || properties.OrderID === undefined) ? null : properties.OrderID;
    this.CommandType = "Store.Shipping.Domain.Commands.MarkDeliveredCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}


