namespace Store.Orders.Service.Data
{
    /// <summary>
    /// Values of <see cref="OrderDataModel.Status"/>, stored by name.
    /// </summary>
    public enum OrderStatus
    {
        Placed,
        Shipped,
        Cancelled
    }
}
