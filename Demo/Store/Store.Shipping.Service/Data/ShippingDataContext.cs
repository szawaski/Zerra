using Zerra.Repository.Memory;

namespace Store.Shipping.Service.Data
{
    /// <summary>
    /// Shipping is intentionally memory-only, there's no database to reach or fall back from.
    /// The point of this service is hosting the CQRS server inside ASP.NET Core over HTTP instead of the raw TCP transport
    /// the other services use, not another data store, so it needs nothing else running to try it.
    /// </summary>
    public sealed class ShippingDataContext : MemoryDataContext
    {
    }
}
