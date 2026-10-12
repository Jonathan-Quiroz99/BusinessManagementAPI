using BusinessManagementAPI.Data;
using BusinessManagementAPI.DTOs;
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

    //Get order with id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _context.Orders.Where(O => O.Id == id)
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
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    //Create new order
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDto orderDto)
    {
        //captures product id's from incoming order
        var productIds = orderDto.Items
            .Select(item => item.ProductId)
            .ToList();

        //validate any duplicated products
        if (productIds.Count != productIds.Distinct().Count())
        {
            return BadRequest("An order cannot contain duplicate products.");
        }

        //gets existing products from database fro future validations
        var existingProducts = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.IsActive
            }).ToListAsync();

        //validates if incoming products exist in database
        if (productIds.Any(id => !existingProducts.Any(p => p.Id == id)))
        {
            var missing = productIds
                .Where(id => !existingProducts.Any(p => p.Id == id)).ToList();

            return BadRequest($"Product {((missing.Count > 1) ? "IDs" : "ID")} {string.Join(", ", missing)} {((missing.Count > 1) ? "are" : "is")} invalid or missing.");
        }

        //validates if incoming products are active
        if(existingProducts.Any(p => !p.IsActive))
        {
            var inactive = existingProducts.Where(p => !p.IsActive).Select(p => p.Id).ToList();

            return BadRequest($"Product {((inactive.Count > 1) ? "IDs" : "ID")} {string.Join(", ", inactive)} {((inactive.Count > 1) ? "are" : "is")} unavailable.");
        }        

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

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, response);
    }

    //updates order using orderId
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDto UpdatedOrder)
    {
        //captures product id's from incoming order
        var productsIds = UpdatedOrder.Items
            .Select(item => item.ProductId)
            .ToList();

        //validates if there are any duplicated products
        if (productsIds.Count != productsIds.Distinct().Count())
        {
            return BadRequest("An order cannot contain duplicate products.");
        }

        //calls order from database to initiate update
        var order = await _context.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if(order == null)
        {
            return NotFound();
        }

        if(!order.IsActive)
        {
            return BadRequest("cannot modify inactive orders");
        }

        //gets existing orders from database to compare incoming products existence and availability
        var existingProducts = await _context.Products
            .Where(p => productsIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.IsActive
            }).ToListAsync();

        //validates if products exist in database 
        if (productsIds.Any(id => !existingProducts.Any(p => p.Id == id)))
        {
            var missingProductIds = productsIds
                .Where(id => !existingProducts.Any(p => p.Id == id)).ToList();

            return BadRequest($"Product {((missingProductIds.Count > 1) ? "IDs" : "ID")} {string.Join(", ", missingProductIds)} {((missingProductIds.Count > 1) ? "are" : "is")} invalid or missing.");
        }

        //validates if products are active
        if(existingProducts.Any(p => !p.IsActive))
        {
            var inactive = existingProducts.Where(p => !p.IsActive).Select(p => p.Id).ToList();

            return BadRequest($"Products {((inactive.Count > 1) ? "IDs" : "ID")} {string.Join(", ", inactive)} {((inactive.Count > 1)? "are" : "is")} unavailable");
        }

        //remove existing order items
        _context.OrderItems.RemoveRange(order.Items);

        //clear current collection
        order.Items.Clear();

        //update order status
        order.IsActive = UpdatedOrder.IsActive;

        // Add the new order items
        foreach (var item in UpdatedOrder.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });
        }

        //save changes
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

        return Ok(response);
    }

    //Delete order using given id
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        //find order with id
        var order = await _context.Orders.FindAsync(id);
        if(order == null)
        {
            return NotFound();
        }

        if(!order.IsActive)
        {
            return BadRequest("Unable to delete inactive orders");
        }

        return BadRequest("Unable to delete orders");

        ////delete order
        //_context.Orders.Remove(order);
        //await _context.SaveChangesAsync();
        //return NoContent();
    }
}