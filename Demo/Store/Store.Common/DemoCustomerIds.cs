namespace Store.Common
{
    /// <summary>
    /// Well-known customer IDs for the seed data, shared the same way as <see cref="DemoProductIds"/> so
    /// the Orders and Reviews seed data can agree on who bought what.
    /// </summary>
    public static class DemoCustomerIds
    {
        public static readonly Guid Ada = Guid.Parse("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0001");
        public static readonly Guid Grace = Guid.Parse("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0002");
        public static readonly Guid Alan = Guid.Parse("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0003");
    }
}
