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
            var productsQuery = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews)
                .Include(p => p.Provider)
                .Where(p => p.IsAvailable);

            if (!string.IsNullOrWhiteSpace(search))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            if (isFeatured == true)
            {
                productsQuery = productsQuery.Where(p => p.IsFeatured == true);
            }

            var products = await productsQuery.ToListAsync();
            ViewBag.Categories = await _context.Products
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;

            return View(products);
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
                Response.Cookies.Append("Cart_Pending_Product", productId.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddMinutes(30)
                });

                TempData["Error"] = "Please log in to add items to your cart.";
                return RedirectToAction("Login", "Account");
            }

            // ✅ التحقق إذا المنتج موجود مسبقًا في الكارت
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
                // إذا كان المستخدم مسجل دخول، استعرض السلة من قاعدة البيانات
                cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId && !c.IsCheckedOut)
                    .Include(c => c.Product)
                    .ToListAsync();
            }
            else
            {
                // إذا لم يكن مسجل دخول، استعرض السلة من الكوكيز
                var cartCookie = Request.Cookies["Cart_Pending_Product"];
                var productIds = cartCookie != null ? cartCookie.Split(',').Select(int.Parse).ToList() : new List<int>();

                cartItems = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .Select(p => new CartItem
                    {
                        ProductId = p.ProductId,
                        Product = p,
                        Quantity = 1, // القيم الافتراضية لأن البيانات غير موجودة في الكوكيز
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

            // حساب المجموع
            var totalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price);

            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = totalAmount;

            return View();
        }

        // تنفيذ عملية الدفع
        [HttpPost]
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Checkout(int userId)
        {
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && !c.IsCheckedOut)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Store");
            }

            // إنشاء طلب جديد
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price)
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // إضافة عناصر الطلب
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    SubTotal = item.Quantity * item.Product.Price
                };

                _context.OrderItems.Add(orderItem);
            }

            // إضافة الدفع الوهمي (يمكنك استبداله بطريقة دفع حقيقية هنا)
            var payment = new Payment
            {
                UserId = userId,
                OrderId = order.OrderId,
                PaymentDate = DateTime.Now,
                AmountPaid = order.TotalAmount,
                PaymentMethod = "Fake Payment"  // استبدلها بطريقة الدفع الحقيقية
            };

            _context.Payments.Add(payment);

            // تحديث السلة (تحديد العناصر التي تم دفعها)
            foreach (var item in cartItems)
            {
                item.IsCheckedOut = true; // تحديث حالة السلة إلى "تم الدفع"
            }

            await _context.SaveChangesAsync();

            // الانتقال إلى صفحة تفاصيل الطلب بعد الدفع
            TempData["Success"] = "Your order has been placed successfully!";
            return RedirectToAction("OrderDetails", "Shop", new { id = order.OrderId });
        }

        // عرض تفاصيل الطلب
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
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

    }
}

