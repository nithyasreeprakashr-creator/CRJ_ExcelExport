# CRJ Swagger Excel Export (.NET 10 + PostgreSQL)

This solution connects to PostgreSQL, exposes CRJ data through Swagger, and downloads an Excel workbook containing **only** these four worksheets: `WELFARE`, `HOUSING`, `TRAINING`, and `LEGAL`. The provided workbook template is included under `Templates/CRJTemplate.xlsx`.

## Prerequisites

- .NET 10 SDK
- PostgreSQL server (default local port: `5432`)
- Visual Studio 2022 with a .NET 10-compatible SDK, or VS Code

## 1. Create the database

In pgAdmin, connect to your PostgreSQL server, create a database named `crj_db`, select it, open Query Tool, and run `Database/create_crj_schema.sql`.

The SQL creates:
- `CRJ_Record_Mapping_Details`
- `CRJ_Header`
- `CRJ_Details`

The duplicate `Interest` field in the original specification cannot be represented twice in one table, so the second Excel Interest field is stored as `Interest_2` (C# property `Interest2`). `Detail_ID` is an internal identity primary key; `Record_ID` remains the foreign key to `CRJ_Header`.

## 2. Configure the PostgreSQL connection

Edit `appsettings.json` and replace `CHANGE_ME` with your local PostgreSQL password:

```json
"ConnectionStrings": {
  "CrjDatabase": "Host=localhost;Port=5432;Database=crj_db;Username=postgres;Password=CHANGE_ME"
}
```

For real environments, do not commit credentials. Use .NET user-secrets or environment variables, e.g. `ConnectionStrings__CrjDatabase`.

## 3. Choose schema initialization and sample data

`appsettings.json` has:

```json
"Database": {
  "InitializeOnStartup": true,
  "SeedDemoData": true
}
```

- `InitializeOnStartup=true`: calls EF Core `EnsureCreatedAsync` for a fresh empty demo database. It does **not** migrate an existing schema.
- `SeedDemoData=true`: inserts sample rows if the header table is empty.

The included SQL script creates the schema. If you run the SQL script first, the app will connect to those tables and seed sample data. To use only your own existing production tables/data, set `SeedDemoData=false`; set `InitializeOnStartup=false` if schema is managed by your DBA.

## 4. Run

From this folder:

```powershell
dotnet restore
dotnet build
dotnet run
```

Open the URL printed in the terminal with `/swagger` appended. The root `/` endpoint is intentionally not defined; there is no homepage redirect.

## 5. Swagger endpoints

- `GET /api/crj/database/health` — tests PostgreSQL connectivity.
- `GET /api/crj/records` — returns headers and related details from PostgreSQL as JSON.
- `GET /api/crj/export/excel` — downloads an `.xlsx` workbook containing only the four required worksheets.

## Excel export notes

The workbook template is copied to the build output. The export service populates the target sheets from records where `Report_Type` or the parent header's `Record_Code` identifies the sheet. Other template sheets are removed from the exported workbook. The worksheet layout is based on the supplied template and uses row 10 as the first data row.

## Common issues

- **Connection refused**: confirm PostgreSQL service is running and host/port are correct.
- **Password authentication failed**: update the password in `appsettings.json` or the `ConnectionStrings__CrjDatabase` environment variable.
- **Relation/table does not exist**: run `Database/create_crj_schema.sql` against `crj_db` and verify the exact quoted table names.
- **Swagger only shows root or no API endpoints**: this solution has no root endpoint; open `/swagger` and check build output for startup errors.
- **Template missing**: ensure `Templates/CRJTemplate.xlsx` exists and its build action copies to output (configured in the project file).

## SQL Server later

Keep the controllers and Excel service database-agnostic. To move to SQL Server later, install `Microsoft.EntityFrameworkCore.SqlServer`, change `UseNpgsql(...)` to `UseSqlServer(...)`, and configure a SQL Server connection string. Review SQL Server identity/column type compatibility and run the schema creation/migration appropriate to that database. Do not run PostgreSQL SQL against SQL Server.


## SQL Server query version history

The `Database/crj_query_version_history.sql` script creates:
- `dbo.CRJ_QueryVersion` — stores successful query versions in SQL Server.
- `dbo.usp_ExecuteAndVersionCRJQuery` — executes a trusted SQL batch and stores a new sequential version after success.
- `dbo.usp_GetCRJQueryHistory` — lists saved versions.
- `dbo.usp_GetCRJQueryVersion` — retrieves one version's SQL text without executing it.
- `dbo.usp_RevertCRJQueryVersion` — re-executes a selected historical query as a NEW version; it does not undo prior data changes.

### Setup
1. Open the SQL Server database used by this project in SSMS.
2. Open and run `Database/crj_query_version_history.sql`.
3. Test with the example calls at the bottom of that script.
4. Run `EXEC dbo.usp_GetCRJQueryHistory @QueryName = N'CRJ_dataEntry';` to verify history.

### Safety and scope
This is a SQL Server database-only query history feature. It does not create `.sql` files and is not automatically wired to a new Swagger endpoint. Run the stored procedures directly in SSMS or call them from a controlled backend service. Restrict execution to trusted operators: the execution procedure runs the supplied SQL text. Use one SQL batch per call; do not include `GO`, `USE`, or transaction-control statements in the submitted query text. Re-executing an old `INSERT`, `UPDATE`, or `DELETE` query may change data again; query-version revert is not data rollback.

The project ZIP also retains the existing PostgreSQL/EF Core and SQL Server/ADO.NET setup, existing records and Excel export endpoints, and the four-sheet template (WELFARE, HOUSING, TRAINING, LEGAL) with header colours preserved and body fills cleared. The database connections and build have not been live-tested in this environment; configure your own connection strings before running.


## Calculation-enabled Excel export

The export uses the Open XML SDK to update the supplied `Templates/CRJTemplate.xlsx` workbook. This avoids requiring ClosedXML to parse the full workbook. Existing worksheets, styles, and formulas are preserved; columns S (Total Deposit), T (System Difference), and Z (Under Paid) are not overwritten with database values. If rows extend beyond existing formulas, the export adds formulas for those rows and marks the workbook for full recalculation when opened in Microsoft Excel.

Note: Open XML writes and preserves formulas but does not itself calculate formula results. Microsoft Excel recalculates the formulas when the downloaded workbook is opened.
