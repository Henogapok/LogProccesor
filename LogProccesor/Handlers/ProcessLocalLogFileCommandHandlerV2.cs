using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using LogProccesor.Models;
using LogProccesor.Requests;
using MediatR;

namespace LogProccesor.Handlers;

public class ProcessLocalLogFileCommandHandler : IRequestHandler<ProcessLocalLogFileCommandV2, LogReport>
{
    public async Task<LogReport> Handle(ProcessLocalLogFileCommandV2 request, CancellationToken cancellationToken)
    {
        var report = new LogReport();
        var regex = new Regex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>[A-Z]+)\] (?<message>.+)$");

        var errorsByHour = new ConcurrentDictionary<string, int>();
        var messageCounts = new ConcurrentDictionary<string, int>();
        var intervals = new ConcurrentDictionary<string, (DateTime last, TimeSpan sum, int count)>();

        const string filePath = "Logs/generated-log.log";
        
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100_000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });

        
        // Чтение файла в канал
        var readerTask = Task.Run(async () =>
        {
            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                await channel.Writer.WriteAsync(line, cancellationToken);
            }
            channel.Writer.Complete();
        });
        

        // Обработка в нескольких потоках
        var workerCount = Environment.ProcessorCount;
        var workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
        {
            await foreach (var line in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var match = regex.Match(line);
                if (!match.Success) continue;

                var timestamp = DateTime.Parse(match.Groups["timestamp"].Value);
                var level = match.Groups["level"].Value.ToUpperInvariant();
                var message = match.Groups["message"].Value;
                
                /*var threadId = Environment.CurrentManagedThreadId;
                Console.WriteLine($"[Поток {threadId}] Обработана строка: {line.Substring(0, Math.Min(40, line.Length))}");*/

                // Errors by hour
                if (level == "ERROR")
                {
                    var hour = timestamp.ToString("yyyy-MM-dd HH:00");
                    errorsByHour.AddOrUpdate(hour, 1, (_, count) => count + 1);
                }

                // Frequent messages
                messageCounts.AddOrUpdate(message, 1, (_, count) => count + 1);

                // Interval by level
                intervals.AddOrUpdate(level,
                    _ => (timestamp, TimeSpan.Zero, 0),
                    (_, prev) =>
                    {
                        var delta = timestamp - prev.last;
                        return (timestamp, prev.sum + delta, prev.count + 1);
                    });
            }
        }, cancellationToken)).ToList();

        
        
        
        
        var sw = new Stopwatch();
        sw.Start();
        
        await readerTask;
        
        sw.Stop();
        Console.WriteLine($"Файл прочитан за {sw.ElapsedMilliseconds}ms");
        sw.Restart();
        
        await Task.WhenAll(workers);
        sw.Stop();
        Console.WriteLine($"Все записи обработаны за {sw.ElapsedMilliseconds}");

        // Заполняем итоговую модель
        report.ErrorsByHour = new Dictionary<string, int>(errorsByHour);
        report.TopMessages = messageCounts
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        report.AvgIntervalsByLevel = intervals.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.count == 0 ? "N/A" : (kv.Value.sum.TotalSeconds / kv.Value.count).ToString("F2") + " sec"
        );

        return report;
    }
}
