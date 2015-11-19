using System.Threading.Tasks;

namespace QMunicate.Core.Logger
{
    public class QmunicateLoggerHolder
    {
        public static IQmunicateLogger LoggerInstance { get; set; }

        public static async Task Log(QmunicateLogLevel logLevel, string message)
        {
            var logger = LoggerInstance;
            if (logger != null)
            {
                await logger.Log(logLevel, message);
            }
        }
    }
}
