using System.Threading.Tasks;

namespace QMunicate.Core.Logger
{
    public interface IQmunicateLogger
    {
        Task Log(QmunicateLogLevel logLevel, string message);
    }

    public enum QmunicateLogLevel
    {
        Error,
        Warn,
        Debug
    }
}
