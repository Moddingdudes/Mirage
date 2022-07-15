using System;
using UnityEngine;

namespace Mirage.Tests.PlayerTests
{
    /// <summary>
    /// checks for errors or exceptions sent to `Debug.unityLogger.logHandler`
    /// </summary>
    public class LogErrorChecker : ILogHandler, IDisposable
    {
        private ILogHandler inner;
        public bool HasErrors;

        public LogErrorChecker()
        {
            this.inner = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = this;
        }
        public void Dispose()
        {
            Debug.unityLogger.logHandler = this.inner;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            this.HasErrors = true;
            this.inner.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if (logType == LogType.Error || logType == LogType.Assert || logType == LogType.Exception)
                this.HasErrors = true;

            this.inner.LogFormat(logType, context, format, args);
        }
    }
}

