using Lithnet.ResourceManagement.McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services.AddSingleton<MimClientFactory>();
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "mim-mcp", Version = "1.0.0" };
    options.ServerInstructions = ServerInstructions.Text;
})
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
