using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

// The isolated worker model with the ASP.NET Core integration supports .NET 10.
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
await builder.Build().RunAsync();
