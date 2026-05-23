using Microsoft.EntityFrameworkCore;
using StockReplenishmentAPI.Models;

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
            Guid requestId = await _queue.DequeueAsync(stoppenToken);
            using IServiceScope scope = _scopeFactory.CreateScope();
            StockDbContext db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

            StockRequest? request = await db.StockRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == requestId, stoppenToken);

            if(request == null)
                continue;

            request.Logs.Add($"System: Reuqest entered processing queue");

            await db.SaveChangesAsync(stoppenToken);

            while(request.Status == RequestStatus.PendingReview)
            {
                await Task.Delay(2000,stoppenToken);
                await db.Entry(request).ReloadAsync(stoppenToken);
            }

            if(request.Status == RequestStatus.Denied)
            {
                request.Logs.Add("System: Request denied");

                await db.SaveChangesAsync(stoppenToken);

                continue;
            }

            request.Logs.Add("System: Starting fulfillment");
            await db.SaveChangesAsync(stoppenToken);

            while(request.Status == RequestStatus.Approved)
            {
                await db.Entry(request).ReloadAsync(stoppenToken);

                if(request.ExpectedFulfullmentTime == null)
                {
                    request.Logs.Add("System: Missing fulfillment ETA");
                    request.Status = RequestStatus.Failed;
                    await db.SaveChangesAsync(stoppenToken);
                    break;
                }

                TimeSpan remaining = request.ExpectedFulfullmentTime.Value - DateTime.UtcNow;

                if(remaining <= TimeSpan.Zero)
                {
                    request.Fulfill();
                    request.Logs.Add("System: Request fulfilled");
                    await db.SaveChangesAsync(stoppenToken);
                    break;
                }

                await Task.Delay(1000,stoppenToken);
            }
        }
    }
}