using System.Threading.Tasks;
using log4net;
using NuGet.Common;

namespace LiGet
{
    public class Log4NetLoggerAdapter : ILogger
    {
        private readonly ILog log4net;
        public Log4NetLoggerAdapter(ILog log4net)
        {
            this.log4net = log4net;

        }

        public void Log(LogLevel level, string data)
        {
            switch(level) {
                case LogLevel.Debug:
                case LogLevel.Verbose:
                    log4net.Debug(data);
                    break;
                case LogLevel.Information:
                    log4net.Info(data);
                    break;
                case LogLevel.Warning:
                    log4net.Warn(data);
                    break;
                case LogLevel.Error:
                    log4net.Error(data);
                    break;
                case LogLevel.Minimal:
                    log4net.Fatal(data);
                    break;
            }
        }

        public void Log(ILogMessage message)
        {
            throw new System.NotImplementedException();
        }

        public Task LogAsync(LogLevel level, string data)
        {
            throw new System.NotImplementedException();
        }

        public Task LogAsync(ILogMessage message)
        {
            throw new System.NotImplementedException();
        }

        public void LogDebug(string data)
        {
            log4net.Debug(data);
        }

        public void LogError(string data)
        {
            log4net.Error(data);
        }

        public void LogInformation(string data)
        {
            log4net.Info(data);
        }

        public void LogInformationSummary(string data)
        {
            log4net.Info(data);
        }

        public void LogMinimal(string data)
        {
            log4net.Fatal(data);
        }

        public void LogVerbose(string data)
        {
            log4net.Debug(data);
        }

        public void LogWarning(string data)
        {
            log4net.Error(data);
        }
    }
}