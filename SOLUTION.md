# Solution: design and trade-offs

## Overview

The solution has two applications:

- **Backend**: an ASP.NET Core Web API on .NET 10 using Entity Framework Core 10 with SQLite. Writes
  go through REST controllers, and list reads go through OData endpoints that support `$filter`,
  `$orderby`, `$top`, `$skip` and `$count`.
- **Frontend**: an Angular 22 single-page app (standalone components and signals) with one
  reusable data table used by the Customers, Purchase orders and Products pages.

## Data model

| Entity | Key points |
|---|---|
| `Customer` | `Id`, `Name`, `Email`. Soft deleted (`IsDeleted`, `DeletedAt`). |
| `Product` | `Id`, `Name`, `Description`, `Price` (`decimal`). Soft deleted. |
| `PurchaseOrder` | `Id`, `CustomerId` (FK, one customer has many orders), `ClientReference` (unique index), `Timestamp` (`DateTimeOffset`, UTC), `Status`, `Total`, `IsArchived`, `ArchivedAt`. |
| `PurchaseOrderItem` | `Id`, `PurchaseOrderId` (FK), `ProductId` (FK), `Quantity` (`uint`), `UnitPrice`. |

Decisions:

- **Rules live in the model where possible.** The `Price` setter applies `Math.Abs`, so a negative
  price can't be stored no matter which code path sets it. `Quantity` is a `uint`, so negative
  quantities are rejected when the request is read, and validation requires at least 1. The
  `Timestamp` setter converts any offset to UTC.
- **Price at the time of ordering.** `PurchaseOrderItem.UnitPrice` is copied from the product when a
  line is added. Changing a product's price later doesn't change existing orders. On update,
  existing lines keep their captured price; new lines (or lines switched to another product) take
  the current price.
- **Nothing is physically deleted.** Customers and products are soft deleted, because existing orders
  still reference them and the history must stay readable. Purchase orders are archived. Deleted
  customers and products are hidden from lists and can't be used for new orders, but existing
  orders still show them.
- **Foreign keys use `Restrict`** for customer and product, so the database itself refuses a hard
  delete that would orphan orders. Items cascade with their order.

## Persistence: SQLite

SQLite was chosen because it needs no installation (it ships inside the NuGet package), works on
every OS, and still gives a real relational database: the unique index on `ClientReference` and the
foreign keys are enforced by the database, not just by application code.

The database is a file, `assignment.db`, configured in `appsettings.json`
(`ConnectionStrings:DefaultConnection`).

**Startup extension.** `DatabaseStartupExtensions.InitialiseDatabaseAsync()` runs before the app
serves requests. It creates the database if it doesn't exist, applies pending migrations, and
seeds sample data in Development (`Database:SeedSampleData`).

**SQLite type limitations.** SQLite has no native `decimal` or `DateTimeOffset` type, and EF Core
can't sort or compare those types in SQL on SQLite. This matters because the OData endpoints need
to sort by price and total and filter by date. Two value converters solve it while the C# model
keeps using `decimal` and `DateTimeOffset`:

- money is stored as **whole cents** in an `INTEGER` column (`MoneyToCentsConverter`), which keeps
  amounts exact and makes `ORDER BY price` and `price gt 100` run in SQL;
- `DateTimeOffset` is stored as a **sortable 64-bit integer** (`DateTimeOffsetToBinaryConverter`).
  Because every timestamp is UTC, the stored values sort chronologically.

Alternatives considered:

- **EF Core InMemory provider.** Simple, but it doesn't enforce unique indexes or foreign keys and
  doesn't support migrations.
- **SQL Server LocalDB / PostgreSQL.** These support the types natively, but need an install or Docker.

## API design

**REST for writes, OData for list reads.**

| Endpoint | Purpose |
|---|---|
| `/api/customers` | Get, add, update, delete (soft) |
| `/api/products` | Get, add, update, delete (soft) |
| `/api/purchaseorders` | Add, update, archive (delete), and get details with the backend-calculated total |
| `/odata/Customers`, `/odata/Products`, `/odata/PurchaseOrders` | Filtering, sorting, paging, counting |

- All controllers inherit from `ControllerBase`. The OData controllers derive from `ODataController`
  (itself a `ControllerBase`) and use their own names (`CustomersODataController`, …), so their
  routes can't clash with the REST controllers.
- **The OData endpoints return DTO projections, not entities.** Soft-deleted rows and internal fields
  never leak, the purchase order list can include computed columns (`customerName`, `itemCount`,
  `total`), and because the projection is an `IQueryable`, filtering and sorting still run in SQL.
- **Totals are calculated in the backend.** `PurchaseOrder.RecalculateTotal()` runs whenever an
  order is created or updated, and the total is stored so the list can sort and filter on it. The
  details endpoint recalculates the total from the lines when it reads the order.
- **Errors use ProblemDetails.** Validation failures return `400` with field errors, and conflicts
  (duplicate client reference, editing an archived order) return `409` with a readable message
  that the frontend shows as-is.

### Business rules

- **Status transitions** live in one place, `PurchaseOrderStatusRules`:

  ```
  Pending ──► Confirmed ──► Fulfilled
     │            │
     └────────────┴──► Cancelled        (Fulfilled and Cancelled are final)
  ```

  A change that doesn't make sense returns `400` with a helpful message under `errors.Status`,
- **Unique client reference.** The controller checks first so it can return a clear `409`. The
  unique index is the real guarantee: if two requests save the same reference at the same moment,
  the database rejects the second and that is also returned as `409`.
- **Archived orders are read-only.** Orders can't be created for deleted customers or with deleted
  products.

## Frontend design

- **Angular 22 with standalone components, signals and the new control flow** (`@if`, `@for`). No
  NgModules. Pages are lazy-loaded per route.
- **One reusable `DataTable` component** drives all three pages. A page passes an OData entity set
  and column definitions (`text`, `number`, `currency`, `date`, `enum`, `boolean`), and the table
  builds the OData query itself: `$filter` from the column filters, `$orderby` from the clicked
  header, `$top`/`$skip`/`$count` for paging.
  - The header holds the **Add** and **Update** buttons. Update acts on the row selected with the
    radio button. Each row has a **Delete** button (labelled **Archive** for purchase orders) with a
    confirmation dialog.
  - Each filter is one control that matches its column type: a text search, a comparison plus
    value for numbers and dates, or a dropdown for status. Active filters are highlighted and
    can be cleared one at a time.
- **Modals** use the browser's native `<dialog>` element, which provides focus trapping, Escape to
  close and a backdrop without extra libraries.
- **Purchase order form.** A reactive form with a `FormArray` of items. It shows the price that
  applies to each line ("as ordered" for existing lines) and an estimated total, labelled as an
  estimate because the backend total is authoritative.
- **Status rules in the UI.** The form explains which statuses are possible next and shows a
  message as soon as an invalid status is chosen, without a round trip. The backend still
  validates and has the final say; its message is shown if anything gets past the form.
- **Purchase order details modal.** Clicking an order shows the customer, the items with quantity
  and price at the time, and the total calculated by the backend.
- **Dev proxy.** `ng serve` forwards `/api` and `/odata` to the backend, so the app works without
  any CORS or URL configuration. CORS for `http://localhost:4200` is still configured in the API.

## Testing

- **Backend (xUnit).**
  - Domain tests cover the status rules and messages, the total calculation, UTC timestamps,
    archiving, soft deletes, positive prices and the cents conversion.
  - Controller tests run against a real SQLite in-memory database. They cover price capture,
    backend totals, duplicate client references (controller check and unique index), invalid
    status changes, archive instead of delete, read-only archived orders, hidden soft-deleted
    customers, and price/timestamp sorting and filtering in SQL.
- **Frontend (Vitest).** The navbar and the status rules.

## Trade-offs and limitations

| Trade-off | Why | What would change in production |
|---|---|---|
| SQLite with value converters | Zero-install setup while keeping database-enforced constraints | SQL Server or PostgreSQL with native `decimal`/`datetimeoffset` columns |
| Money stored with 2 decimals | Exact cents, sortable in SQL | Configurable scale per currency |
| Order total stored on the order | Lets OData sort/filter on total | Fine as a snapshot; could also be a database computed column |
| Status rules duplicated in frontend and backend | Instant feedback in the UI | Expose allowed transitions from an API endpoint so the UI reads them |
| Migrations applied at startup | Simple to run for a demo or assessment | Apply migrations in the deployment pipeline instead |
| No authentication or authorisation | Out of scope | ASP.NET Core authentication plus per-role policies |
| No concurrency control | Out of scope | A row version on `PurchaseOrder` to detect conflicting edits |
| Soft-deleted records can't be restored from the UI | Not required | Restore endpoints and an "include deleted" filter |

