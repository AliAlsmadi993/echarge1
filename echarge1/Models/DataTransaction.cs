using System;
using System.Collections.Generic;

namespace echarge1.Models;

public partial class DataTransaction
{
    public int TransactionId { get; set; }

    public int? BuyerId { get; set; }

    public int? SellerId { get; set; }

    public int? DataId { get; set; }

    public DateTime? DateOfTransaction { get; set; }

    public decimal? Amount { get; set; }
}
