# Order desk: customers, products and purchase orders

A .NET 10 Web API with an Angular 22 frontend for managing customers, products and purchase orders.

| Folder | What it is |
|---|---|
| `Backend/` | ASP.NET Core Web API: EF Core + SQLite, REST controllers, OData query endpoints, xUnit tests |
| `Frontend/` | Angular app: navbar, reusable OData data table, add/update modals, purchase order details |

## Prerequisites

| Tool | Version | Check with | Notes |
|---|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | **10.0** (10.0.100 or later) | `dotnet --version` 
| [Node.js](https://nodejs.org/) | **24 LTS** (24.15.0+) or 22.22.3+ | `node --version`

## Run locally

Start the backend first, then the frontend, in two terminals.

### 1. Backend

```bash
cd Backend/Assignment.Api
dotnet run
```

### 2. Frontend

```bash
cd Frontend
npm install
npm start
```

Open http://localhost:4200. The dev server forwards `/api` and `/odata` requests to the backend on
port 5080 (`Frontend/proxy.conf.json`), so no CORS or URL setup is needed.

## Run the tests

```bash
cd Backend
dotnet test 
```

```bash
cd Frontend
npm test -- --watch=false
```
