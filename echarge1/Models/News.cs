using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class News
{
    public int NewsId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }

    public DateTime? PublishDate { get; set; }

    public string? Status { get; set; }
}
