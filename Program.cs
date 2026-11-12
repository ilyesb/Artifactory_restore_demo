using Newtonsoft.Json;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var payload = JsonConvert.SerializeObject(new
{
    Message = "Restauration NuGet via Artifactory OK",
    Date = DateTime.UtcNow
});

Log.Information("{Payload}", payload);
Log.CloseAndFlush();
