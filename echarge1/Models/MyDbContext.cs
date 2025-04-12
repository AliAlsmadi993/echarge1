using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace echarge1.Models;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<BlacklistedUser> BlacklistedUsers { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<ChargingBooking> ChargingBookings { get; set; }

    public virtual DbSet<ChargingStation> ChargingStations { get; set; }

    public virtual DbSet<ContactMessage> ContactMessages { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseEnrollment> CourseEnrollments { get; set; }

    public virtual DbSet<DataTransaction> DataTransactions { get; set; }

    public virtual DbSet<HomeContent> HomeContents { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PagesContent> PagesContents { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<PromotionRequest> PromotionRequests { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ServiceCard> ServiceCards { get; set; }

    public virtual DbSet<Testimonial> Testimonials { get; set; }

    public virtual DbSet<UserDatum> UserData { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-0QT3U0S;Database=EChargeDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__719FE4E81894B7CB");

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Admins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Admins__UserID__70DDC3D8");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACE6B1FDF8");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105340F7829F4").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.ProfileImage).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.UserType)
                .HasMaxLength(20)
                .HasDefaultValue("Customer");
            entity.Property(e => e.WarningCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<BlacklistedUser>(entity =>
        {
            entity.HasKey(e => e.BlacklistId).HasName("PK__Blacklis__AFDBF418BE7B2D3B");

            entity.Property(e => e.BlockedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.BlacklistedUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Blacklist__UserI__56E8E7AB");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B2A5268EE27");

            entity.Property(e => e.CartItemId).HasColumnName("CartItemID");
            entity.Property(e => e.DateAdded)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Product");

            entity.HasOne(d => d.User).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_User");
        });

        modelBuilder.Entity<ChargingBooking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Charging__73951ACD8378364F");

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.ChargerType)
                .HasMaxLength(50)
                .HasDefaultValue("Type1");
            entity.Property(e => e.StationId).HasColumnName("StationID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Station).WithMany(p => p.ChargingBookings)
                .HasForeignKey(d => d.StationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChargingB__Stati__412EB0B6");

            entity.HasOne(d => d.User).WithMany(p => p.ChargingBookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChargingB__UserI__403A8C7D");
        });

        modelBuilder.Entity<ChargingStation>(entity =>
        {
            entity.HasKey(e => e.StationId).HasName("PK__Charging__E0D8A6DD6089EC27");

            entity.Property(e => e.StationId).HasColumnName("StationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PricePerCharge).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__ContactM__C87C037C543C58B2");

            entity.Property(e => e.MessageId).HasColumnName("MessageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.Subject).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ContactMessages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ContactMe__UserI__73BA3083");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D71875D484887");

            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Instructor).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__CourseEn__7F6877FBD5D54C72");

            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CvPath).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EnrollmentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdPath).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseEnrollments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__CourseEnr__Cours__49C3F6B7");

            entity.HasOne(d => d.User).WithMany(p => p.CourseEnrollments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__CourseEnr__UserI__48CFD27E");
        });

        modelBuilder.Entity<DataTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__DataTran__55433A4B1C6E2DE4");

            entity.Property(e => e.TransactionId)
                .ValueGeneratedNever()
                .HasColumnName("TransactionID");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.BuyerId).HasColumnName("BuyerID");
            entity.Property(e => e.DataId).HasColumnName("DataID");
            entity.Property(e => e.DateOfTransaction).HasColumnType("datetime");
            entity.Property(e => e.SellerId).HasColumnName("SellerID");
        });

        modelBuilder.Entity<HomeContent>(entity =>
        {
            entity.HasKey(e => e.ContentId).HasName("PK__HomeCont__2907A87E3048E0C7");

            entity.ToTable("HomeContent");

            entity.Property(e => e.ContentId).HasColumnName("ContentID");
            entity.Property(e => e.ButtonText).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");
            entity.Property(e => e.Section).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF4EDF650F");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Orders__UserID__52593CB8");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED06A184E6CF55");

            entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__OrderItem__Order__5812160E");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__OrderItem__Produ__59063A47");
        });

        modelBuilder.Entity<PagesContent>(entity =>
        {
            entity.HasKey(e => e.PageId).HasName("PK__PagesCon__C565B124A4919C1E");

            entity.ToTable("PagesContent");

            entity.Property(e => e.PageId).HasColumnName("PageID");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");
            entity.Property(e => e.PageName).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A5880D00B9F");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payments__OrderI__7C4F7684");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Payments__UserID__7B5B524B");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED33741587");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Discount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("ImageURL");
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PromotionImageUrl).HasMaxLength(255);
            entity.Property(e => e.PromotionText).HasMaxLength(255);
            entity.Property(e => e.ProviderId).HasColumnName("ProviderID");

            entity.HasOne(d => d.Provider).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProviderId)
                .HasConstraintName("FK_Products_Provider");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F4ECCE26077E");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductImages_Products");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__ProductR__74BC79AE17EDF013");

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Rating).HasColumnType("decimal(2, 1)");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductReviews_Products");

            entity.HasOne(d => d.User).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ProductReviews_Users");
        });

        modelBuilder.Entity<PromotionRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Promotio__3214EC077F7FC279");

            entity.Property(e => e.AmountPaid).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ApprovedUntil).HasColumnType("datetime");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Product).WithMany(p => p.PromotionRequests)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromotionRequest_Product");

            entity.HasOne(d => d.Provider).WithMany(p => p.PromotionRequests)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromotionRequest_Provider");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79AE7E7EC0A7");

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Reviews__UserID__6B24EA82");
        });

        modelBuilder.Entity<ServiceCard>(entity =>
        {
            entity.HasKey(e => e.ServiceCardId).HasName("PK__ServiceC__C62024018E03AFBA");

            entity.Property(e => e.ServiceCardId).HasColumnName("ServiceCardID");
            entity.Property(e => e.ButtonLink).HasMaxLength(255);
            entity.Property(e => e.ButtonText).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("ImageURL");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Testimonial>(entity =>
        {
            entity.HasKey(e => e.TestimonialId).HasName("PK__Testimon__91A23E731D62BD8C");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.Testimonials)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Testimonials_AppUsers");
        });

        modelBuilder.Entity<UserDatum>(entity =>
        {
            entity.HasKey(e => e.UserDataId).HasName("PK__UserData__6EE7A00FD5751EB7");

            entity.Property(e => e.UserDataId)
                .ValueGeneratedNever()
                .HasColumnName("UserDataID");
            entity.Property(e => e.DataType)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DataValue).HasColumnType("text");
            entity.Property(e => e.DateCollected).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
