//-----------------------------------------------------------------------
// <copyright file="QueueFairLogger.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
namespace QueueFair.Adapter
{
    using System;
    using Microsoft.Extensions.Logging;

    public class QueueFairLogger
    {
        private ILogger logger;

        public QueueFairLogger(ILogger l)
        {
            this.logger = l;
        }

        public static LogLevel Level { get; set; } = LogLevel.Warning;

        public void Log(string message)
        {
            this.logger.Log(Level, "QF " + message);
        }

        public void Err(Exception exception)
        {
            this.logger.LogError(exception, "QF an error occurred!");
        }
    }
}
