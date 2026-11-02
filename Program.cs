using Microsoft.EntityFrameworkCore;
using Npgsql;
using WarehouseProcurement.Data;
using WarehouseProcurement.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("Procurement");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var databaseUrl = builder.Configuration["DATABASE_URL"];
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        connectionString = CreateConnectionStringFromDatabaseUrl(databaseUrl);
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure the 'Procurement' connection string or the DATABASE_URL environment variable.");
}

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

static string CreateConnectionStringFromDatabaseUrl(string databaseUrl)
{
    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
        || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
    {
        throw new InvalidOperationException(
            "DATABASE_URL must be a valid postgres:// or postgresql:// URL.");
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(uri.Host)
        || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
    {
        throw new InvalidOperationException(
            "DATABASE_URL must include a username, password, host, and database name.");
    }

    var connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = SslMode.Require
    };

    foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var pair = parameter.Split('=', 2);
        var key = Uri.UnescapeDataString(pair[0]);
        var value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;

        if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
        {
            connectionString.SslMode = value.ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable,
                "allow" => SslMode.Allow,
                "prefer" => SslMode.Prefer,
                "require" => SslMode.Require,
                "verify-ca" => SslMode.VerifyCA,
                "verify-full" => SslMode.VerifyFull,
                _ => throw new InvalidOperationException($"Unsupported DATABASE_URL sslmode '{value}'.")
            };
        }
    }

    return connectionString.ConnectionString;
}
