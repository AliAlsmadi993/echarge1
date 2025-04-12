using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public string? ReceiptImageUrl { get; set; }

    public string? ShippingAddress { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual AppUser? User { get; set; }
}
