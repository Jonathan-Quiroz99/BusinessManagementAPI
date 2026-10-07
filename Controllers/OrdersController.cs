using BusinessManagementAPI.Data;
using BusinessManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    //Get all orders
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ToListAsync();

        return Ok(orders);
    }

    //Create new order
    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        // Add the order to the database
        _context.Orders.Add(order);

        // Save the order and its related items
        await _context.SaveChangesAsync();

        //
        return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetOrderItems()
    {
        var items = await _context.OrderItems.ToListAsync();

        return Ok(items);
    }
}