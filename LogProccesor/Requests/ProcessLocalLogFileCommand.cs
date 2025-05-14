using LogProccesor.Models;
using MediatR;

namespace LogProccesor.Requests;

public class ProcessLocalLogFileCommand : IRequest<LogReport>
{
}