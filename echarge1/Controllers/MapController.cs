﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using echarge1.Models;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Text.Json;
using System.Globalization;

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

        // ✅ GET: Edit Station Page
        [HttpGet]
        public async Task<IActionResult> EditStation(int id)
        {
            var station = await _context.ChargingStations.FindAsync(id);
            if (station == null)
                return NotFound();

            return View(station);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStation(int id, ChargingStation updated, string? locationUrl, string? deviceLocation, string? chargerTypesJson)
        {
            var existing = await _context.ChargingStations.FirstOrDefaultAsync(s => s.StationId == id);
            if (existing == null)
                return NotFound();

            // تحديث الحقول الأساسية فقط
            existing.Name = updated.Name;
            existing.Capacity = updated.Capacity;
            existing.AvailableSpots = updated.AvailableSpots;
            existing.PricePerCharge = updated.PricePerCharge;

            if (!string.IsNullOrEmpty(locationUrl))
            {
                var location = ExtractCoordinatesFromGoogleMapsUrl(locationUrl);
                if (location == null)
                {
                    ModelState.AddModelError("Location", "Invalid Google Maps link.");
                    return View(existing);
                }
                existing.Location = location;
            }
            else if (!string.IsNullOrEmpty(deviceLocation))
            {
                existing.Location = deviceLocation;
            }

            if (!string.IsNullOrEmpty(chargerTypesJson))
            {
                existing.ChargerTypesJson = chargerTypesJson;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Station updated successfully.";
            return RedirectToAction("MyStations");
        }

        private string? ExtractCoordinatesFromGoogleMapsUrl(string url)
        {
            try
            {
                int atIndex = url.IndexOf("@");
                if (atIndex != -1)
                {
                    string coordsPart = url.Substring(atIndex + 1);
                    string[] parts = coordsPart.Split(',');

                    if (parts.Length >= 2)
                    {
                        double lat = double.Parse(parts[0], CultureInfo.InvariantCulture);
                        double lng = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        return $"{lat},{lng}";
                    }
                }
            }
            catch
            {
                // ignore error
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> NavigateToEmergency(int requestId)
        {
            var request = await _context.EmergencyRequests.FindAsync(requestId);
            if (request == null || string.IsNullOrWhiteSpace(request.Location))
            {
                TempData["Error"] = "Location not found.";
                return RedirectToAction("AdminPanel", "Emergency");
            }

            return RedirectToAction("Index", new { latlng = request.Location });
        }


        [HttpGet]
        public async Task<IActionResult> GetEvSupportUnits()
        {
            var units = await _context.EvsupportUnits.ToListAsync();

            var result = units.Select(u =>
            {
                try
                {
                    var parts = u.Location?.Split(',');
                    if (parts?.Length >= 2 &&
                        double.TryParse(parts[0], out double lat) &&
                        double.TryParse(parts[1], out double lng))
                    {
                        return new
                        {
                            id = u.Id,
                            name = u.Name,
                            type = u.Type,
                            status = u.Status,
                            vehicleType = u.VehicleType,
                            lat,
                            lng
                        };
                    }
                }
                catch { }

                return null;
            }).Where(x => x != null).ToList();

            return Json(result);
        }
        [HttpGet]
        public IActionResult AddSupportUnit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupportUnit(EvsupportUnit unit, string? locationUrl, string? deviceLocation)
        {
            if (!string.IsNullOrEmpty(locationUrl))
            {
                var location = ExtractCoordinatesFromGoogleMapsUrl(locationUrl);
                if (location == null)
                {
                    ModelState.AddModelError("Location", "Invalid Google Maps link.");
                    return View(unit);
                }
                unit.Location = location;
            }
            else if (!string.IsNullOrEmpty(deviceLocation))
            {
                unit.Location = deviceLocation;
            }

            unit.CreatedAt = DateTime.Now;
            unit.Status = "Available";

            _context.EvsupportUnits.Add(unit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Support Unit added successfully.";
            return RedirectToAction("Index");
        }


    }
}

