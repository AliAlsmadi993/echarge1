using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class ServiceCard
{
    public int ServiceCardId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string ButtonText { get; set; } = null!;

    public string ButtonLink { get; set; } = null!;
}
