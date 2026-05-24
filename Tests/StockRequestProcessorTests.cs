using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using StockReplenishmentAPI.Models;
using StockReplenishmentAPI.Services;

namespace StockReplenishmentAPI.Tests.Services;

[TestFixture]
public class StockRequestProcessorTests
{
    private StockDbContext _db = null!;
    private RequestQueueService _queue = null!;
    private StockRequestProcessor _processor = null!;
    private CancellationTokenSource _cts = null!;

    [SetUp]
    public void Setup()
    {
        DbContextOptions<StockDbContext> options = new DbContextOptionsBuilder<StockDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new StockDbContext(options);
        _queue = new RequestQueueService();
        _cts = new CancellationTokenSource();
        
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(StockDbContext)).Returns(_db);

        IServiceScope scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _processor = new StockRequestProcessor(scopeFactory,_queue);
    }

    [TearDown]
    public async Task TearDown()
    {
        if(_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        if(_db != null)
        {
            await _db.DisposeAsync();
        }

        if(_processor != null)
        {
            await _processor.StopAsync(CancellationToken.None);
            _processor.Dispose();
        }
    }

    [Test]
    public async Task ApproveRequest_ShouldBecomeFulFilled()
    {
        var request = new StockRequest(
            worker: "John",
            priority: Priority.Urgent,
            building: Building.HallA,
            table: 5,
            items: []
        )
        {
            Status = RequestStatus.Approved,
            ApprovedAt = DateTime.UtcNow,
            ExpectedFulfullmentTime = DateTime.UtcNow.AddSeconds(1)
        };

        _db.StockRequests.Add(request);
        await _db.SaveChangesAsync();
        
        await _queue.QueueRequest(request.Id);

        await _processor.StartAsync(_cts.Token);

        await Task.Delay(3000);

        StockRequest? updated = await _db.StockRequests.FirstOrDefaultAsync(x => x.Id == request.Id);

        await _cts.CancelAsync();

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo(RequestStatus.Fulfilled));
        Assert.That(updated.Logs.Any(x => x.Contains("fulfilled")), Is.True);
    }

    [Test]
    public async Task DeniedRequest_ShouldRemainDenied()
    {
        StockRequest request = new(
            worker: "John",
            priority: Priority.MEDIUM,
            building: Building.HallB,
            table: 12,
            items: []
        )
        {
            Status = RequestStatus.Denied
        };
        _db.StockRequests.Add(request);

        await _db.SaveChangesAsync();
        await _queue.QueueRequest(request.Id);
        await _processor.StartAsync(_cts.Token);

        await Task.Delay(2000);

        StockRequest? updated = await _db.StockRequests.FirstOrDefaultAsync(x => x.Id == request.Id);
        await _cts.CancelAsync();

        Assert.That(updated,Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo(RequestStatus.Denied));
        Assert.That(updated.Logs.Any(x => x.Contains("Request denied")), Is.True);
    }

    [Test]
    public async Task MissingEta_ShouldFailRequest()
    {
        StockRequest request = new(
            worker: "John",
            priority: Priority.LOW,
            building: Building.HallB,
            table: 12,
            items: []
        )
        {
            Status = RequestStatus.Approved,
            ApprovedAt = DateTime.UtcNow
        };

        _db.StockRequests.Add(request);
        await _db.SaveChangesAsync();
        await _queue.QueueRequest(request.Id);

        await _processor.StartAsync(_cts.Token);
        await Task.Delay(2000);

        StockRequest? updated = await _db.StockRequests.FirstOrDefaultAsync(x => x.Id == request.Id);

        await _cts.CancelAsync();

        Assert.That(updated!.Status, Is.EqualTo(RequestStatus.Failed));
        Assert.That(updated.Logs.Any(x => x.Contains("Missing fulfillment ETA")), Is.True);
    }
}