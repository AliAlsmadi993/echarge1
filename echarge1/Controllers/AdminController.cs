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
    public async Task<IActionResult> CourseApplications()
    {
        var enrollments = await _context.CourseEnrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();

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


}
