using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class EvsupportUnit
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string? VehicleType { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }
}
