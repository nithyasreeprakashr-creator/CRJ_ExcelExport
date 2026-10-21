
using CRJSwaggerExcelExport_NET10.Data;
using CRJSwaggerExcelExport_NET10.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRJSwaggerExcelExport_NET10.Controllers;

[ApiController]
[Route("api/crj")]
public class CrjController(
	CrjDbContext db,
    SqlServerCrjRepository sqlServer,
    IConfiguration configuration,
	ExcelExportService excelExportService) : ControllerBase
{
    private bool IsSqlServer => string.Equals(configuration["Database:Provider"], "SqlServer", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Returns CRJ headers and their details from PostgreSQL.
	/// </summary>
	[HttpGet("records")]
	public async Task<IActionResult> GetRecords(
		CancellationToken cancellationToken)
	{
		var records = IsSqlServer
            ? await sqlServer.GetRecordsAsync(cancellationToken)
            : await db.CrjHeaders
			.AsNoTracking()
			.Include(x => x.MappingDetail)
			.Include(x => x.Details)
			.OrderBy(x => x.RecordId)
			.ToListAsync(cancellationToken);

		var result = records.Select(h => new
		{
			h.RecordId,
			h.RecordCode,
			Description = h.MappingDetail?.Description,
			h.TimeStamp,
			h.UserId,

			Details = h.Details.Select(d => new
			{
				d.Id,
				d.RecordId,
				d.FileId,
				d.JobName,
				d.RcvdDateType,
				d.DepositDate,
				d.Days,
				d.Hours,
				d.DueAmount,
				d.AmountRecd,
				d.AdjHrs,
				d.AdjDue,
				d.AdjPaid,
				d.Interest,
				d.LiqDamgs,
				d.LegalFees,
				d.Principal,
				d.Claims,
				d.Cost,
				d.Legal,
				d.TotalDeposit,
				d.SystemDifference,
				d.EmployerNo,
				d.EmployerName,
				d.ReportType,
				d.WorkMonth,
				d.Comments,
				d.UnderPaid,
				d.SideNotes,
				d.BatchedBy,
				d.BatchedByDate,
				d.KeyedBy,
				d.KeyedByDate,
				d.CheckedBy,
				d.CheckedByDate
			})
		});

		return Ok(result);
	}

	/// <summary>
	/// Downloads the calculation-enabled CRJ Excel template populated with data.
	/// All worksheets in the template are preserved; the four main CRJ sheets are populated.
	/// </summary>
	[HttpGet("export/excel")]
	[Produces(
		"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
	public async Task<IActionResult> ExportExcel(
		CancellationToken cancellationToken)
	{
		var bytes = await excelExportService.BuildWorkbookAsync(
			cancellationToken);

		var fileName =
			$"CRJ_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

		return File(
			bytes,
			"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
			fileName);
	}
}
