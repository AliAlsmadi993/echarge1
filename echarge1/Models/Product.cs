using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public decimal? Discount { get; set; }

    public bool IsAvailable { get; set; }

    public string? Category { get; set; }

    public int? ProviderId { get; set; }

    public bool? IsFeatured { get; set; }

    public string? PromotionText { get; set; }

    public string? PromotionImageUrl { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual AppUser? Provider { get; set; }
}
