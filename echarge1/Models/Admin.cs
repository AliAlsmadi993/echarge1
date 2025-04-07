using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public int? UserId { get; set; }

    public string Role { get; set; } = null!;

    public virtual AppUser? User { get; set; }
}
