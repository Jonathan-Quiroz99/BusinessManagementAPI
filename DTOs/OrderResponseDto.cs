namespace BusinessManagementAPI.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public bool IsActive { get; set; }

    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}