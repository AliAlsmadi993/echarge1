using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class PagesContent
{
    public int PageId { get; set; }

    public string PageName { get; set; } = null!;

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? ImageUrl { get; set; }
}
