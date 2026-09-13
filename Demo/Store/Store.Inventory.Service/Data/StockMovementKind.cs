namespace Store.Inventory.Service.Data
{
    /// <summary>
    /// Values of <see cref="StockMovementDataModel.Kind"/>, stored by name.
    /// </summary>
    public enum StockMovementKind
    {
        Restocked,
        Reserved,
        Released,
        Shipped
    }
}
