using System.ComponentModel.DataAnnotations;

namespace BusinessManagementAPI.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public bool IsActive { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemResponseDto> Items { get; set; } = new();

    public decimal Total { get; set; }
}

public class OrderItemResponseDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}