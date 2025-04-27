using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class EmergencyMessage
{
    public int EmergencyMessageId { get; set; }

    public int EmergencyRequestId { get; set; }

    public int SenderId { get; set; }

    public string MessageText { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public virtual EmergencyRequest EmergencyRequest { get; set; } = null!;

    public virtual AppUser Sender { get; set; } = null!;
}
