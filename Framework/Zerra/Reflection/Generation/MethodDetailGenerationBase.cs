namespace Zerra.Reflection.Generation
{
    public abstract class MethodDetailGenerationBase<T> : MethodDetail<T>
    {
        protected readonly object locker;
        public MethodDetailGenerationBase(object locker) => this.locker = locker;
    }
}
