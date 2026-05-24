namespace StockReplenishmentAPI.Models;

// Stock replenishment request priority
public enum Priority
{
    LOW,
    MEDIUM,
    Urgent
}

public enum Building
{
    HallA,
    HallB,
    HallC,
    AssemblyHO1,
    AssemblyHO2,
}

public enum RequestStatus
{
    PendingReview,
    Approved,
    Denied,
    Fulfilled,
    Failed
}

public class MaterialItem
{
    public int Id { get; set; }
    public int ArticleNumber { get; set;}
    public string Description {get;set;} = string.Empty;
    public int Quantity {get;set;}
}


public class StockRequest
{
    public Guid Id {get;set;} = Guid.NewGuid();

    public string Worker { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public List<MaterialItem> Items { get; set; } = [];
    public Building Building { get; set;}
    public int Table { get; set;}
    public List<string> Logs {get;set;} = [];
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt {get; set;}
    public DateTime? ExpectedFulfullmentTime {get;set;}
    public int EstimatedDuration {get;set;}

    public RequestStatus Status { get; set; } = RequestStatus.PendingReview;

    public string? DenialReason {get;set;}

    private StockRequest()
    {
    }

    public StockRequest(
        string worker,
        Priority priority,
        Building building,
        int table,
        List<MaterialItem> items
    )
    {
        Worker = worker;
        Priority = priority;
        Building = building;
        Table = table; 
        Items = items; 

        Logs.Add($"{worker}: Created new stock replenish request of {Items.Count} for {building}/{table}; [{priority}]");
    }

    #region Public methods
    public void Approve(string admin)
    {
        Status = RequestStatus.Approved;

        ApprovedAt = DateTime.UtcNow;

        EstimatedDuration = GetDurationFromPriority(Priority);

        ExpectedFulfullmentTime = ApprovedAt.Value.AddSeconds(EstimatedDuration);

        Logs.Add($"{admin}: Approved stock request");
    }

    public void Deny(string admin, string reason)
    {
        Status = RequestStatus.Denied;
        DenialReason = reason; 

        Logs.Add($"{admin}: Denied request, Reason: {reason}");
    }

    public void Fulfill()
    {
        Status = RequestStatus.Fulfilled;
        
        Logs.Add($"System: Request has been fulfilled");
    }

    public void UpdatePriority(Priority priority, string admin)
    {
        Priority = (Priority)priority;
        Logs.Add($"{admin}: Updated priority of {GenerateRequestName()} to {priority}");

        if(Status != RequestStatus.Approved)
            return;
        
        int newDuration = GetDurationFromPriority(priority);

        EstimatedDuration = newDuration;

        ExpectedFulfullmentTime = DateTime.UtcNow.AddSeconds(newDuration);

        Logs.Add("System: Fulfillment ETA update");
    }

    public string GenerateRequestName()
    {
        return $"{Building}|{Table}_{Items.Count}_{Priority}";
    }
    #endregion

    public static int GetDurationFromPriority(Priority priority)
    {
        return priority switch
        {
            Priority.LOW => Random.Shared.Next(40,60),
            Priority.MEDIUM => Random.Shared.Next(20,40),
            Priority.Urgent => Random.Shared.Next(5,15),
            _ => 30
        };
    }
}
