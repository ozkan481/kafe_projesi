namespace CoffeeShopApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = "Kahve";
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
