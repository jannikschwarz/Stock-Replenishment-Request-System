using StockReplenishmentAPI.Models;

namespace StockReplenishmentAPI.Services;

public static class SeedStockRequestService
{
    private static readonly string[] Workers =
    [
        "John","Mike","Jannik","Olaf","Anna","Sara","Karin"
    ];

    private static readonly string[] Admins = [
        "Raju","Jonas","Jannik"
    ];

    private static readonly string[] Materials = [
        "Bolts","Screws","Hydraulic Pipe","Motor component","Steel plate","Cable Harness","ARC Extinguisher",
        "Handle","Switch","Display","Tripper","Contact","Coil"
    ];
    
    private static readonly string[] DenialReasons = [
        "Out of stock","Unauthorized","Duplicate request","Invalid article number","Item discontinued","Incorrect quantity"
    ];

    public static async Task SeedAsync(
        StockDbContext db,
        RequestQueueService queue
    )
    {
        if(db.StockRequests.Any())
            return;

        Random random = new ();
        Priority[] priorities = Enum.GetValues<Priority>();
        Building[] buildings = Enum.GetValues<Building>();

        int numberOfRequests = random.Next(9,15);
        for(int i = 0; i < numberOfRequests; i++)
        {
            int itemCount = random.Next(1,8);

            List<MaterialItem> materialItems = [];

            for(int j = 0; j < itemCount; j++)
            {
                int materialNumber = random.Next(Materials.Length);

                materialItems.Add(new MaterialItem
                {
                   ArticleNumber = materialNumber,
                   Description = Materials[materialNumber],
                   Quantity = random.Next(1,50)
                });
            }

            StockRequest request = new(
                worker: Workers[random.Next(Workers.Length)],
                priority: priorities[random.Next(priorities.Length)],
                building: buildings[random.Next(buildings.Length)],
                table: random.Next(1,10),
                items: materialItems
            );

            double stateChance = random.NextDouble();
            string admin = Admins[random.Next(Admins.Length)];

            if(stateChance <= 0.6)
            {
                request.Approve(admin);
                request.Logs.Add("System: Seeded as approved request");
            }
            else if(stateChance <= 0.8)
            {
                string reason = DenialReasons[random.Next(DenialReasons.Length)];
                request.Deny(admin,reason);
                request.Logs.Add("System: Seeded as denied request");
            }

            db.StockRequests.Add(request);
        }

        await db.SaveChangesAsync();

        foreach(StockRequest request in db.StockRequests)
        {
            if(request.Status != RequestStatus.Denied)
            {
                await queue.QueueRequest(request.Id);
            }
        }
    }
}