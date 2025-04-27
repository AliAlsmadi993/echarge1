using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using echarge1.Models;

namespace echarge1.Controllers
{
    public class ShopController : Controller
    {
        private readonly MyDbContext _context;

        public ShopController(MyDbContext context)
        {
            _context = context;
        }

        // ✅ عرض المتجر مع الفلاتر والبحث
        public async Task<IActionResult> Store(string? search, string? category, bool? isFeatured)
        {
            var baseQuery = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews)
                .Include(p => p.Provider)
                .Where(p => p.IsAvailable);

            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                baseQuery = baseQuery.Where(p => p.Category == category);
            }

            if (isFeatured == true)
            {
                baseQuery = baseQuery.Where(p => p.IsFeatured == true);
            }

            // 🧩 قسم المنتجات حسب المصدر
            var providerProducts = await baseQuery
                .Where(p => p.ProviderId != null)
                .ToListAsync();

            var siteProducts = await baseQuery
                .Where(p => p.ProviderId == null)
                .ToListAsync();

            // التصنيفات
            ViewBag.Categories = await _context.Products
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.SiteProducts = siteProducts;

            return View(providerProducts);
        }

        // ✅ عرض تفاصيل منتج معين
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // احضر المنتجات المشابهة لنفس الكاتيجوري
            var relatedProducts = await _context.Products
                .Where(p => p.Category == product.Category && p.ProductId != product.ProductId && p.IsAvailable)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // ✅ إضافة منتج إلى السلة مع التحقق من تسجيل الدخول
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // ❗ إذا ما في يوزر في السيشن، خزنه مؤقتًا في الكوكيز
                var cartCookie = Request.Cookies["Cart_Pending_Product"];
                var productIds = cartCookie != null ? cartCookie.Split(',').ToList() : new List<string>();

                // إضافة المنتج الجديد إلى الكوكيز
                productIds.Add(productId.ToString());

                // تخزين الكوكيز
                Response.Cookies.Append("Cart_Pending_Product", string.Join(",", productIds), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddMinutes(30)
                });

                TempData["Success"] = "Product added to your cart successfully.";
                return RedirectToAction("Store");
            }

            // ✅ التحقق إذا المنتج موجود مسبقًا في الكارت (إذا كان المستخدم مسجل دخول)
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId && !c.IsCheckedOut);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductId = productId,
                    UserId = userId.Value,
                    Quantity = quantity,
                    DateAdded = DateTime.Now,
                    IsCheckedOut = false
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product added to your cart successfully.";
            return RedirectToAction("Store");
        }

        // ✅ عرض المنتجات المميزة فقط
        public async Task<IActionResult> Featured()
        {
            await RemoveExpiredFeaturedProducts();
            var featuredProducts = await _context.Products
                .Where(p => p.IsFeatured == true && p.IsAvailable)
                .Include(p => p.ProductImages)
                .ToListAsync();

            return View("Store", featuredProducts);
        }
 public async Task<IActionResult> Cart()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            List<CartItem> cartItems;

            if (userId != null)
            {
                // إذا كان المستخدم مسجل دخول
                cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId && !c.IsCheckedOut)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.ProductImages) // ✅ جلب صور المنتج
                    .ToListAsync();
            }
            else
            {
                // إذا كان الزائر مش مسجل دخول
                var cartCookie = Request.Cookies["Cart_Pending_Product"];
                var productIds = cartCookie != null ? cartCookie.Split(',').Select(int.Parse).ToList() : new List<int>();

                cartItems = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .Include(p => p.ProductImages) // ✅ جلب صور المنتج للزائر
                    .Select(p => new CartItem
                    {
                        ProductId = p.ProductId,
                        Product = p,
                        Quantity = 1, // الزائر افتراضي الكمية 1
                    })
                    .ToListAsync();
            }

            return View(cartItems);
        }

        // حذف منتج من السلة
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId && !c.IsCheckedOut);

                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cartCookie = Request.Cookies["Cart_Pending_Product"];
                var productIds = cartCookie != null ? cartCookie.Split(',').ToList() : new List<string>();

                if (productIds.Contains(productId.ToString()))
                {
                    productIds.Remove(productId.ToString());
                    Response.Cookies.Append("Cart_Pending_Product", string.Join(",", productIds), new CookieOptions
                    {
                        Expires = DateTimeOffset.Now.AddMinutes(30)
                    });
                }
            }

            TempData["Success"] = "Product removed from your cart.";
            return RedirectToAction("Cart");
        }

        // التحقق من عملية الدفع والتحويل لصفحة الدفع


        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && !c.IsCheckedOut)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Store");
            }

            var totalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price);
            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = totalAmount;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckout(IFormFile ReceiptImage, string shippingAddress)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null || userId == 0)
            {
                TempData["Error"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && !c.IsCheckedOut)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Store");
            }

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price),
                ShippingAddress = shippingAddress, // تخزين العنوان
            };

            // التعامل مع رفع صورة الوصل
            if (ReceiptImage != null && ReceiptImage.Length > 0)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ReceiptImage.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ReceiptImage.CopyToAsync(stream);
                }

                // حفظ مسار الصورة في الـ Order
                order.ReceiptImageUrl = "/uploads/" + fileName;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    SubTotal = item.Quantity * item.Product.Price
                });
            }

            _context.Payments.Add(new Payment
            {
                UserId = userId.Value,
                OrderId = order.OrderId,
                PaymentDate = DateTime.Now,
                AmountPaid = order.TotalAmount,
                PaymentMethod = "Cash"
            });

            foreach (var item in cartItems)
            {
                item.IsCheckedOut = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your order has been placed successfully!";
            return RedirectToAction("OrderDetails", new { id = order.OrderId });
        }

        // عرض تفاصيل الطلب
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User) // ⬅️ ضروري لجلب بيانات المستخدم
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        public async Task<IActionResult> History()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Please log in to view your order history.";
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
        public async Task<IActionResult> RateProduct(int orderId, int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductReviews)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return NotFound();
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = HttpContext.Session.GetInt32("UserId").Value
            };

            return View(review);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(ProductReview review)
        {
            if (ModelState.IsValid)
            {
                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your review has been submitted.";
                return RedirectToAction("History");
            }

            return View(review);
        }
        private async Task RemoveExpiredFeaturedProducts()
        {
            var now = DateTime.Now;

            var expiredPromos = await _context.PromotionRequests
                .Include(p => p.Product)
                .Where(p => p.Status == "Approved" && p.ApprovedUntil < now)
                .ToListAsync();

            foreach (var promo in expiredPromos)
            {
                if (promo.Product != null)
                {
                    promo.Product.IsFeatured = false;
                }

                promo.Status = "Expired"; // خيار إضافي لتحديث حالة الطلب
            }

            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public IActionResult GetCartPreview()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            List<object> previewItems = new();

            if (userId != null)
            {
                var items = _context.CartItems
                    .Where(c => c.UserId == userId && !c.IsCheckedOut)
                    .Include(c => c.Product)
                    .Take(3)
                    .ToList();

                previewItems = items.Select(i => new
                {
                    name = i.Product.Name,
                    imageUrl = i.Product.ImageUrl ?? "/images/default.jpg",
                    quantity = i.Quantity,
                    price = i.Product.Price
                }).ToList<object>();
            }
            else
            {
                // fallback لسلة الزوار إذا كنت تخزنها في الكوكيز أو سيشن مثلاً
                previewItems = new List<object>(); // أو رجع عناصر من الكوكيز إن وجدت
            }

            return Json(previewItems);
        }
        [HttpGet]
        public IActionResult GetCartCount()
        {
            int count = 0;
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                // للمستخدم المسجل
                count = _context.CartItems
                    .Where(c => c.UserId == userId && !c.IsCheckedOut)
                    .Sum(c => c.Quantity);
            }
            else
            {
                // للزائر، إذا كنت مخزن السلة في session كـ string مثلًا: "1,2,2,3"
                var cartString = HttpContext.Session.GetString("cart");
                if (!string.IsNullOrEmpty(cartString))
                {
                    var productIds = cartString.Split(',').Select(int.Parse);
                    count = productIds.Count();
                }
            }

            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, string action)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Cart");

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && !c.IsCheckedOut);

            if (item != null)
            {
                if (action == "increase")
                    item.Quantity++;
                else if (action == "decrease" && item.Quantity > 1)
                    item.Quantity--;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Cart");
        }



    }
}

