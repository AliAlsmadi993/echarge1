using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? UserId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
}
