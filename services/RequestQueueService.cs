using System.Threading.Channels;

namespace StockReplenishmentAPI.Services; 

public class RequestQueueService
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(); 

    public async Task QueueRequest(Guid requestId)
    {
        await _queue.Writer.WriteAsync(requestId);
    }

    public async Task<Guid> DequeueAsync(CancellationToken token)
    {
        return await _queue.Reader.ReadAsync(token);
    }
}