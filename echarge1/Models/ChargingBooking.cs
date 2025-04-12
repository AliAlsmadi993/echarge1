using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class ChargingBooking
{
    public int BookingId { get; set; }

    public int? UserId { get; set; }

    public int? StationId { get; set; }

    public DateTime BookingDate { get; set; }

    public string? Status { get; set; }

    public string ChargerType { get; set; } = null!;

    public virtual ChargingStation? Station { get; set; }

    public virtual AppUser? User { get; set; }
}
