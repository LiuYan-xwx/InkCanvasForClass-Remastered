using Microsoft.Extensions.Logging;

namespace InkCanvasForClass_Remastered.Services.Logging
{
    public class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
    {
        private static readonly AsyncLocal<Stack<object>> ScopeStack = new AsyncLocal<Stack<object>>();
        private FileLoggerProvider Provider { get; } = provider;
        private string CategoryName { get; } = categoryName;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            List<string> scopes = [];
            if (ScopeStack.Value != null)
            {
                scopes.AddRange(ScopeStack.Value.Select(scope => (scope.ToString() ?? "") + "=>"));
            }
            var message = string.Join("", scopes) + formatter(state, exception) + (exception != null ? "\n" + exception : "");
            Provider.WriteLog($"{DateTime.Now}|{logLevel}|{CategoryName}|{message}");
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
