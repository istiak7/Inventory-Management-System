using Inventory_Management_System.Shared.Services.Outbox;
using Quartz;

namespace Inventory_Management_System.Shared.Background
{
    [DisallowConcurrentExecution]
    public class OutboxProcessorJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        public OutboxProcessorJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Executing OutboxProcessorJob...");
           
            using var scope = _serviceProvider.CreateScope();
            var outboxProcessorService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            await outboxProcessorService.ProcessOutboxMessagesAsync(context.CancellationToken);
        }
    }
}
