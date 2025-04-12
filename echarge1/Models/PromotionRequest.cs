using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class PromotionRequest
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int ProviderId { get; set; }

    public DateTime RequestDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ApprovedUntil { get; set; }

    public decimal AmountPaid { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual AppUser Provider { get; set; } = null!;
}
