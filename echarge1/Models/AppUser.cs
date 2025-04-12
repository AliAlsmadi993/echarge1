using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class AppUser
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string UserType { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public string? ProfileImage { get; set; }

    public DateTime? LastLogin { get; set; }

    public int? WarningCount { get; set; }

    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

    public virtual ICollection<BlacklistedUser> BlacklistedUsers { get; set; } = new List<BlacklistedUser>();

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<ChargingBooking> ChargingBookings { get; set; } = new List<ChargingBooking>();

    public virtual ICollection<ContactMessage> ContactMessages { get; set; } = new List<ContactMessage>();

    public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<PromotionRequest> PromotionRequests { get; set; } = new List<PromotionRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Testimonial> Testimonials { get; set; } = new List<Testimonial>();
}
