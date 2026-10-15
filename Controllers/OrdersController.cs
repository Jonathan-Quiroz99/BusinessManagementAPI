using BusinessManagementAPI.Data;
using BusinessManagementAPI.DTOs;
using BusinessManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList(),
                Total = order.Items.Sum(i => i.UnitPrice * i.Quantity)
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
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList(),
                Total = order.Items.Sum(i => i.UnitPrice * i.Quantity)
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Order",
                Detail = "An order cannot contain duplicate products."
            });
        }

        //gets existing products from database fro future validations
        var existingProducts = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.IsActive,
                p.Price
            }).ToListAsync();

        //validates if incoming products exist in database
        if (productIds.Any(id => !existingProducts.Any(p => p.Id == id)))
        {
            var missing = productIds
                .Where(id => !existingProducts.Any(p => p.Id == id)).ToList();

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Products not found in Database",
                Detail = $"Product {((missing.Count > 1) ? "IDs" : "ID")} {string.Join(", ", missing)} {((missing.Count > 1) ? "are" : "is")} invalid or missing."
            });
        }

        //validates if incoming products are active
        if(existingProducts.Any(p => !p.IsActive))
        {
            var inactive = existingProducts.Where(p => !p.IsActive).Select(p => p.Id).ToList();

            return BadRequest(new ProblemDetails 
            { Status = StatusCodes.Status400BadRequest, 
              Title = "Inactive Products",
              Detail = $"Product {((inactive.Count > 1) ? "IDs" : "ID")} {string.Join(", ", inactive)} {((inactive.Count > 1) ? "are" : "is")} unavailable." 
            });
        }

        //creates order using dto for order items
        var order = new Order
        {
            Items = orderDto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = existingProducts.First(p => p.Id == item.ProductId).Price
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
                Quantity = item.Quantity,
                UnitPrice = existingProducts.First(p => p.Id == item.ProductId).Price
            }).ToList(),
            Total = order.Items.Sum(i => i.UnitPrice * i.Quantity)
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Order",
                Detail = "An order cannot contain duplicate products."
            });
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
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Inactive Order",
                Detail = "cannot modify inactive orders" }
            );
        }

        //gets existing products from database to compare incoming products existence and availability
        var existingProducts = await _context.Products
            .Where(p => productsIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.IsActive,
                p.Price
            }).ToListAsync();

        //validates if products exist in database 
        if (productsIds.Any(id => !existingProducts.Any(p => p.Id == id)))
        {
            var missingProductIds = productsIds
                .Where(id => !existingProducts.Any(p => p.Id == id)).ToList();

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Products not found in Database",
                Detail = $"Product {((missingProductIds.Count > 1) ? "IDs" : "ID")} {string.Join(", ", missingProductIds)} {((missingProductIds.Count > 1) ? "are" : "is")} invalid or missing."
            });
        }

        var inactiveids = existingProducts.Where(p => !p.IsActive).Select(p => p.Id).ToList();

        var existingOrderProductIds = order.Items
            .Select(i => i.ProductId)
            .ToList();

        var incomingNewInactive = inactiveids.Where(id => !existingOrderProductIds.Contains(id)).ToList();

        //validates if any inactive products are new to the existing order, there are any new incoming products
        //reject order
        if (incomingNewInactive.Any())
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Inactive Products",
                Detail = $"Products {((incomingNewInactive.Count > 1) ? "IDs" : "ID")} {string.Join(", ", incomingNewInactive)} {((incomingNewInactive.Count > 1) ? "are" : "is")} unavailable"
            });
        }

        // Keep the original order items before replacing them
        var originalItems = order.Items.ToList();

        //remove existing order items
        _context.OrderItems.RemoveRange(order.Items);

        //clear current collection
        order.Items.Clear();

        //update order status
        order.IsActive = UpdatedOrder.IsActive;

        // Add the new order items
        foreach (var item in UpdatedOrder.Items)
        {
            decimal unitPrice;

            // captures original items that are already in existing item
            var originalItem = originalItems.FirstOrDefault(i => i.ProductId == item.ProductId);

            //if item exist in order, use unitprice from existing order
            if (originalItem != null)
            {
                unitPrice = originalItem.UnitPrice;
            }
            else
            {
                // if there is a new item use unitprice from database
                unitPrice = existingProducts
                    .First(p => p.Id == item.ProductId)
                    .Price;
            }

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice
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
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            Total = order.Items.Sum(i => i.UnitPrice * i.Quantity)
        };

        return Ok(response);
    }

    //Delete order using given id
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        //find order with id
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if(order == null)
        {
            return NotFound();
        }

        if(!order.IsActive)
        {
            return BadRequest("Order already inactive");
        }

        ////set order as inactive
        order.IsActive = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}