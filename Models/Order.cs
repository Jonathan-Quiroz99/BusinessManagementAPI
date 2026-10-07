namespace BusinessManagementAPI.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime Date {  get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    public List<OrderItem> Items { get; set; } = new();
}
