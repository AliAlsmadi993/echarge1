using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? UserId { get; set; }

    public int? OrderId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal AmountPaid { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual AppUser? User { get; set; }
}
