using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class UserDatum
{
    public int UserDataId { get; set; }

    public int? UserId { get; set; }

    public string? DataType { get; set; }

    public string? DataValue { get; set; }

    public DateTime? DateCollected { get; set; }
}
