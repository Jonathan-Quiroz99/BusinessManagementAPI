using System.ComponentModel.DataAnnotations;

namespace BusinessManagementAPI.DTOs;

public class UpdateOrderDto
{
    public bool IsActive { get; set; }

    [Required]
    [MinLength (1)]
    public List<UpdateOrderItems> Items { get; set; } = new();
}

public class UpdateOrderItems
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
