using MediatR;

namespace LogProccesor.Requests;

public class GenerateLogFileCommand : IRequest<GenerateLogFileResult>
{
    public int SizeMb { get; set; }
}

public class GenerateLogFileResult
{
    public string FilePath { get; set; } = default!;
    public double SizeInMb { get; set; }
}