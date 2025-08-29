using System;
using System.Collections.Generic;

namespace IPhoneShop.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string CustomerId { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public int Quantity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public string DeliveryAddress { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
