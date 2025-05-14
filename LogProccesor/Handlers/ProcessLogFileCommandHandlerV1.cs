using System.Text.RegularExpressions;
using LogProccesor.Models;
using LogProccesor.Requests;
using MediatR;

namespace LogProccesor.Handlers;

public class ProcessLogFileCommandHandlerV1 : IRequestHandler<ProcessLogFileCommand, LogReport>
{
    public async Task<LogReport> Handle(ProcessLogFileCommand request, CancellationToken cancellationToken)
    {
        var report = new LogReport();

        var errorByHour = new Dictionary<string, int>();
        var messageCount = new Dictionary<string, int>();
        var intervals = new Dictionary<string, (DateTime last, TimeSpan sum, int count)>();

        using var reader = new StreamReader(request.Stream);
        string? line;
        Regex regex = new(@"^(?<timestamp>\d{4}[-/]\d{2}[-/]\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>[A-Za-z]+)\] (?<message>.+)$");

        while ((line = await reader.ReadLineAsync()) != null)
        {
            var match = regex.Match(line);
            if (!match.Success)
            {
                Console.WriteLine("Skipped: " + line);
                continue;
            }

            var timestamp = DateTime.Parse(match.Groups["timestamp"].Value);
            var level = match.Groups["level"].Value;
            var message = match.Groups["message"].Value;

            // Errors per hour
            if (level == "ERROR")
            {
                var hourKey = timestamp.ToString("yyyy-MM-dd HH:00");
                errorByHour.TryGetValue(hourKey, out int count);
                errorByHour[hourKey] = count + 1;
            }

            // Top messages
            messageCount.TryGetValue(message, out int msgCount);
            messageCount[message] = msgCount + 1;

            // Avg intervals
            if (intervals.TryGetValue(level, out var val))
            {
                var delta = timestamp - val.last;
                intervals[level] = (timestamp, val.sum + delta, val.count + 1);
            }
            else
            {
                intervals[level] = (timestamp, TimeSpan.Zero, 0);
            }
        }

        report.ErrorsByHour = errorByHour;
        report.TopMessages = messageCount
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        report.AvgIntervalsByLevel = intervals.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.count == 0 ? "N/A" : (kv.Value.sum.TotalSeconds / kv.Value.count).ToString("F2") + " sec");

        return report;
    }
}