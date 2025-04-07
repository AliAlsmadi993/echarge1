using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class HomeContent
{
    public int ContentId { get; set; }

    public string Section { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? ButtonText { get; set; }

    public string? ButtonLink { get; set; }
}
