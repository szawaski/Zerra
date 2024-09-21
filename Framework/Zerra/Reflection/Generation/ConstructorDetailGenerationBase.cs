namespace Zerra.Reflection.Generation
{
    public abstract class ConstructorDetailGenerationBase<T> : ConstructorDetail<T>
    {
        protected readonly object locker;
        public ConstructorDetailGenerationBase(object locker) => this.locker = locker;
    }
}
