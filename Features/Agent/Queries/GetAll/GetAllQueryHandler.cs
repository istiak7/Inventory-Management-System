using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Services.AgentService;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Inventory_Management_System.Features.Agent.Queries.GetAll
{
    public class GetAllQueryHandler(ReportAgentService reportAgentService) : IRequestHandler<GetAllQuery, Result>
    {
        public async Task<Result> Handle(GetAllQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.searchTerm))
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Bad Request",
                    Message = "Search term cannot be null or empty.",
                    Data = null
                };
            }

            try
            {
                var kernel = await reportAgentService.GetKernelAsync();
                var chatService = kernel.Services.GetRequiredService<IChatCompletionService>();

                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(request.searchTerm.Trim());

                // Configure the settings to automatically execute discovered database tools if called by the LLM.
                var settings = new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var response = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: settings,
                    kernel: kernel,
                    cancellationToken: cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "OK",
                    Message = "Request processed successfully.",
                    Data = new { Answer = response.Content }
                };
            }
            catch (Exception ex)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Internal Server Error",
                    Message = $"An error occurred while processing the request: {ex.Message}",
                    Data = null
                };
            }

        }
    }
}
