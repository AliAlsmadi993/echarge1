using echarge1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

public class AdminController : Controller
{
    private readonly MyDbContext _context;

    public AdminController(MyDbContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> AdminDashboard()
    {
        // الإحصائيات الرئيسية
        var totalUsers = await _context.AppUsers.CountAsync(u => u.UserType == "User");
        var totalProviders = await _context.AppUsers.CountAsync(u => u.UserType == "Provider");
        var totalProducts = await _context.Products.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalBookings = await _context.ChargingBookings.CountAsync();
        var totalCourses = await _context.Courses.CountAsync();

        // إرسال البيانات إلى الـ View عبر ViewBag
        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalProviders = totalProviders;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalBookings = totalBookings;
        ViewBag.TotalCourses = totalCourses;

        return View();
    }


    // ✅ تسجيل الخروج
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // حذف بيانات الجلسة
        return RedirectToAction("Login", "Account");
    }

    // Display all service cards
    public async Task<IActionResult> Index()
    {
        var serviceCards = await _context.ServiceCards.ToListAsync();
        return View(serviceCards);
    }

    // Create new service card
    public IActionResult Create()
    {
        return View();
    }

    // POST: Create new service card
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,ButtonText,ButtonLink")] ServiceCard serviceCard, IFormFile imageFile)
    {
        string imagesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        if (!Directory.Exists(imagesFolderPath))
        {
            Directory.CreateDirectory(imagesFolderPath);
        }

        if (imageFile != null && imageFile.Length > 0)
        {
            string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName) + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(imagesFolderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            serviceCard.ImageUrl = "/images/" + fileName;
        }

        _context.Add(serviceCard);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Edit an existing service card
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var serviceCard = await _context.ServiceCards.FindAsync(id);
        if (serviceCard == null)
        {
            return NotFound();
        }
        return View(serviceCard);
    }

    // POST: Edit an existing service card
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int ServiceCardId, [Bind("ServiceCardId,Title,Description,ImageUrl,ButtonText,ButtonLink")] ServiceCard serviceCard, IFormFile imageFile)
    {
        if (ServiceCardId != serviceCard.ServiceCardId)
        {
            return NotFound();
        }

        try
        {
            // Handle the image file upload if present
            if (imageFile != null && imageFile.Length > 0)
            {
                string imagesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                if (!Directory.Exists(imagesFolderPath))
                {
                    Directory.CreateDirectory(imagesFolderPath);
                }

                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName) + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(imagesFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                serviceCard.ImageUrl = "/images/" + fileName;
            }

            _context.Update(serviceCard);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceCardExists(serviceCard.ServiceCardId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(Index));
    }


    // Delete a service card
    // Action to handle delete request
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var serviceCard = await _context.ServiceCards.FindAsync(id);
        if (serviceCard == null)
        {
            return NotFound();
        }

        _context.ServiceCards.Remove(serviceCard);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ServiceCardExists(int id)
    {
        return _context.ServiceCards.Any(e => e.ServiceCardId == id);
    }




    public IActionResult Messages()
    {
        var messages = _context.ContactMessages.ToList();
        return View(messages);
    }

    // Display the reply page for a message
    public IActionResult ReplyMessage(int id)
    {
        var message = _context.ContactMessages.FirstOrDefault(m => m.MessageId == id);
        if (message == null)
            return NotFound();

        return View(message);
    }

    // Submit the reply
    [HttpPost]
    public async Task<IActionResult> ReplyMessage(int id, string replyContent)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        TempData["Success"] = "Reply sent successfully.";
        return RedirectToAction("Messages");
    }

    // Delete a message
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        _context.ContactMessages.Remove(message);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Message deleted successfully.";
        return RedirectToAction("Messages");
    }

    // Transfer message to testimonials
    public async Task<IActionResult> TransferToTestimonials(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        var testimonial = new Testimonial
        {
            UserId = message.UserId,
            MessageId = message.MessageId,
            Subject = message.Subject,
            Message = message.Message,
            CreatedAt = DateTime.Now
        };
        _context.Testimonials.Add(testimonial);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Message transferred to testimonials successfully.";
        return RedirectToAction("Testimonials");
    }

    // Display all testimonials
    public IActionResult Testimonials()
    {
        var testimonials = _context.Testimonials.ToList();
        return View(testimonials);
    }

    // Delete a testimonial
    public async Task<IActionResult> DeleteTestimonial(int id)
    {
        var testimonial = await _context.Testimonials.FindAsync(id);
        if (testimonial == null)
            return NotFound();

        _context.Testimonials.Remove(testimonial);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Testimonial deleted successfully.";
        return RedirectToAction("Testimonials");
    }



    /// corsess//////////////////////////
    /// 



    [HttpGet]
    public async Task<IActionResult> Courses()
    {
        var courses = await _context.Courses
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        return View(courses);
    }



    [HttpGet]
    public IActionResult AddCourse()
    {
        return View();
    }

    // ✅ حفظ الكورس الجديد
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCourse(Course course)
    {
        if (!ModelState.IsValid)
            return View(course);

        course.CreatedAt = DateTime.Now;

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Course added successfully.";
        return RedirectToAction("CourseApplications");
    }

    public async Task<IActionResult> CourseApplications(int id)
    {
        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == id)
            .Include(e => e.Course)
            .Include(e => e.User)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();

        ViewBag.CourseTitle = enrollments.FirstOrDefault()?.Course?.Title ?? "Course Applications";

        return View(enrollments);
    }

    public async Task<IActionResult> ViewApplication(int id)
    {
        var application = await _context.CourseEnrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EnrollmentId == id);

        if (application == null)
            return NotFound();

        return View(application);
    }


    [HttpPost]
    public async Task<IActionResult> UpdateApplicationStatus(int id, string status)
    {
        var application = await _context.CourseEnrollments.FindAsync(id);
        if (application == null)
            return NotFound();

        application.Status = status;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Application marked as {status}.";
        return RedirectToAction("CourseApplications");
    }

    // ✅ عرض كل المزودين
    public async Task<IActionResult> Providers()
    {
        var providers = await _context.AppUsers
            .Where(u => u.UserType == "Provider")
            .ToListAsync();

        return View(providers);
    }

    // ✅ عرض المنتجات للمراجعة أو الإدارة
    public async Task<IActionResult> AllProducts()
    {
        var allProducts = await _context.Products
            .Include(p => p.Provider)
            .Include(p => p.ProductImages)
            .ToListAsync();

        var siteProducts = allProducts.Where(p => p.ProviderId == null).ToList();
        var providerProducts = allProducts.Where(p => p.ProviderId != null).ToList();

        ViewBag.SiteProducts = siteProducts;
        ViewBag.ProviderProducts = providerProducts;

        return View(); // سنستخدم ViewBag بدلاً من Model مباشرةً
    }
    public async Task<IActionResult> SiteOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderItems.Any(oi => oi.Product.ProviderId == null)) // فقط المنتجات التابعة للموقع
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders); // تمرير البيانات إلى الـ View
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(int OrderId, string Status)
    {
        var order = await _context.Orders.FindAsync(OrderId);

        if (order == null)
        {
            return NotFound();
        }

        // تحديث حالة الطلب بناءً على الحالة المختارة من الـ Dropdown
        order.Status = Status;

        // حفظ التغييرات في قاعدة البيانات
        await _context.SaveChangesAsync();

        TempData["Success"] = "Order status updated successfully.";

        return RedirectToAction("SiteOrders");  // يمكنك توجيه المستخدم إلى الصفحة التي تحتوي على الطلبات
    }


    // ✅ الموافقة على الترويج
    [HttpPost]
    public async Task<IActionResult> ApprovePromotion(int requestId)
    {
        var request = await _context.PromotionRequests.FindAsync(requestId);
        if (request != null)
        {
            request.Status = "Approved";

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product != null)
            {
                product.IsFeatured = true;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Promotion approved successfully.";
        }

        return RedirectToAction("PromotionRequests");
    }

    // ✅ عرض كل طلبات الترويج
    public async Task<IActionResult> PromotionRequests()
    {
        var requests = await _context.PromotionRequests
            .Include(r => r.Product)
            .Include(r => r.Provider)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProvider(int id)
    {
        var provider = await _context.AppUsers
            .Include(u => u.Products)
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(u => u.UserId == id && u.UserType == "Provider");

        if (provider == null)
        {
            TempData["Error"] = "Provider not found.";
            return RedirectToAction("Providers");
        }

        // حذف صور المنتجات من wwwroot
        foreach (var product in provider.Products)
        {
            foreach (var image in product.ProductImages)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.ProductImages.RemoveRange(product.ProductImages);
        }

        // حذف جميع المنتجات التابعة للمزود
        _context.Products.RemoveRange(provider.Products);

        // حذف المزود نفسه
        _context.AppUsers.Remove(provider);

        await _context.SaveChangesAsync();
        TempData["Success"] = "Provider and all related products have been deleted.";
        return RedirectToAction("Providers");
    }

    [HttpGet]
    public IActionResult AddSiteProduct()
    {
        return View(); // يعرض نموذج الإدخال
    }


    [HttpPost]
    public async Task<IActionResult> AddSiteProduct(Product product, List<IFormFile> Images)
    {
        product.CreatedAt = DateTime.Now;
        product.IsAvailable = true;
        product.ProviderId = null; // ⬅️ المنتج تابع للموقع نفسه

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
        Directory.CreateDirectory(uploadPath);

        foreach (var file in Images)
        {
            if (file.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var image = new ProductImage
                {
                    ProductId = product.ProductId,
                    ImageUrl = "/images/products/" + fileName,
                    UploadedAt = DateTime.Now
                };

                _context.ProductImages.Add(image);
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Site product added successfully.";
        return RedirectToAction("AllProducts");
    }

    // ✅ عرض منتجات الموقع فقط (التي لا تملك ProviderId)
    public async Task<IActionResult> SiteProducts()
    {
        var products = await _context.Products
            .Where(p => p.ProviderId == null)
            .Include(p => p.ProductImages)
            .ToListAsync();

        return View(products);
    }

    // ✅ تعديل منتج خاص بالموقع
    public async Task<IActionResult> EditSiteProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.ProviderId == null);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSiteProduct(int id, Product product, List<IFormFile> NewImages)
    {
        if (id != product.ProductId || product.ProviderId != null)
            return NotFound();

        _context.Entry(product).State = EntityState.Modified;

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
        Directory.CreateDirectory(uploadPath);

        foreach (var file in NewImages)
        {
            if (file.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var image = new ProductImage
                {
                    ProductId = product.ProductId,
                    ImageUrl = "/images/products/" + fileName,
                    UploadedAt = DateTime.Now
                };

                _context.ProductImages.Add(image);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Site product updated successfully.";
        return RedirectToAction("SiteProducts");
    }

    // ✅ حذف منتج خاص بالموقع
    [HttpPost]
    public async Task<IActionResult> DeleteSiteProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.ProviderId == null);

        if (product == null)
        {
            TempData["Error"] = "Product not found or not a site product.";
            return RedirectToAction("SiteProducts");
        }

        foreach (var image in product.ProductImages)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        _context.ProductImages.RemoveRange(product.ProductImages);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Site product deleted successfully.";
        return RedirectToAction("SiteProducts");
    }

}
