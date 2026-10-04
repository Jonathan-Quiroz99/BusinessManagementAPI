using BusinessManagementAPI.Data;
using BusinessManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Dependency injection of the AppDbContext
    // This allows the controller to interact with the database
    private readonly AppDbContext _context;

    // Constructor for the ProductsController class
    // The AppDbContext is injected into the controller through the constructor
    public ProductsController(AppDbContext context)
    {
        // Assign the injected AppDbContext to the private field
        // This allows the controller to use the database context for data access
        _context = context;
    }

    // In-memory list of products for demonstration purposes
    private static readonly List<Product> Products = new()
    {
        // Sample products for demonstration purposes
        new Product
        {
            Id = 1,
            Name = "Chocolate Cake",
            Description = "Chocolate cake with vanilla filling",
            Price = 450,
            IsActive = true
        },
        new Product
        {
            Id = 2,
            Name = "Cheesecake",
            Description = "Classic cheesecake with strawberry topping",
            Price = 380,
            IsActive = true
        }
    };

    // GET api/products
    [HttpGet]
    // This method retrieves all products from the database asynchronously
    public async Task<IActionResult> GetAll()
    {
        // Retrieve all products from the database using Entity Framework Core
        // The ToListAsync method is used to execute the query and return the results as a list
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    // GET api/products/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Retrieve a product by its ID from the database asynchronously
        // The FindAsync method is used to find the product with the specified ID
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    // POST api/products
    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        // Add the product to the database context
        _context.Products.Add(product);
        // Save the changes to the database asynchronously
        await _context.SaveChangesAsync();

        // Return a 201 Created response with the location of the newly created product
        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product
            );
    }

    // PUT api/products/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product updatedProduct)
    {
        // Find the product by ID
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        // Update the product properties
        product.Name = updatedProduct.Name;
        product.Description = updatedProduct.Description;
        product.Price = updatedProduct.Price;
        product.IsActive = updatedProduct.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Find the product by ID
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        // Remove the product
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}