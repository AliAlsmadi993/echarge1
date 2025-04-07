using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class Testimonial
{
    public int TestimonialId { get; set; }

    public int? UserId { get; set; }

    public int MessageId { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
}
