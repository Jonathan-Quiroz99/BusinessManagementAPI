using System.ComponentModel.DataAnnotations;

namespace BusinessManagementAPI.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 9999.99)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public List<OrderItem> OrderItems { get; set; } = new();
}