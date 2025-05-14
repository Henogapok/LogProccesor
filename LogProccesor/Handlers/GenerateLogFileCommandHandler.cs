using System.Text;
using LogProccesor.Requests;
using MediatR;

namespace LogProccesor.Handlers;

public class GenerateLogFileCommandHandler : IRequestHandler<GenerateLogFileCommand, GenerateLogFileResult>
{
    public async Task<GenerateLogFileResult> Handle(GenerateLogFileCommand request, CancellationToken cancellationToken)
    {
        var logsDir = "Logs";
        var fileName = "generated-log.log";
        var filePath = Path.Combine(logsDir, fileName);

        Directory.CreateDirectory(logsDir);

        var levels = new[] { "INFO", "WARNING", "ERROR" };
        var messages = new[]
        {
            "Starting application",
            "Low disk space",
            "Database connection failed",
            "Scheduled job started",
            "Timeout while contacting external service",
            "Health check passed",
            "High memory usage",
            "Application stopped gracefully",
            "Disk read failure"
        };

        var rand = new Random();
        var currentTime = new DateTime(2025, 5, 2, 0, 0, 0);
        var targetSize = request.SizeMb * 1024L * 1024L;

        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        while (writer.BaseStream.Length < targetSize)
        {
            currentTime = currentTime.AddSeconds(rand.Next(1, 60));
            var level = levels[rand.Next(levels.Length)];
            var message = messages[rand.Next(messages.Length)];
            var line = $"{currentTime:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
        var fileSizeBytes = new FileInfo(filePath).Length;

        return new GenerateLogFileResult
        {
            FilePath = filePath,
            SizeInMb = fileSizeBytes / 1024d / 1024d
        };
    }
}