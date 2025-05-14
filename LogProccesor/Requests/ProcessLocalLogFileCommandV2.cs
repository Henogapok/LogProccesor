using LogProccesor.Models;
using MediatR;

namespace LogProccesor.Requests;

public class ProcessLocalLogFileCommandV2 : IRequest<LogReport>
{
}