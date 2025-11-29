namespace Zerra.CQRS
{
    public interface IHandler
    {
        BusContext Context { get; }
        void Initialize(BusContext context);
    }
}
