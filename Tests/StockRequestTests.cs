using NUnit.Framework;
using StockReplenishmentAPI.Models;

namespace StockReplenishmentAPI.Tests.Models; 

[TestFixture]
public class StockRequestTests
{
    private List<MaterialItem> _items = null!;


    [SetUp]
    public void Setup()
    {
        _items = [
            new MaterialItem{
                Id = 1,
                ArticleNumber = 1001,
                Description = "ARC Extinguisher",
                Quantity = 10
            },
            new MaterialItem{
                Id = 2,
                ArticleNumber = 1002,
                Description = "Tripper",
                Quantity = 8
            },
            new MaterialItem{
                Id = 3,
                ArticleNumber = 1003,
                Description = "Cable Harness",
                Quantity = 9
            },
            new MaterialItem{
                Id = 4,
                ArticleNumber = 1004,
                Description = "Bolts",
                Quantity = 5
            },
        ];
    }

    [Test]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        StockRequest request = new StockRequest(
            "John",
            Priority.MEDIUM,
            Building.HallA,
            12,
            _items
        );

        Assert.That(request.Worker, Is.EqualTo("John"));
        Assert.That(request.Priority, Is.EqualTo(Priority.MEDIUM));
        Assert.That(request.Building, Is.EqualTo(Building.HallA));
        Assert.That(request.Table, Is.EqualTo(12));
        Assert.That(request.Status, Is.EqualTo(RequestStatus.PendingReview));
        Assert.That(request.Logs.Count, Is.EqualTo(1));
    }

    [Test]
    public void Approve_ShouldUpdateStatusAndTimes()
    {
        StockRequest request = new StockRequest(
            "John",
            Priority.LOW,
            Building.HallB,
            5,
            _items
        );

        request.Approve("Admin1");

        Assert.That(request.Status, Is.EqualTo(RequestStatus.Approved));
        Assert.That(request.ApprovedAt, Is.Not.Null);
        Assert.That(request.ExpectedFulfullmentTime, Is.Not.Null);
        Assert.That(request.EstimatedDuration, Is.GreaterThan(0));
        Assert.That(request.Logs.Last(),Does.Contain("Approved stock request"));
    }

    [Test]
    public void Deny_ShouldSetStatusAndReason()
    {
        StockRequest request = new StockRequest(
            "David",
            Priority.MEDIUM,
            Building.HallC,
            3,
            _items
        );

        request.Deny("Admin2","Unauthorized");
        
        Assert.That(request.Status, Is.EqualTo(RequestStatus.Denied));
        Assert.That(request.DenialReason, Is.EqualTo("Unauthorized"));
        Assert.That(request.Logs.Last(),Does.Contain("Denied request"));
    }

    [Test]
    public void Fulfill_ShouldSetStatusToFulfilled()
    {
        StockRequest request = new StockRequest(
            "Lora",
            Priority.Urgent,
            Building.AssemblyHO1,
            9,
            _items
        );

        request.Fulfill();

        Assert.That(request.Status, Is.EqualTo(RequestStatus.Fulfilled));
        Assert.That(request.Logs.Last(),Does.Contain("fulfilled"));
    }

    [Test]
    public void UpdatePriority_WhenApproved_ShouldUpdateEta()
    {
        StockRequest request = new StockRequest(
            "Ruji",
            Priority.LOW,
            Building.AssemblyHO2,
            5,
            _items
        );

        request.Approve("Admin1");

        var oldEta = request.ExpectedFulfullmentTime;

        request.UpdatePriority(Priority.Urgent,"Admin");

        Assert.That(request.Priority, Is.EqualTo(Priority.Urgent));
        Assert.That(request.EstimatedDuration, Is.InRange(5,15));
        Assert.That(request.ExpectedFulfullmentTime, Is.Not.EqualTo(oldEta));
    }

    [Test]
    public void UpdatePriority_WhenNotApproved_ShouldNotUpdateEta()
    {
        StockRequest request = new StockRequest(
            "Jonas",
            Priority.LOW,
            Building.HallC,
            5,
            _items
        );

        request.UpdatePriority(Priority.Urgent,"Admin");

        Assert.That(request.Priority, Is.EqualTo(Priority.Urgent));
        Assert.That(request.ExpectedFulfullmentTime, Is.Null);
    }

    [Test]
    public void GenerateRequestName_ShouldReturnCorrectFormat()
    {
        StockRequest request = new StockRequest(
            "John",
            Priority.MEDIUM,
            Building.HallB,
            7,
            _items
        );

        var result = request.GenerateRequestName();

        Assert.That(result,Is.EqualTo("HallB|7_4_MEDIUM"));
    }

    [TestCase(Priority.LOW,40,60)]
    [TestCase(Priority.MEDIUM,20,40)]
    [TestCase(Priority.Urgent,5,15)]
    public void GetDurationFromPriority_ShouldReturnExpectedRange(
        Priority priority,
        int min, 
        int max
    )
    {
        int duration = StockRequest.GetDurationFromPriority(priority);

        Assert.That(duration, Is.InRange(min,max));
    }
}
