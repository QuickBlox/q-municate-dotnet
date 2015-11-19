using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace QMunicate.Core.Extensions
{
    public static class AsyncExtensions
    {
        public static async void Forget(this Task task) { await task; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget(this IAsyncInfo asyncInfo) { }
    }
}
