using System.ComponentModel.DataAnnotations.Schema;

namespace StockReplenishmentAPI.Models;

// Stock replenishment request priority
public enum Priority
{
    LOW,
    MEDIUM,
    Urgen
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
    Fulfilled
}

public class MaterialItem
{
    public int Id { get; set; }
    public int ArticleNumber { get; set;}
    public string Description {get;set;} = string.Empty;
    public int Quantity {get;set;}
}


public abstract class StockRequest
{
    public Guid Id {get;set;} = Guid.NewGuid();

    public string Worker { get; }
    public Priority Priority { get; private set; }
    public List<MaterialItem> Items { get; set; } = [];
    public Building Building { get; set;}
    public int Table { get; set;}
    public List<string> Logs {get;set;} = [];
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public RequestStatus Status { get; set; } = RequestStatus.PendingReview;

    public string? DenialReason {get;set;}

    public StockRequest(){}

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

    public void UpdatePriority(Priority priority, string worker)
    {
        Priority = priority;
        Logs.Add($"{worker}: Updated priority of {GenerateRequestName()}");
    }

    public string GenerateRequestName()
    {
        return $"{Building}|{Table}_{Items.Count}_{Priority}";
    }
    #endregion
}
