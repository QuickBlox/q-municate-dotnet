namespace QMunicate.Core.DependencyInjection
{
    internal abstract class TypeBindingBase
    {
        internal LifetimeMode Mode { get; set; }

        internal abstract object GetRealInstance();
    }
}
