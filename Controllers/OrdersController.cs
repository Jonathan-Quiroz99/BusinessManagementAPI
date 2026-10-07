using BusinessManagementAPI.Data;
using BusinessManagementAPI.Models;
using BusinessManagementAPI.DTOs;
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
            //makes sure only correct information shows using dto
            .Select(order => new OrderResponseDto
            {
                Id = order.Id,
                Date = order.Date,
                IsActive = order.IsActive,
                Items = order.Items.Select(item => new OrderItemResponseDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            })
            .ToListAsync();


        return Ok(orders);
    }

    //Create new order
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDto orderDto)
    {
        //creates order using dto for order items
        var order = new Order
        {
            Items = orderDto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        // Add the order to the database
        _context.Orders.Add(order);

        // Save the order and its related items
        await _context.SaveChangesAsync();

        //create response for order to show dto version of order items
        var response = new OrderResponseDto
        {
            Id = order.Id,
            Date = order.Date,
            IsActive = order.IsActive,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, response);
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetOrderItems()
    {
        var items = await _context.OrderItems.ToListAsync();

        return Ok(items);
    }
}