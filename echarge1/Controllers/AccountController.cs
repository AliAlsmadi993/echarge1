using Microsoft.AspNetCore.Mvc;
using echarge1.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly MyDbContext _context;

    public AccountController(MyDbContext context)
    {
        _context = context;
    }

    // GET: Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: Register
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

        // التحقق من صحة النوع المدخل
        if (user.UserType != "User" && user.UserType != "Provider")
        {
            ModelState.AddModelError("UserType", "Invalid user type.");
            return View(user);
        }

        // حفظ البيانات
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.CreatedAt = DateTime.Now;
        user.Status = "Active";
        user.LastLogin = null;

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Registration successful!";
        return RedirectToAction("Login", "Account");
    }
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AppUser model)
    {
        if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.PasswordHash))
        {
            TempData["Error"] = "Please enter both email and password.";
            return View(model);
        }

        // ابحث عن المستخدم بالإيميل
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == model.Email && u.Status == "Active");

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.PasswordHash, user.PasswordHash))
        {
            TempData["Error"] = "Invalid credentials!";
            return View(model);
        }

        // تخزين البيانات في السيشن
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserType", user.UserType);
        HttpContext.Session.SetString("UserName", user.FullName);

        // توجيه المستخدم حسب النوع
        if (user.UserType == "Admin")
            return RedirectToAction("Dashboard", "Admin");
        else if (user.UserType == "Provider")
            return RedirectToAction("Dashboard", "Provider");
        else
            return RedirectToAction("Index", "Home");
    }

}
