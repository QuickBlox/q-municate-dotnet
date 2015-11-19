namespace QMunicate.Core.DependencyInjection
{
    internal class TypeBinding<TSource, TReal> : TypeBindingBase
        where TReal : TSource, new()
    {
        private TReal singletonInstance;

        private object singletonSyncObject = new object();

        internal override object GetRealInstance()
        {
            object result = null;
            if (this.Mode == LifetimeMode.Singleton)
            {
                if (this.singletonInstance == null)
                {
                    lock (this.singletonSyncObject)
                    {
                        if (this.singletonInstance == null)
                        {
                            this.singletonInstance = new TReal();
                        }
                    }
                }

                result = this.singletonInstance;
            }
            else
            {
                result = new TReal();
            }

            return result;
        }
    }
}
