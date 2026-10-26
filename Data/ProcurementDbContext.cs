using Microsoft.EntityFrameworkCore;
using WarehouseProcurement.Models;

namespace WarehouseProcurement.Data;

public sealed class ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>().HasIndex(s => s.Name);
        modelBuilder.Entity<Product>().HasIndex(p => p.SKU).IsUnique();
        modelBuilder.Entity<Warehouse>().HasIndex(w => w.Code).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasIndex(p => p.Number).IsUnique();
        modelBuilder.Entity<GoodsReceipt>().HasIndex(r => r.Number).IsUnique();

        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(line => line.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<PurchaseOrder>()
            .Property(order => order.ExpectedAt)
            .HasColumnType("timestamp without time zone");

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(order => order.Lines)
            .WithOne(line => line.PurchaseOrder)
            .HasForeignKey(line => line.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasMany(receipt => receipt.Lines)
            .WithOne(line => line.GoodsReceipt)
            .HasForeignKey(line => line.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
