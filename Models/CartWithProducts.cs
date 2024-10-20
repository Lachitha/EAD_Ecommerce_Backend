public class CartWithProducts
{
    public string UserId { get; set; }
    public List<CartItemWithProductDetails> Items { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CartItemWithProductDetails
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public string ProductName { get; set; } // Include the product name
    public string ProductDescription { get; set; } // Include the product description
    public string ProductImageUrl { get; set; } // Include the product image URL
}

public class AddToCartRequest
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveFromCartRequest
{
    public string ProductId { get; set; }
}

public class UpdateQuantityRequest
{
    public string ProductId { get; set; }
    public int NewQuantity { get; set; }
}
