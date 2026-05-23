using Microsoft.EntityFrameworkCore;
using StockReplenishmentAPI.Models;
using StockReplenishmentAPI.Services;

namespace StockReplenishmentAPI.Services;

public class StockRequestProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; 
    private readonly RequestQueueService _queue;

    public StockRequestProcessor(
        IServiceScopeFactory scopeFactory,
        RequestQueueService queue
    )
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppenToken
    )
    {
        while (!stoppenToken.IsCancellationRequested)
        {
            var requestId = await _queue.DequeueAsync(stoppenToken);

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

            StockRequest? request = await db.StockRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == requestId, stoppenToken);

            if(request == null)
                continue;

            while(request.Status == RequestStatus.PendingReview)
            {
                await Task.Delay(2000,stoppenToken);
                await db.Entry(request).ReloadAsync(stoppenToken);
            }

            if(request.Status == RequestStatus.Denied)
                continue;

            var randomDelay = Random.Shared.Next(5000,15000);

            await Task.Delay(randomDelay,stoppenToken);

            request.Fulfill();

            await db.SaveChangesAsync(stoppenToken);
        }
    }
}