using Microsoft.EntityFrameworkCore;
using StockReplenishmentAPI.Models;
using StockReplenishmentAPI.Services;

var builder = WebApplication.CreateBuilder(args);

//Set services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseInMemoryDatabase("StockDb"); 
});
builder.Services.AddSingleton<RequestQueueService>();
builder.Services.AddHostedService<StockRequestProcessor>();

var app = builder.Build();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using(IServiceScope scope = app.Services.CreateScope())
{
    StockDbContext db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    RequestQueueService queue = scope.ServiceProvider.GetRequiredService<RequestQueueService>();

    await SeedStockRequestService.SeedAsync(db,queue);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();