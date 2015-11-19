using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMunicate.Core.DependencyInjection
{
    internal class InstanceBinding<TSource, TReal> : TypeBindingBase where TReal : TSource
    {
        private readonly TReal instance;

        public InstanceBinding(TReal instance)
        {
            this.instance = instance;
        }

        internal override object GetRealInstance()
        {
            return instance;
        }
    }
}
