# Dual database setup (.NET 10)

- `Database:Provider = PostgreSQL`: original Entity Framework Core queries, local PostgreSQL.
- `Database:Provider = SqlServer`: ADO.NET (`Microsoft.Data.SqlClient`) for the same three Swagger endpoints.
- Update `ConnectionStrings:SqlServerDatabase` to your actual server, database and authentication. Do not commit real passwords.
- Set `Database:InitializeOnStartup = false` and `SeedDemoData = false` for SQL Server; SQL Server schema and records must already exist. Startup automatically skips PostgreSQL initialization in SQL Server mode.
- Expected SQL Server tables: `[CRJ_Header]`, `[CRJ_Details]`, `[CRJ_Record_Mapping_Details]`, with columns matching the PostgreSQL schema (including `[System Difference]`). Default schema is `dbo`; adjust repository SQL if your tables use another schema.
- Restore NuGet packages and run the project in Visual Studio. Test `/swagger`, `/api/crj/database/health`, `/api/crj/records`, `/api/crj/export/excel`.
- SQL Server health checks connectivity only; `records` checks table/query compatibility.
- Original PostgreSQL database, EF model, demo seeder, and Excel template are preserved.
- No live database connection or build was performed in this environment.



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
