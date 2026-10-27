namespace CoffeeShopApp.Models
{
    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => Product.Price * Quantity;

        public CartItem(Product product, int quantity = 1)
        {
            Product = product;
            Quantity = quantity;
        }
    }
}
