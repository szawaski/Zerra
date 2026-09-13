namespace Store.Common
{
    /// <summary>
    /// Well-known product IDs for the seed data. Each service seeds its own data store independently,
    /// so the Catalog products, their Inventory stock, and the sample Orders agree on these IDs.
    /// </summary>
    public static class DemoProductIds
    {
        public static readonly Guid MechanicalKeyboard = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0001");
        public static readonly Guid WirelessMouse = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0002");
        public static readonly Guid UltrawideMonitor = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0003");
        public static readonly Guid UsbCDock = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0004");
        public static readonly Guid NoiseCancellingHeadphones = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0005");
        public static readonly Guid Webcam4K = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0006");
        public static readonly Guid StandingDesk = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0007");
        public static readonly Guid ErgonomicChair = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0008");
        public static readonly Guid DeskLamp = Guid.Parse("0a6f1f3e-6c1d-4f1b-9c55-1b1f4c0e0009");
    }
}
