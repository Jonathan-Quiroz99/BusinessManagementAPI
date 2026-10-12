using System.ComponentModel.DataAnnotations;

namespace BusinessManagementAPI.DTOs;

public class CreateOrderDto
{
    [Required]
    [MinLength (1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

