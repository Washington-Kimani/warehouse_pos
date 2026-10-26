using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WarehouseProcurement.Data;
using WarehouseProcurement.Models;

namespace WarehouseProcurement.Controllers;

public sealed class HomeController(ProcurementDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var movements = await db.StockMovements.AsNoTracking().ToListAsync();
        var stockByProduct = movements
            .GroupBy(movement => movement.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(movement => movement.Quantity));
        var products = await db.Products.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToListAsync();
        var stockRows = products
            .Select(product => new StockBalanceViewModel(
                product,
                stockByProduct.GetValueOrDefault(product.Id)))
            .ToList();
        var lowStock = stockRows
            .Where(row => row.Product.ReorderLevel > 0 && row.OnHand <= row.Product.ReorderLevel)
            .OrderBy(row => row.OnHand)
            .Take(5)
            .ToList();
        var model = new DashboardViewModel(
            await db.Products.CountAsync(product => product.IsActive),
            await db.Suppliers.CountAsync(supplier => supplier.IsActive),
            await db.PurchaseOrders.CountAsync(order =>
                order.Status == PurchaseOrderStatuses.Open ||
                order.Status == PurchaseOrderStatuses.PartiallyReceived),
            stockRows.Count(row => row.OnHand > 0),
            lowStock,
            await db.PurchaseOrders.AsNoTracking()
                .Include(order => order.Supplier)
                .OrderByDescending(order => order.CreatedAtUtc)
                .Take(6)
                .ToListAsync());

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
