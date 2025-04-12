using Microsoft.AspNetCore.Mvc;
using echarge1.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;

public class AccountController : Controller
{
    private readonly MyDbContext _context;

    public AccountController(MyDbContext context)
    {
        _context = context;
    }

    // ✅ GET: Register
    public IActionResult Register()
    {
        return View();
    }

    // ✅ POST: Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AppUser user, string confirmPassword)
    {
        if (user.PasswordHash != confirmPassword)
        {
            ModelState.AddModelError("PasswordHash", "Passwords do not match");
            return View(user);
        }

        if (await _context.AppUsers.AnyAsync(u => u.Email == user.Email))
        {
            ModelState.AddModelError("Email", "Email already in use");
            return View(user);
        }

        // ✅ تشفير كلمة المرور وتخزين البيانات
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.CreatedAt = DateTime.Now;
        user.Status = "Active";
        user.LastLogin = null;

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Registration successful!";
        return RedirectToAction("Login", "Account");
    }

    // ✅ GET: Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // ✅ POST: Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AppUser model)
    {
        if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.PasswordHash))
        {
            TempData["Error"] = "Please enter both email and password.";
            return View(model);
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == model.Email && u.Status == "Active");

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.PasswordHash, user.PasswordHash))
        {
            TempData["Error"] = "Invalid credentials!";
            return View(model);
        }

        // ✅ تخزين معلومات الجلسة
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserType", user.UserType);
        HttpContext.Session.SetString("UserName", user.FullName);

        // تخزين صورة المستخدم في ViewBag أو ViewData
        HttpContext.Session.SetString("UserProfileImage", user.ProfileImage ?? "/images/default-profile.png");

        user.LastLogin = DateTime.Now;
        await _context.SaveChangesAsync();

        // ✅ Logging: التحقق إذا كانت الكوكيز تحتوي على بيانات
        var cartCookie = Request.Cookies["Cart_Pending_Product"];
        if (!string.IsNullOrEmpty(cartCookie))
        {
            var productIds = cartCookie.Split(',').Select(int.Parse).ToList();

            // ✅ إضافة المنتجات إلى السلة الخاصة بالمستخدم
            foreach (var productId in productIds)
            {
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == user.UserId);

                if (existingItem == null)
                {
                    _context.CartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        UserId = user.UserId,
                        Quantity = 1,
                        DateAdded = DateTime.Now,
                        IsCheckedOut = false
                    });
                }
            }

            // ✅ حفظ التغييرات في قاعدة البيانات
            await _context.SaveChangesAsync();

            // ✅ حذف الكوكيز بعد إضافة المنتجات إلى السلة
            Response.Cookies.Delete("Cart_Pending_Product");
        }

        // ✅ توجيه المستخدم حسب نوعه
        return user.UserType switch
        {
            "Admin" => RedirectToAction("AdminDashboard", "Admin"),
            "Provider" => RedirectToAction("MyProducts", "Provider"),
            _ => RedirectToAction("Index", "Home"),
        };
    }

    public async Task<IActionResult> Profile()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["Error"] = "Please log in to view your profile.";
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        return View(user);  // تمرير البيانات للمودل في الـ View
    }

    // ✅ عرض صفحة تعديل البروفايل
    public async Task<IActionResult> Edit()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["Error"] = "Please log in to edit your profile.";
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        return View(user);  // تمرير بيانات المستخدم إلى View لتعديلها
    }

    // ✅ حفظ التعديلات على البروفايل
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AppUser model, IFormFile profileImage)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["Error"] = "Please log in to update your profile.";
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        // تحديث بيانات المستخدم
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;

        // التعامل مع صورة البروفايل
        if (profileImage != null && profileImage.Length > 0)
        {
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(stream);
            }

            user.ProfileImage = "/uploads/" + fileName; // حفظ مسار الصورة
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Your profile has been updated successfully!";
        return RedirectToAction("Profile");
    }

    // ✅ تسجيل الخروج
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // مسح جميع البيانات من الجلسة
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult RegisterAdmin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAdmin(AppUser user, string Password, string ConfirmPassword)
    {
        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError("PasswordHash", "Passwords do not match.");
            return View(user);
        }

        if (await _context.AppUsers.AnyAsync(u => u.Email == user.Email))
        {
            ModelState.AddModelError("Email", "Email already in use.");
            return View(user);
        }

        // إعداد بيانات الأدمن
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
        user.UserType = "Admin";
        user.CreatedAt = DateTime.Now;
        user.Status = "Active";
        user.LastLogin = null;

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        // إضافة إلى جدول Admin
        var admin = new Admin
        {
            UserId = user.UserId,
            Role = "SuperAdmin" // أو Admin حسب الحاجة
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Admin registered successfully.";
        return RedirectToAction("Login", "Account");
    }

}
