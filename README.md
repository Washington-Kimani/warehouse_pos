# Northstar Warehouse Procurement

A C# / ASP.NET Core 10 procurement and inbound-inventory starter application. It provides a responsive browser UI for supplier and product records, purchase orders, partial goods receipts, and a stock movement ledger.

## Run with Docker

Requirements: Docker with the Compose plugin.

```sh
docker compose up --build
```

Open [http://localhost:8080](http://localhost:8080). Compose starts PostgreSQL, waits for its health check, then starts the application. On first start, the application creates the PostgreSQL schema and adds a `MAIN` warehouse. PostgreSQL data is stored in the `postgres_data` Docker volume and survives container restarts.

The default database credentials are for local development only. Set `POSTGRES_PASSWORD` (and optionally `POSTGRES_USER` and `POSTGRES_DB`) in the environment or a local `.env` file before using this setup beyond local development. You can change the host ports with `APP_PORT` and `POSTGRES_PORT`.

To run the app outside Docker against the Compose database, start PostgreSQL with `docker compose up -d database`, then run:

```sh
ConnectionStrings__Procurement='Host=localhost;Port=5432;Database=warehouse_procurement;Username=warehouse;Password=warehouse_dev_password' dotnet run
```

The previous `warehouse-procurement.db` SQLite file is not modified or imported; the PostgreSQL database starts fresh. To stop the containers while keeping the data, use `docker compose down`. To permanently delete the PostgreSQL data, use `docker compose down --volumes`.

## Procurement workflow

1. Add suppliers and products, including each product's stock unit and reorder level.
2. Create a purchase order for a supplier and destination warehouse.
3. Open the order and post one or more receipts. Partial deliveries are supported.
4. Each receipt updates the order's received quantity and creates receipt and stock-movement records in a single database transaction.
5. The inventory screen calculates warehouse balances from the movement ledger.

Receipts cannot exceed the remaining quantity on an order, and posted receipts are retained as ledger entries.

## Current scope

This is an initial procurement slice, not a feature-complete ERPNext replacement. It does not yet include user sign-in/role permissions, multi-unit conversions, purchase invoices/payables, approvals, supplier returns, stock transfers/adjustments, tax/accounting, or branch-local synchronization. Prices are stored as entered; currency and tax configuration are not yet implemented.

SQLite keeps the starter app self-contained for local evaluation. Before exposing it to a business LAN or relying on it for concurrent operational use, add authentication and authorization, database migrations and backups, and move to a server database such as PostgreSQL or SQL Server. The initial local-server mode survives an internet outage only while the application host and LAN remain available.
