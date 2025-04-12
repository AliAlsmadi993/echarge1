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
        HttpContext.Session.SetString("UserProfileImage", user.ProfileImage ?? "/images/default-profile.png");

        user.LastLogin = DateTime.Now;
        await _context.SaveChangesAsync();

        // ✅ دمج السلة من الكوكيز إلى قاعدة البيانات
        var cartCookie = Request.Cookies["Cart_Pending_Product"];
        if (!string.IsNullOrEmpty(cartCookie))
        {
            var productIds = cartCookie.Split(',').Select(int.Parse).ToList();
            var grouped = productIds.GroupBy(id => id)
                                     .Select(g => new { ProductId = g.Key, Count = g.Count() })
                                     .ToList();

            foreach (var item in grouped)
            {
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.ProductId == item.ProductId && c.UserId == user.UserId && !c.IsCheckedOut);

                if (existingItem != null)
                {
                    existingItem.Quantity += item.Count;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        UserId = user.UserId,
                        Quantity = item.Count,
                        DateAdded = DateTime.Now,
                        IsCheckedOut = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            Response.Cookies.Delete("Cart_Pending_Product");
        }

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

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

    public async Task<IActionResult> Edit()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["Error"] = "Please log in to edit your profile.";
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

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

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;

        if (profileImage != null && profileImage.Length > 0)
        {
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await profileImage.CopyToAsync(stream);

            user.ProfileImage = "/uploads/" + fileName;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Your profile has been updated successfully!";
        return RedirectToAction("Profile");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
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

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
        user.UserType = "Admin";
        user.CreatedAt = DateTime.Now;
        user.Status = "Active";
        user.LastLogin = null;

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        var admin = new Admin
        {
            UserId = user.UserId,
            Role = "SuperAdmin"
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Admin registered successfully.";
        return RedirectToAction("Login", "Account");
    }
}
