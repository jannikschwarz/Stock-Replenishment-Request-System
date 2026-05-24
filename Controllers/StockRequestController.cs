using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockReplenishmentAPI.DTOs;
using StockReplenishmentAPI.Models;
using StockReplenishmentAPI.Services;

namespace StockReplenishmentAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class StockRequestController : ControllerBase
{
    private readonly StockDbContext _db;
    private readonly RequestQueueService _queue; 

    public StockRequestController(
        StockDbContext db,
        RequestQueueService queue
    )
    {
        _db = db; 
        _queue = queue;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateStockRequestDTO dto
    )
    {
        var request = new StockRequest(
            dto.Worker,
            dto.Priority,
            dto.Building,
            dto.Table,
            dto.Items
        );

        _db.StockRequests.Add(request);

        await _db.SaveChangesAsync();

        var dbId = _db.ContextId;
        Console.WriteLine(dbId);
        

        await _queue.QueueRequest(request.Id);

        return Ok(request);
    }

    [HttpGet(Name = "Get")]
    public async Task<IActionResult> GetRequest(
        [FromQuery] Guid id
    )
    {
        var request = await _db.StockRequests.FindAsync(id);

        if(request == null)
            return NotFound();

        return Ok(request);
    }

    [HttpGet(Name = "GetAll")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int status
    )
    {
        if(status < 0 || status > 4)
            return Ok(await _db.StockRequests.ToListAsync());
        return Ok(await _db.StockRequests.Where(x => x.Status == (RequestStatus)status).ToListAsync());
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromQuery] string admin
    )
    {
        var request = await _db.StockRequests.FindAsync(id);

        if(request == null)
            return NotFound();

        request.Approve(admin);

        await _db.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPost("{id}/deny")]
    public async Task<IActionResult> Deny(
        Guid id,
        DenyRequestDTO dto
    )
    {
        var request = await _db.StockRequests.FindAsync(id);

        if(request == null)
            return NotFound();

        request.Deny(dto.Admin,dto.Reason);

        await _db.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPost("{id}/priority")]
    public async Task<IActionResult> Priority(
        Guid id,
        PriorityDTO dto
    )
    {
        var request = await _db.StockRequests.FindAsync(id);

        if(request == null)
            return NotFound();

        request.UpdatePriority(dto.Priority,dto.Admin);

        await _db.SaveChangesAsync();

        return Ok(request);
    }
}