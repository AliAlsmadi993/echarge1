using echarge1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ChargingStationController : Controller
{
    private readonly MyDbContext _context;

    public ChargingStationController(MyDbContext context)
    {
        _context = context;
    }

    // ✅ API - Return all stations as JSON (for map usage)
    public JsonResult GetAllStationsJson()
    {
        var stations = _context.ChargingStations.Select(s => new
        {
            s.StationId,
            s.Name,
            s.Location,
            s.Capacity,
            s.AvailableSpots,
            s.PricePerCharge
        }).ToList();

        return Json(stations);
    }

    // ✅ GET: Show Add Station Form
    [HttpGet]
    public IActionResult AddStation()
    {
        return View();
    }

    // ✅ POST: Add new station from Google Maps or device location
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStation(ChargingStation station, string? locationUrl, string? deviceLocation, string? chargerTypesJson)
    {
        var providerId = HttpContext.Session.GetInt32("UserId");

        if (providerId == null)
        {
            TempData["Error"] = "Please login first.";
            return RedirectToAction("Login", "Account");
        }

        if (!string.IsNullOrEmpty(locationUrl))
        {
            var location = ExtractCoordinatesFromGoogleMapsUrl(locationUrl);
            if (location == null)
            {
                ModelState.AddModelError("Location", "Invalid Google Maps link.");
                return View(station);
            }
            station.Location = location;
        }
        else if (!string.IsNullOrEmpty(deviceLocation))
        {
            station.Location = deviceLocation;
        }

        // ✅ ربط المحطة بالمزود الحالي
        station.ProviderId = providerId.Value;

        station.CreatedAt = DateTime.Now;
        station.AvailableSpots = station.Capacity;
        station.ChargerTypesJson = chargerTypesJson;

        _context.ChargingStations.Add(station);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Station added successfully.";
        var userType = HttpContext.Session.GetString("UserType");

        if (userType == "Admin")
            return RedirectToAction("ManageStations", "ChargingStation");
        else if (userType == "Provider")
            return RedirectToAction("MyStations", "Map");

        return RedirectToAction("Index", "Home");
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

    // ✅ POST: Update Station Info
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStation(int id, ChargingStation station, string? locationUrl, string? deviceLocation, string? chargerTypesJson)
    {
        if (id != station.StationId)
            return NotFound();

        if (!string.IsNullOrEmpty(locationUrl))
        {
            var location = ExtractCoordinatesFromGoogleMapsUrl(locationUrl);
            if (location == null)
            {
                ModelState.AddModelError("Location", "Invalid Google Maps link.");
                return View(station);
            }
            station.Location = location;
        }
        else if (!string.IsNullOrEmpty(deviceLocation))
        {
            station.Location = deviceLocation;
        }

        station.ChargerTypesJson = chargerTypesJson;

        _context.Entry(station).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Station updated successfully.";
        return RedirectToAction("ManageStations");
    }

    // ✅ POST: Delete Station
    [HttpPost]
    public async Task<IActionResult> DeleteStation(int id)
    {
        var station = await _context.ChargingStations.FindAsync(id);
        if (station == null)
            return NotFound();

        _context.ChargingStations.Remove(station);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Station deleted successfully.";
        return RedirectToAction("ManageStations");
    }

    // ✅ GET: Admin/Provider - Manage All Stations
    public async Task<IActionResult> ManageStations()
    {
        var stations = await _context.ChargingStations
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(stations);
    }

    // ✅ POST: User books a charging slot
    [HttpPost]
    public async Task<IActionResult> BookChargingSlot(int stationId, DateTime bookingDate)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var booking = new ChargingBooking
        {
            StationId = stationId,
            UserId = userId,
            BookingDate = bookingDate,
            Status = "Pending"
        };

        _context.ChargingBookings.Add(booking);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Charging appointment booked.";
        return RedirectToAction("MyChargingBookings");
    }

    // ✅ GET: User View My Bookings
    public async Task<IActionResult> MyChargingBookings()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var bookings = await _context.ChargingBookings
            .Include(b => b.Station)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    // ✅ Extract lat,lng from Google Maps link
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
}