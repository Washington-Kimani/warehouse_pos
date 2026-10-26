using System.ComponentModel.DataAnnotations;

namespace WarehouseProcurement.Models;

public sealed class PurchaseOrderCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a supplier.")]
    public int SupplierId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a warehouse.")]
    public int WarehouseId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ExpectedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<PurchaseOrderLineInput> Lines { get; set; } = [];
}

public sealed class PurchaseOrderLineInput
{
    public int ProductId { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal UnitCost { get; set; }
}

public sealed class ReceivePurchaseOrderViewModel
{
    [StringLength(100)]
    public string? SupplierReference { get; set; }

    public Dictionary<int, decimal> Quantities { get; set; } = [];
}

public sealed record DashboardViewModel(
    int ProductCount,
    int SupplierCount,
    int OpenOrderCount,
    int StockedProductCount,
    IReadOnlyList<StockBalanceViewModel> LowStock,
    IReadOnlyList<PurchaseOrder> RecentOrders);

public sealed record StockBalanceViewModel(Product Product, decimal OnHand);
