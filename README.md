# Northstar Warehouse Procurement

A C# / ASP.NET Core 10 procurement and inbound-inventory starter application. It provides a responsive browser UI for supplier and product records, purchase orders, partial goods receipts, and a stock movement ledger.

## Run locally

Requirements: .NET 10 SDK.

```sh
dotnet restore
dotnet run
```

Open the local URL printed by ASP.NET Core. On first start, the application creates `warehouse-procurement.db` in the application directory and adds a `MAIN` warehouse. No internet connection is needed after the application and its dependencies are available locally. Staff on the same LAN can use the host machine's LAN address while that host and network are available.

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
