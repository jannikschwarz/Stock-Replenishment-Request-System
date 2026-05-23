using StockReplenishmentAPI.Models;

namespace StockReplenishmentAPI.DTOs; 

public class CreateStockRequestDTO
{
    public string Worker { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public List<MaterialItem> Items { get; set; } = [];
    public Building Building { get; set;}
    public int Table { get; set;}
}

public class DenyRequestDTO
{
    public string Admin {get;set;} = string.Empty;
    public string Reason {get;set;} = string.Empty;
}

public class PriorityDTO
{
    public string Admin {get;set;} = string.Empty;
    public Priority Priority {get;set;}
}