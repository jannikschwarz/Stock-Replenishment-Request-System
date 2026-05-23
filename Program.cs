using Microsoft.EntityFrameworkCore;
using StockReplenishmentAPI.Models;
using StockReplenishmentAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Initiate in memory Database
builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseInMemoryDatabase("StockDb"); 
});

builder.Services.AddSingleton<RequestQueueService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
