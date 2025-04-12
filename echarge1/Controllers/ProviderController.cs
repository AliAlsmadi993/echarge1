using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using echarge1.Models;

namespace echarge1.Controllers
{
    public class ProviderController : Controller
    {
        private readonly MyDbContext _context;

        public ProviderController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Profile()
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.AppUsers.FindAsync(providerId);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(AppUser updatedUser)
        {
            var user = await _context.AppUsers.FindAsync(updatedUser.UserId);
            if (user == null)
                return NotFound();

            user.FullName = updatedUser.FullName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Email = updatedUser.Email;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProductImage(int id, int productId)
        {
            var image = await _context.ProductImages.FirstOrDefaultAsync(i => i.ImageId == id);
            if (image != null)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("EditProduct", new { id = productId });
        }

        public async Task<IActionResult> MyProducts()
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.ProviderId == providerId)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> MyOrders()
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User) // Include User to show name and address
                .Where(o => o.OrderItems.Any(oi => oi.Product.ProviderId == providerId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User) // Get User details for order
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            // هنا نعرض الـ View بدلاً من PartialView
            return View("GetOrderDetails", order);
        }


       [HttpPost]
public async Task<IActionResult> UpdateOrderStatus(int OrderId, string Status)
{
    var order = await _context.Orders.FindAsync(OrderId);

    if (order == null)
    {
        return NotFound();
    }

    // تحديث حالة الطلب بناءً على الزر الذي ضغط عليه الأدمن
    order.Status = Status;

    // حفظ التغييرات في قاعدة البيانات
    await _context.SaveChangesAsync();

    TempData["Success"] = "Order status updated successfully.";

    return RedirectToAction("MyOrders");
}


        [HttpPost]
        public async Task<IActionResult> RequestPromotion(int productId)
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            var existing = await _context.PromotionRequests
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.Status == "Pending");

            if (existing != null)
            {
                TempData["Error"] = "You already have a pending promotion request for this product.";
                return RedirectToAction("MyProducts");
            }

            var request = new PromotionRequest
            {
                ProductId = productId,
                ProviderId = providerId.Value,
                RequestDate = DateTime.Now,
                Status = "Pending",
                AmountPaid = 10.00m
            };

            _context.PromotionRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Promotion request sent successfully.";
            return RedirectToAction("MyProducts");
        }

        public async Task<IActionResult> PromotionRequests()
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            var requests = await _context.PromotionRequests
                .Include(r => r.Product)
                .Where(r => r.ProviderId == providerId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, List<IFormFile> Images)
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            product.ProviderId = providerId.Value;
            product.CreatedAt = DateTime.Now;
            product.IsAvailable = true;

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

            TempData["Success"] = "Product added successfully.";
            return RedirectToAction("MyProducts");
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product, List<IFormFile> Images)
        {
            var existing = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

            if (existing == null)
                return NotFound();

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
            existing.Category = product.Category;

            if (Images != null && Images.Any())
            {
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
                            ProductId = existing.ProductId,
                            ImageUrl = "/images/products/" + fileName,
                            UploadedAt = DateTime.Now
                        };

                        _context.ProductImages.Add(image);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction("MyProducts");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.PromotionRequests)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            _context.PromotionRequests.RemoveRange(product.PromotionRequests);

            foreach (var img in product.ProductImages)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            _context.ProductImages.RemoveRange(product.ProductImages);

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction("MyProducts");
        }

        public async Task<IActionResult> Dashboard()
        {
            int? providerId = HttpContext.Session.GetInt32("UserId");
            if (providerId == null)
                return RedirectToAction("Login", "Account");

            var totalProducts = await _context.Products.CountAsync(p => p.ProviderId == providerId);
            var totalOrders = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.ProviderId == providerId))
                .CountAsync();

            var totalRevenue = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.ProviderId == providerId))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalStations = await _context.ChargingStations
                .Where(s => s.Location.Contains($"pid={providerId}"))
                .CountAsync();

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalStations = totalStations;

            return View();
        }

    }
}
