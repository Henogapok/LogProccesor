using MediatR;
using Serilog;
using System.Reflection;
using LogProccesor.Requests;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// 🔹 Services
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L; // 1 ГБ
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L; // 1 ГБ
});
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/process-log", async (HttpRequest request, IFormFile file, IMediator mediator) =>
    {
        using var stream = file.OpenReadStream();
        var result = await mediator.Send(new ProcessLogFileCommand { Stream = stream });
        return Results.Ok(result);
    })
    .AllowAnonymous()
    .DisableAntiforgery();
app.MapPost("/process-log-v2", async (HttpRequest request, IFormFile file, IMediator mediator) =>
    {
        using var stream = file.OpenReadStream();
        var result = await mediator.Send(new ProcessLogFileCommandV2 { Stream = stream });
        return Results.Ok(result);
    })
    .AllowAnonymous() 
    .DisableAntiforgery();
app.MapPost("/process-local-log-v1", async (HttpRequest request, IMediator mediator) =>
    {
        var result = await mediator.Send(new ProcessLocalLogFileCommand());
        return Results.Ok(result);
    })
    .AllowAnonymous()
    .DisableAntiforgery();
app.MapPost("/process-local-log-v2", async (HttpRequest request, IMediator mediator) =>
    {
        var result = await mediator.Send(new ProcessLocalLogFileCommandV2());
        return Results.Ok(result);
    })
    .AllowAnonymous()
    .DisableAntiforgery();

app.MapPost("/generate-log", async (int sizeMb, IMediator mediator) =>
{
    var result = await mediator.Send(new GenerateLogFileCommand { SizeMb = sizeMb });
    return Results.Ok(new
    {
        Message = "Log file generated",
        File = result.FilePath,
        Size = $"{result.SizeInMb:F2} MB"
    });
});

app.Run();