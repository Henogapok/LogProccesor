namespace LogProccesor.Models;

public class LogReport
{
    public Dictionary<string, int> ErrorsByHour { get; set; } = new();
    public Dictionary<string, int> TopMessages { get; set; } = new();
    public Dictionary<string, string> AvgIntervalsByLevel { get; set; } = new();
}