using LogProccesor.Models;
using MediatR;

namespace LogProccesor.Requests;

public class ProcessLogFileCommand : IRequest<LogReport>
{
    public Stream Stream { get; set; } = default!;
}