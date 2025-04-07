using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class ContactMessage
{
    public int MessageId { get; set; }

    public int? UserId { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
}
