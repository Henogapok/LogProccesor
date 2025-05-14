using LogProccesor.Models;
using MediatR;

namespace LogProccesor.Requests;

public class ProcessLogFileCommandV2 : IRequest<LogReport>
{
    public Stream Stream { get; set; } = default!;
}