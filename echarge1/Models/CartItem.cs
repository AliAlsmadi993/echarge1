using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime? DateAdded { get; set; }

    public bool IsCheckedOut { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
