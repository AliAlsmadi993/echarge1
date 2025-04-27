using echarge1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace echarge1.Controllers
{
    public class EmergencyController : Controller
    {
        private readonly MyDbContext _context;

        public EmergencyController(MyDbContext context)
        {
            _context = context;
        }

        // ✅ تنفيذ طلب الطوارئ من المستخدم
        [HttpGet]
        public async Task<IActionResult> Request(string type, string location)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Json(new { success = false, message = "Login required." });

            var request = new EmergencyRequest
            {
                UserId = userId.Value,
                Location = location,
                RequestType = type,
                Status = "Pending",
                RequestTime = DateTime.Now
            };

            _context.EmergencyRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Chat", new { requestId = request.EmergencyRequestId });
        }

        // ✅ عرض جميع الطلبات للأدمن
        public async Task<IActionResult> AdminPanel()
        {
            var requests = await _context.EmergencyRequests
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();

            return View(requests);
        }

        // ✅ عرض محادثة الطوارئ الخاصة بطلب معيّن
        public async Task<IActionResult> Chat(int requestId)
        {
            var messages = await _context.EmergencyMessages
                .Where(m => m.EmergencyRequestId == requestId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var request = await _context.EmergencyRequests.FindAsync(requestId); // ⬅️ جلب الطلب

            ViewBag.RequestId = requestId;
            ViewBag.CurrentUserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.RequestLocation = request?.Location; // ⬅️ تمرير الموقع للخريطة

            return View(messages);
        }


        // ✅ إرسال رسالة جديدة
        [HttpPost]
        public async Task<IActionResult> SendMessage(int requestId, string message)
        {
            int? senderId = HttpContext.Session.GetInt32("UserId");

            if (senderId == null || string.IsNullOrWhiteSpace(message))
                return Json(new { success = false });

            var msg = new EmergencyMessage
            {
                EmergencyRequestId = requestId,
                SenderId = senderId.Value,
                MessageText = message,
                SentAt = DateTime.Now
            };

            _context.EmergencyMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ✅ جلب الرسائل (لتحديث تلقائي)
        public async Task<IActionResult> GetMessages(int requestId)
        {
            var messages = await _context.EmergencyMessages
    .Where(m => m.EmergencyRequestId == requestId)
    .Include(m => m.Sender)
    .OrderBy(m => m.SentAt)
    .ToListAsync();

            var result = messages.Select(m => new
            {
                m.SenderId,
                m.MessageText,
                SenderName = m.Sender.FullName,
                Time = m.SentAt?.ToString("HH:mm")
            });

            return Json(result);

        }
        public async Task<IActionResult> MyRequests()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var requests = await _context.EmergencyRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();

            return View(requests);
        }
        [HttpGet]
        public IActionResult RequestOptions(string type)
        {
            ViewBag.Type = type;
            return View();
        }

    }
}
