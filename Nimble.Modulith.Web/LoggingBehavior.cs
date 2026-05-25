using Mediator; using System.Diagnostics; using System.Reflection;
namespace Nimble.Modulith.Web;
public class LoggingBehavior<TRequest,TResponse>(ILogger<LoggingBehavior<TRequest,TResponse>> logger):IPipelineBehavior<TRequest,TResponse> where TRequest:notnull,IMessage
{ public async ValueTask<TResponse> Handle(TRequest request,MessageHandlerDelegate<TRequest,TResponse> next,CancellationToken ct){ logger.LogInformation("Handling {RequestName}",typeof(TRequest).Name); foreach(var p in request.GetType().GetProperties()) logger.LogInformation("Property {Property}: {@Value}",p.Name,p.GetValue(request)); var sw=Stopwatch.StartNew(); var response=await next(request,ct); logger.LogInformation("Handled {RequestName} in {Ms} ms",typeof(TRequest).Name,sw.ElapsedMilliseconds); return response; } }
