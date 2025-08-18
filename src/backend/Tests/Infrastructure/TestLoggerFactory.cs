using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ea_Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Factory for creating test loggers and mocked logger instances.
    /// Provides consistent logging setup across all test scenarios.
    /// </summary>
    public static class TestLoggerFactory
    {
        /// <summary>
        /// Creates a null logger for tests that don't need logging output.
        /// </summary>
        /// <typeparam name="T">Type for the logger</typeparam>
        /// <returns>Null logger instance</returns>
        public static ILogger<T> CreateNullLogger<T>()
        {
            return NullLogger<T>.Instance;
        }

        /// <summary>
        /// Creates a mock logger for tests that need to verify logging behavior.
        /// </summary>
        /// <typeparam name="T">Type for the logger</typeparam>
        /// <returns>Mock logger instance</returns>
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a console logger for tests that need visible output.
        /// </summary>
        /// <typeparam name="T">Type for the logger</typeparam>
        /// <returns>Console logger instance</returns>
        public static ILogger<T> CreateConsoleLogger<T>()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            return loggerFactory.CreateLogger<T>();
        }
    }
}
