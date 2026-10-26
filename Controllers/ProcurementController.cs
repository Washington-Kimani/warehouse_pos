using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseProcurement.Data;
using WarehouseProcurement.Models;

namespace WarehouseProcurement.Controllers;

public sealed class ProcurementController(ProcurementDbContext db, ILogger<ProcurementController> logger) : Controller
{
    public async Task<IActionResult> Suppliers()
    {
        return View(await db.Suppliers.AsNoTracking().OrderBy(supplier => supplier.Name).ToListAsync());
    }

    public IActionResult NewSupplier() => View(new Supplier());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewSupplier(Supplier supplier)
    {
        if (!ModelState.IsValid)
        {
            return View(supplier);
        }

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Supplier {supplier.Name} was added.";
        return RedirectToAction(nameof(Suppliers));
    }

    public async Task<IActionResult> Products()
    {
        return View(await db.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync());
    }

    public IActionResult NewProduct() => View(new Product());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProduct(Product product)
    {
        product.SKU = product.SKU.Trim();
        if (await db.Products.AnyAsync(existing => existing.SKU == product.SKU))
        {
            ModelState.AddModelError(nameof(product.SKU), "That SKU is already in use.");
        }

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Product {product.Name} was added.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> PurchaseOrders()
    {
        var orders = await db.PurchaseOrders.AsNoTracking()
            .Include(order => order.Supplier)
            .Include(order => order.Warehouse)
            .Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> NewPurchaseOrder()
    {
        await PopulatePurchaseOrderOptions();
        return View(new PurchaseOrderCreateViewModel
        {
            Lines = [new PurchaseOrderLineInput()]
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewPurchaseOrder(PurchaseOrderCreateViewModel model)
    {
        var lines = model.Lines
            .Where(line => line.ProductId > 0 || line.Quantity > 0 || line.UnitCost > 0)
            .ToList();

        if (lines.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Lines), "Add at least one product line.");
        }
        if (lines.Any(line => line.ProductId <= 0 || line.Quantity <= 0 || line.UnitCost < 0))
        {
            ModelState.AddModelError(nameof(model.Lines), "Every line needs a product and a quantity greater than zero.");
        }
        if (lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
        {
            ModelState.AddModelError(nameof(model.Lines), "Each product may appear only once on an order.");
        }

        var products = await db.Products.Where(product => product.IsActive).ToListAsync();
        var productIds = products.Select(product => product.Id).ToHashSet();
        if (lines.Any(line => !productIds.Contains(line.ProductId)))
        {
            ModelState.AddModelError(nameof(model.Lines), "One or more selected products are unavailable.");
        }

        var supplierExists = await db.Suppliers.AnyAsync(supplier => supplier.Id == model.SupplierId && supplier.IsActive);
        var warehouseExists = await db.Warehouses.AnyAsync(warehouse => warehouse.Id == model.WarehouseId);
        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Choose an active supplier.");
        }
        if (!warehouseExists)
        {
            ModelState.AddModelError(nameof(model.WarehouseId), "Choose a valid warehouse.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePurchaseOrderOptions();
            return View(model);
        }

        var order = new PurchaseOrder
        {
            Number = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            SupplierId = model.SupplierId,
            WarehouseId = model.WarehouseId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpectedAt = model.ExpectedAt,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            Status = PurchaseOrderStatuses.Open,
            Lines = lines.Select(line => new PurchaseOrderLine
            {
                ProductId = line.ProductId,
                OrderedQuantity = line.Quantity,
                UnitCost = line.UnitCost
            }).ToList()
        };

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Purchase order {order.Number} was created.";
        return RedirectToAction(nameof(PurchaseOrder), new { id = order.Id });
    }

    public async Task<IActionResult> PurchaseOrder(int id)
    {
        var order = await db.PurchaseOrders.AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.Warehouse)
            .Include(item => item.Lines)
                .ThenInclude(line => line.Product)
            .FirstOrDefaultAsync(item => item.Id == id);

        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id, ReceivePurchaseOrderViewModel model)
    {
        var order = await db.PurchaseOrders
            .Include(item => item.Lines)
                .ThenInclude(line => line.Product)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (order is null)
        {
            return NotFound();
        }
        if (order.Status == PurchaseOrderStatuses.Received)
        {
            TempData["Error"] = "This purchase order has already been fully received.";
            return RedirectToAction(nameof(PurchaseOrder), new { id });
        }

        var quantities = new List<(PurchaseOrderLine Line, decimal Quantity)>();
        foreach (var line in order.Lines)
        {
            model.Quantities.TryGetValue(line.Id, out var quantity);
            var remaining = line.OrderedQuantity - line.ReceivedQuantity;
            if (quantity < 0 || quantity > remaining)
            {
                ModelState.AddModelError(
                    $"Quantities[{line.Id}]",
                    $"Enter between 0 and {remaining.ToString("N2")} {line.Product.StockUnit}.");
            }
            if (quantity > 0)
            {
                quantities.Add((line, quantity));
            }
        }
        if (quantities.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Quantities), "Enter a received quantity for at least one line.");
        }
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(PurchaseOrder), new { id });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var receipt = new GoodsReceipt
        {
            Number = $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            PurchaseOrderId = order.Id,
            ReceivedAtUtc = DateTime.UtcNow,
            SupplierReference = string.IsNullOrWhiteSpace(model.SupplierReference)
                ? null
                : model.SupplierReference.Trim()
        };

        foreach (var (line, quantity) in quantities)
        {
            line.ReceivedQuantity += quantity;
            line.Version++;
            receipt.Lines.Add(new GoodsReceiptLine
            {
                PurchaseOrderLineId = line.Id,
                ProductId = line.ProductId,
                Quantity = quantity
            });
            db.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                WarehouseId = order.WarehouseId,
                Quantity = quantity,
                Type = "Purchase receipt",
                Reference = receipt.Number,
                CreatedAtUtc = receipt.ReceivedAtUtc
            });
        }

        order.Status = order.Lines.All(line => line.ReceivedQuantity >= line.OrderedQuantity)
            ? PurchaseOrderStatuses.Received
            : PurchaseOrderStatuses.PartiallyReceived;
        db.GoodsReceipts.Add(receipt);

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync();
            logger.LogWarning(exception, "Concurrent goods receipt rejected for purchase order {PurchaseOrderId}", id);
            TempData["Error"] = "Another receipt changed this order at the same time. Reload it and try again.";
            return RedirectToAction(nameof(PurchaseOrder), new { id });
        }

        TempData["Success"] = $"Goods receipt {receipt.Number} posted. Inventory was updated.";
        return RedirectToAction(nameof(PurchaseOrder), new { id });
    }

    public async Task<IActionResult> Inventory()
    {
        var movements = await db.StockMovements.AsNoTracking()
            .Include(movement => movement.Product)
            .Include(movement => movement.Warehouse)
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .ToListAsync();
        var movementBalances = movements
            .GroupBy(movement => new { movement.ProductId, movement.WarehouseId })
            .ToDictionary(
                group => (group.Key.ProductId, group.Key.WarehouseId),
                group => (OnHand: group.Sum(movement => movement.Quantity),
                    LastMovementUtc: (DateTime?)group.Max(movement => movement.CreatedAtUtc)));
        var products = await db.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync();
        var warehouses = await db.Warehouses.AsNoTracking().OrderBy(warehouse => warehouse.Name).ToListAsync();
        var balances = products
            .SelectMany(product => warehouses.Select(warehouse =>
            {
                var balance = movementBalances.GetValueOrDefault((product.Id, warehouse.Id));
                return new InventoryRowViewModel(product, warehouse, balance.OnHand, balance.LastMovementUtc);
            }))
            .OrderBy(row => row.Product.Name)
            .ToList();
        ViewBag.Movements = movements.Take(30).ToList();
        return View(balances);
    }

    private async Task PopulatePurchaseOrderOptions()
    {
        ViewBag.Suppliers = new SelectList(
            await db.Suppliers.AsNoTracking().Where(supplier => supplier.IsActive).OrderBy(supplier => supplier.Name).ToListAsync(),
            nameof(Supplier.Id), nameof(Supplier.Name));
        ViewBag.Warehouses = new SelectList(
            await db.Warehouses.AsNoTracking().OrderBy(warehouse => warehouse.Name).ToListAsync(),
            nameof(Warehouse.Id), nameof(Warehouse.Name));
        ViewBag.Products = await db.Products.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToListAsync();
    }
}

public sealed record InventoryRowViewModel(Product Product, Warehouse Warehouse, decimal OnHand, DateTime? LastMovementUtc);
