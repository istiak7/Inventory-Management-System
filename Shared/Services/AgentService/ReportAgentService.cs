using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;

namespace Inventory_Management_System.Shared.Services.AgentService
{
    public class ReportAgentService
    {
        private McpClient? _mcpClient;
        private Kernel? _kernel;

        public async Task<Kernel> GetKernelAsync(CancellationToken cancellationToken = default)
        {
            if (_kernel != null) return _kernel;

            // 1. Establish the MCP Client connection over streamable HTTP
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri("https://localhost:7034/api/mcp") // Address of our MCP server
            });

            // Instantiate client and perform initialization handshakes automatically
             _mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

            // 2. Query the MCP server for available database tools
            var tools = await _mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

            // 3. Initialize the Semantic Kernel Builder
            var kernelBuilder = Kernel.CreateBuilder();

            // Configure connection to local Ollama via the OpenAI connector compatibility layer
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: " llama3.1:8b", // Your configured local Ollama model name (e.g., phi4, llama3)
                apiKey: "none",     // Ollama bypasses security key validation
                endpoint: new Uri("http://localhost:11434/v1") // Default Ollama local endpoint
            );

            // 4. Translate MCP tools to Native Kernel Functions and mount them to the kernel
            var databaseFunctions = tools.Select(t => t.AsKernelFunction());
            kernelBuilder.Plugins.AddFromFunctions("DatabasePlugin", databaseFunctions);

            _kernel = kernelBuilder.Build();
            return _kernel;

        }
        // Automatically called by ASP.NET Core's DI container during application shutdown
        public async ValueTask DisposeAsync()
        {
            if (_mcpClient is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_mcpClient != null)
            {
                _mcpClient.DisposeAsync();
            }
        }
    }
}
