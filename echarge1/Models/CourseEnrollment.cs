using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class CourseEnrollment
{
    public int EnrollmentId { get; set; }

    public int? UserId { get; set; }

    public int? CourseId { get; set; }

    public DateTime? EnrollmentDate { get; set; }

    public string? Status { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? AdditionalInfo { get; set; }

    public string? CvPath { get; set; }

    public string? IdPath { get; set; }

    public virtual Course? Course { get; set; }

    public virtual AppUser? User { get; set; }
}
