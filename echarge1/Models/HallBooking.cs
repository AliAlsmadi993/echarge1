using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class HallBooking
{
    public int BookingId { get; set; }

    public string? HallType { get; set; }

    public string? RequestingParty { get; set; }

    public DateOnly? EventDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public string? EventType { get; set; }

    public string? EventTitle { get; set; }

    public string? GuestOfHonor { get; set; }

    public string? AccompanyingActivities { get; set; }

    public bool? NeedsScreens { get; set; }

    public bool? NeedsProjector { get; set; }

    public bool? NeedsAudienceInteraction { get; set; }

    public int? GuestCount { get; set; }

    public bool? NeedsFilm { get; set; }

    public bool? NeedsPconStage { get; set; }

    public bool? NeedsDocumentation { get; set; }

    public string? OtherTechnicalNeeds { get; set; }

    public string? CoordinatorName { get; set; }

    public string? ResponsibleName { get; set; }

    public DateOnly? RequestDate { get; set; }

    public string? Status { get; set; }
}
