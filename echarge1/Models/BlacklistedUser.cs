using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class BlacklistedUser
{
    public int BlacklistId { get; set; }

    public int UserId { get; set; }

    public string? Reason { get; set; }

    public DateTime? BlockedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
