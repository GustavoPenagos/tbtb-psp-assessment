using System.Text;
using Microsoft.Extensions.Configuration;

namespace WebApi.Services;

public interface IFileLoggerService
{
    void LogError(Exception exception, string endpoint, string method, string? traceId = null);
    void LogInfo(string message, string? endpoint = null);
}

public class FileLoggerService : IFileLoggerService
{
    private static readonly object _fileLock = new();
    private readonly string _logFilePath;

    public FileLoggerService(IConfiguration configuration)
    {
        var logDir = configuration["LoggingConfig:LogDirectory"] ?? @"E:\logs";
        var logFileName = configuration["LoggingConfig:LogFileName"] ?? "logs_TBTB.PSP.text";

        try
        {
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }
        catch
        {
            // If primary drive E: is unavailable, fallback to local application directory
            logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        _logFilePath = Path.Combine(logDir, logFileName);
    }

    public void LogError(Exception exception, string endpoint, string method, string? traceId = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine($"TIMESTAMP (UTC): {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}");
        sb.AppendLine($"TRACE ID       : {traceId ?? Guid.NewGuid().ToString()}");
        sb.AppendLine($"METHOD / ROUTE : {method} {endpoint}");
        sb.AppendLine($"EXCEPTION TYPE : {exception.GetType().FullName}");
        sb.AppendLine($"MESSAGE        : {exception.Message}");
        sb.AppendLine("STACK TRACE    :");
        sb.AppendLine(exception.StackTrace);
        if (exception.InnerException != null)
        {
            sb.AppendLine($"INNER EXCEPTION: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}");
            sb.AppendLine(exception.InnerException.StackTrace);
        }
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        WriteToFile(sb.ToString());
    }

    public void LogInfo(string message, string? endpoint = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}] [INFO] {(endpoint != null ? $"[{endpoint}] " : "")}{message}");
        WriteToFile(sb.ToString());
    }

    private void WriteToFile(string content)
    {
        lock (_fileLock)
        {
            try
            {
                File.AppendAllText(_logFilePath, content, Encoding.UTF8);
            }
            catch
            {
                // Fallback to console if file system write fails
                Console.WriteLine(content);
            }
        }
    }
}
