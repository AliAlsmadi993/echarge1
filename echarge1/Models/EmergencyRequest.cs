using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class EmergencyRequest
{
    public int EmergencyRequestId { get; set; }

    public int UserId { get; set; }

    public string RequestType { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? RequestTime { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<EmergencyMessage> EmergencyMessages { get; set; } = new List<EmergencyMessage>();

    public virtual AppUser User { get; set; } = null!;
}
