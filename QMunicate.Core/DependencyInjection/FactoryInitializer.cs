namespace QMunicate.Core.DependencyInjection
{
    public abstract class FactoryInitializer
    {
        public abstract void SetBindings(ServiceLocator initializedFactory);
    }
}
