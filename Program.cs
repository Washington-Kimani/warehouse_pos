using Microsoft.EntityFrameworkCore;
using WarehouseProcurement.Data;
using WarehouseProcurement.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("Procurement")
    ?? throw new InvalidOperationException("The 'Procurement' connection string is not configured.");

builder.Services.AddDbContext<ProcurementDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();
app.UseAuthorization();

// static files
app.UseStaticFiles();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
    db.Database.EnsureCreated();
    if (!db.Warehouses.Any())
    {
        db.Warehouses.Add(new Warehouse { Code = "MAIN", Name = "Main warehouse" });
        db.SaveChanges();
    }
}

app.Run();
