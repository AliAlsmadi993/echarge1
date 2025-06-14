using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using echarge1.Models;
using Microsoft.AspNetCore.Hosting;

namespace echarge1.Controllers
{
    public class CourseController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CourseController(MyDbContext context, IWebHostEnvironment env) // ✅ مرره هنا
        {
            _context = context;
            _env = env;
        }

        // عرض كل الكورسات
        public async Task<IActionResult> AllCourses()
        {
            var courses = await _context.Courses.ToListAsync();
            return View("AllCourses", courses); // نحدد اسم الـ View
        }

        [HttpGet]
        public IActionResult Apply(int id)
        {
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == id);
            if (course == null) return NotFound();

            ViewBag.SelectedCourse = course;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int CourseId, IFormFile CvUpload, IFormFile IdUpload,
    string FullName, string Email, string Phone, string AdditionalInfo)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "You must be logged in to apply.";
                return RedirectToAction("Login", "Account");
            }

            if (CvUpload == null || IdUpload == null)
            {
                TempData["Error"] = "CV and ID are required.";
                return RedirectToAction("Apply", new { id = CourseId });
            }

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadFolder);

            string cvFileName = Guid.NewGuid() + Path.GetExtension(CvUpload.FileName);
            string idFileName = Guid.NewGuid() + Path.GetExtension(IdUpload.FileName);

            string cvFullPath = Path.Combine(uploadFolder, cvFileName);
            string idFullPath = Path.Combine(uploadFolder, idFileName);

            using (var stream = new FileStream(cvFullPath, FileMode.Create))
            {
                await CvUpload.CopyToAsync(stream);
            }
            using (var stream = new FileStream(idFullPath, FileMode.Create))
            {
                await IdUpload.CopyToAsync(stream);
            }

            var enrollment = new CourseEnrollment
            {
                CourseId = CourseId,
                UserId = userId.Value, // ✅ هون التعديل
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                AdditionalInfo = AdditionalInfo,
                CvPath = "/uploads/" + cvFileName,
                IdPath = "/uploads/" + idFileName,
                EnrollmentDate = DateTime.Now,
                Status = "Pending"
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your application has been submitted successfully!";
            return RedirectToAction("Apply", new { id = CourseId });
        }

        public async Task<IActionResult> MyApplications()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Please log in to view your applications.";
                return RedirectToAction("Login", "Account");
            }

            var applications = await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e => e.UserId == userId.Value)
                .ToListAsync();

            return View(applications);
        }
        public async Task<IActionResult> CancelApplication(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "You must be logged in to cancel an application.";
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.EnrollmentId == id && e.UserId == userId.Value);

            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your application has been successfully cancelled.";
            return RedirectToAction("MyApplications");
        }

    }
}
