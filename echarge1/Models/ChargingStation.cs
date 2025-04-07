using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class ChargingStation
{
    public int StationId { get; set; }

    public string Name { get; set; } = null!;

    public string Location { get; set; } = null!;

    public int Capacity { get; set; }

    public int AvailableSpots { get; set; }

    public decimal PricePerCharge { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChargingBooking> ChargingBookings { get; set; } = new List<ChargingBooking>();
}
