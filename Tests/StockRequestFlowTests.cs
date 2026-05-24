using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using StockReplenishmentAPI.Controllers;
using StockReplenishmentAPI.DTOs;
using StockReplenishmentAPI.Models;
using StockReplenishmentAPI.Services;

namespace StockReplenishmentAPI.Tests.Integration;

[TestFixture]
public class StockRequestFullPipelineTests
{
    private ServiceProvider _provider = null!;
    private CancellationTokenSource _cts = null!;

    [SetUp]
    public void Setup()
    {
        _cts = new CancellationTokenSource();

        var services = new ServiceCollection();

        // ONE shared in-memory database
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<StockDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        services.AddSingleton<RequestQueueService>();

        services.AddScoped<StockRequestController>();
        services.AddScoped<StockRequestProcessor>();

        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _cts.Cancel();
        _provider.Dispose();
        _cts.Dispose();
    }

    [Test]
    public async Task Create_Approve_Fulfill_FullPipeline_ShouldSucceed()
    {
        var processor = _provider.GetRequiredService<StockRequestProcessor>();
        var processorTask = processor.StartAsync(_cts.Token);

        var controller = _provider.GetRequiredService<StockRequestController>();

        var createResult = await controller.Create(new CreateStockRequestDTO
        {
            Worker = "John",
            Priority = Priority.Urgent,
            Building = Building.HallA,
            Table = 5,
            Items = new List<MaterialItem>
            {
                new()
                {
                    Id = 1,
                    ArticleNumber = 1001,
                    Description = "Item",
                    Quantity = 2
                }
            }
        });

        Assert.That(createResult, Is.Not.Null);

        Guid requestId;

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
            var request = await db.StockRequests.FirstAsync();
            requestId = request.Id;

            Assert.That(request.Status, Is.EqualTo(RequestStatus.PendingReview));
        }

        await controller.Approve(requestId, "Admin");

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
            var approved = await db.StockRequests.FirstAsync(x => x.Id == requestId);

            Assert.That(approved.Status, Is.EqualTo(RequestStatus.Approved));
        }

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

            var request = await db.StockRequests.FirstAsync(x => x.Id == requestId);
            request.ExpectedFulfullmentTime = DateTime.UtcNow.AddMilliseconds(-1);

            await db.SaveChangesAsync();
        }

        int timeout = 0;
        while (timeout < 10)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();

            var updated = await db.StockRequests
                .AsNoTracking()
                .FirstAsync(x => x.Id == requestId);

            if (updated.Status == RequestStatus.Fulfilled)
            {
                Assert.Pass("Request successfully fulfilled through full pipeline.");
                return;
            }

            await Task.Delay(1000);
            timeout++;
        }

        Assert.Fail("Request was not fulfilled within timeout.");
    }
}