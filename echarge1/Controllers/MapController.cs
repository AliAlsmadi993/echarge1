using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using echarge1.Models;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Text.Json;

namespace echarge1.Controllers
{
    public class MapController : Controller
    {
        private readonly MyDbContext _context;

        public MapController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stations = await _context.ChargingStations.ToListAsync();
            return View(stations);
        }

        [HttpGet]
        public async Task<IActionResult> BookCharging(int stationId)
        {
            var station = await _context.ChargingStations.FindAsync(stationId);
            if (station == null) return NotFound();

            ViewBag.StationId = stationId;
            ViewBag.StationName = station.Name;

            var chargerTypes = JsonSerializer.Deserialize<Dictionary<string, int>>(station.ChargerTypesJson ?? "{}");
            ViewBag.ChargerTypes = chargerTypes?.Keys.ToList() ?? new List<string>();
            ViewBag.ChargerTypesJson = station.ChargerTypesJson;

            ViewBag.ExistingBookings = await _context.ChargingBookings
                .Where(b => b.StationId == stationId && b.BookingDate >= DateTime.Now)
                .ToListAsync();

            return View();
        }




        [HttpPost]
        public async Task<IActionResult> BookCharging(int stationId, DateTime bookingDate, string chargerType)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Please login to book.";
                return RedirectToAction("Index");
            }

            var user = await _context.AppUsers.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // 🔒 Check if user is blacklisted
            if (_context.BlacklistedUsers.Any(b => b.UserId == userId))
            {
                TempData["Error"] = "You are blocked from booking.";
                return RedirectToAction("Index");
            }

            // 🔁 Check if user missed 3 appointments (pending bookings in the past)
            var missedCount = await _context.ChargingBookings
                .CountAsync(b => b.UserId == userId && b.Status == "Pending" && b.BookingDate < DateTime.Now);

            if (missedCount >= 3)
            {
                _context.BlacklistedUsers.Add(new BlacklistedUser
                {
                    UserId = userId.Value,
                    Reason = "Missed 3 charging appointments.",
                    BlockedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Error"] = "You have been blocked due to 3 missed appointments.";
                return RedirectToAction("Index");
            }

            var station = await _context.ChargingStations.FindAsync(stationId);
            if (station == null)
            {
                TempData["Error"] = "Station not found.";
                return RedirectToAction("Index");
            }

            var chargerTypes = JsonSerializer.Deserialize<Dictionary<string, int>>(station.ChargerTypesJson ?? "{}");

            if (!chargerTypes.ContainsKey(chargerType))
            {
                TempData["Error"] = "This station does not support the selected charger type.";
                return RedirectToAction("BookCharging", new { stationId });
            }

            int existingCount = await _context.ChargingBookings
                .CountAsync(b => b.StationId == stationId &&
                                 b.BookingDate == bookingDate &&
                                 b.ChargerType == chargerType &&
                                 b.Status != "Cancelled" &&
                                 b.Status != "Completed");

            if (existingCount >= chargerTypes[chargerType])
            {
                TempData["Error"] = "All chargers of this type are booked at this time.";
                return RedirectToAction("BookCharging", new { stationId });
            }

            var booking = new ChargingBooking
            {
                StationId = stationId,
                UserId = userId.Value,
                BookingDate = bookingDate,
                Status = "Pending",
                ChargerType = chargerType
            };

            _context.ChargingBookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Charging booking created successfully.";
            return RedirectToAction("MyBookings");
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Please login.";
                return RedirectToAction("Index");
            }

            var bookings = await _context.ChargingBookings
                .Include(b => b.Station)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> EditBooking(int id)
        {
            var booking = await _context.ChargingBookings.Include(b => b.Station).FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> EditBooking(ChargingBooking updatedBooking)
        {
            var booking = await _context.ChargingBookings.FindAsync(updatedBooking.BookingId);
            if (booking == null)
                return NotFound();

            booking.BookingDate = updatedBooking.BookingDate;
            booking.Status = updatedBooking.Status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking updated.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.ChargingBookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            _context.ChargingBookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking deleted.";
            return RedirectToAction("MyBookings");
        }
        public async Task<IActionResult> MyStations()
        {
            var providerId = HttpContext.Session.GetInt32("UserId");

            if (providerId == null)
            {
                TempData["Error"] = "Please log in.";
                return RedirectToAction("Login", "Account");
            }

            var stations = await _context.ChargingStations
                .Where(s => s.ProviderId == providerId)
                .ToListAsync();

            return View(stations);
        }

        public async Task<IActionResult> StationBookingsCount()
        {
            var data = await _context.ChargingBookings
                .GroupBy(b => b.StationId)
                .Select(g => new
                {
                    StationId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Json(data);
        }

        public async Task<IActionResult> GetStationsJson()
        {
            var stations = await _context.ChargingStations.ToListAsync();

            var result = stations
                .Select(s =>
                {
                    try
                    {
                        var cleanLocation = s.Location.Split('?')[0];
                        var parts = cleanLocation.Split(',');
                        if (parts.Length >= 2 &&
                            double.TryParse(parts[0], out double lat) &&
                            double.TryParse(parts[1], out double lng))
                        {
                            return new
                            {
                                Id = s.StationId,
                                s.Name,
                                Lat = lat,
                                Lng = lng,
                                Chargers = "Fast",
                                Status = s.AvailableSpots > 0 ? "Available" : "Unavailable",
                                Distance = "Unknown",
                                Time = "Unknown"
                            };
                        }
                    }
                    catch { }

                    return null;
                })
                .Where(s => s != null)
                .ToList();

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> ViewStationDetails(int id)
        {
            var providerId = HttpContext.Session.GetInt32("UserId");

            var station = await _context.ChargingStations
                .FirstOrDefaultAsync(s => s.StationId == id && s.ProviderId == providerId);

            if (station == null)
            {
                TempData["Error"] = "Station not found or unauthorized access.";
                return RedirectToAction("MyStations");
            }

            var bookings = await _context.ChargingBookings
                .Where(b => b.StationId == id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.StationName = station.Name;

            return View(bookings);
        }

    }
}
