using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using echarge1.Models;

namespace echarge1.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext _context;

        public HomeController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var serviceCards = await _context.ServiceCards.ToListAsync();
            var testimonials = await _context.Testimonials
      .Select(t => new
      {
          t.TestimonialId,
          t.Subject,
          t.Message,
          t.CreatedAt,
          UserName = t.UserId != null
              ? _context.AppUsers.Where(u => u.UserId == t.UserId).Select(u => u.FullName).FirstOrDefault()
              : "",  // لو ما فيه UserId، نحط اسمه "مجهول" أو "Anonymous"
          ProfileImage = t.UserId != null
              ? _context.AppUsers.Where(u => u.UserId == t.UserId).Select(u => u.ProfileImage).FirstOrDefault()
              : "/images/default-user.png"  // صورة افتراضية لو ما فيه صورة
      })
      .ToListAsync();


            var userCount = await _context.AppUsers.CountAsync();
            var stationCount = await _context.ChargingStations.CountAsync();
            var reviewCount = await _context.Reviews.CountAsync();
            var testimonialCount = testimonials.Count;


            ViewBag.ServiceCards = serviceCards;
            ViewBag.Testimonials = testimonials;

            ViewBag.Statistics = new
            {
                UserCount = userCount,
                StationCount = stationCount,
                ReviewCount = reviewCount,
                TestimonialCount = testimonialCount
            };


            return View();
        }

        // Displays the privacy policy page
        public IActionResult About()
        {
            return View();
        }

        // Handles errors and shows error details
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        /////contact
        ///
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string phone, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill all fields.";
                return RedirectToAction("Contact");
            }

            var contactMessage = new ContactMessage
            {
                Subject = $"Contact from {name} ({email} - {phone})",
                Message = message,
                CreatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(contactMessage);
            _context.SaveChanges();

            TempData["Success"] = "Your message has been sent successfully!";
            return RedirectToAction("Contact");
        }


    }
}
