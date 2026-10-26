using System.ComponentModel.DataAnnotations;

namespace WarehouseProcurement.Models;

public sealed class Supplier
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    [StringLength(120)]
    public string? ContactName { get; set; }

    [StringLength(40)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class Product
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string SKU { get; set; } = "";

    [Required, StringLength(160)]
    public string Name { get; set; } = "";

    [StringLength(40)]
    public string StockUnit { get; set; } = "each";

    [Range(typeof(decimal), "0", "999999999")]
    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class Warehouse
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Code { get; set; } = "";

    [Required, StringLength(120)]
    public string Name { get; set; } = "";
}

public sealed class PurchaseOrder
{
    public int Id { get; set; }

    [Required, StringLength(32)]
    public string Number { get; set; } = "";

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpectedAt { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = PurchaseOrderStatuses.Open;

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = [];
}

public sealed class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Range(typeof(decimal), "0.0001", "999999999")]
    public decimal OrderedQuantity { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal UnitCost { get; set; }

    public decimal ReceivedQuantity { get; set; }
    public int Version { get; set; }
}

public sealed class GoodsReceipt
{
    public int Id { get; set; }

    [Required, StringLength(32)]
    public string Number { get; set; } = "";

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public DateTime ReceivedAtUtc { get; set; }

    [StringLength(100)]
    public string? SupplierReference { get; set; }

    public List<GoodsReceiptLine> Lines { get; set; } = [];
}

public sealed class GoodsReceiptLine
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public int PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public sealed class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public decimal Quantity { get; set; }

    [Required, StringLength(24)]
    public string Type { get; set; } = "Receipt";

    [Required, StringLength(32)]
    public string Reference { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}

public static class PurchaseOrderStatuses
{
    public const string Open = "Open";
    public const string PartiallyReceived = "Partially received";
    public const string Received = "Received";
}
