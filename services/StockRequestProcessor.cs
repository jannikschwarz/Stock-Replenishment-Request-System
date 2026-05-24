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
            Guid requestId;

            try
            {
                requestId = await _queue.DequeueAsync(stoppenToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessRequest(requestId,stoppenToken);
        }
    }

    private async Task ProcessRequest(Guid requestId, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        try
        {
            var request = await db.StockRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == requestId,token);

            if(request == null)
                return;

            request.Logs.Add("System: Request entered processing queue");
            await db.SaveChangesAsync(token);

            while (!token.IsCancellationRequested)
            {
                var status = await db.StockRequests.Where(r => r.Id == requestId).Select(r => r.Status).FirstOrDefaultAsync(token);

                if(status == RequestStatus.Denied)
                {
                    await AddLog(requestId,"System: Request denied",token);
                    return;
                }

                if(status != RequestStatus.PendingReview)
                    break;
                
                await Task.Delay(1000,token);
            }

            await AddLog(requestId,"System: Starting fulfillment",token);

            for(int i = 0; i < 30 && !token.IsCancellationRequested; i++)
            {
                var snapshot = await db.StockRequests.Where(r => r.Id == requestId).Select(r => new
                {
                    r.Status,r.ExpectedFulfullmentTime
                }).FirstOrDefaultAsync(token);

                if(snapshot == null)
                    return;

                if(snapshot.Status != RequestStatus.Approved)
                    return;

                if(snapshot.ExpectedFulfullmentTime == null)
                {
                    await Fail(requestId,"System: Missing fulfillment ETA",token);
                    return;
                }

                if(snapshot.ExpectedFulfullmentTime <= DateTime.UtcNow)
                {
                    await Fulfill(requestId,token);
                    return;
                }

                await Task.Delay(1000,token);
            }

            await Fail(requestId,"System: Request timed out",token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }
    }

    private async Task AddLog(Guid requestId, string message, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        var request = await db.StockRequests.FindAsync([requestId], token);
        if (request == null) return;

        request.Logs.Add(message);
        await db.SaveChangesAsync(token);
    }

    private async Task Fail(Guid requestId, string message, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        var request = await db.StockRequests.FindAsync([requestId], token);
        if (request == null) 
            return;

        request.Status = RequestStatus.Failed;
        request.Logs.Add(message);

        await db.SaveChangesAsync(token);
    }

    private async Task Fulfill(Guid requestId, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

        var request = await db.StockRequests.FindAsync([requestId], token);
        if (request == null) 
            return;

        request.Fulfill();
        request.Logs.Add("System: Request fulfilled");

        await db.SaveChangesAsync(token);
    }
}