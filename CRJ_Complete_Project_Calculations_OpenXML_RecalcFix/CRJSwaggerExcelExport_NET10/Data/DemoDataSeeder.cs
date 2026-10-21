
using CRJSwaggerExcelExport_NET10.Models;
using Microsoft.EntityFrameworkCore;

namespace CRJSwaggerExcelExport_NET10.Data;

public static class DemoDataSeeder
{
	public static async Task SeedAsync(CrjDbContext db)
	{
		try
		{
			// 1. Ensure the four mapping records exist.
			var types = new[]
			{
				"WELFARE",
				"HOUSING",
				"TRAINING",
				"LEGAL"
			};

			var descriptions = new Dictionary<string, string>
			{
				["WELFARE"] = "Welfare CRJ records",
				["HOUSING"] = "Housing CRJ records",
				["TRAINING"] = "Training CRJ records",
				["LEGAL"] = "Legal CRJ records"
			};

			foreach (var type in types)
			{
				var mappingExists = await db.CrjRecordMappingDetails
					.AnyAsync(x => x.RecordCode == type);

				if (!mappingExists)
				{
					db.CrjRecordMappingDetails.Add(
						new CrjRecordMappingDetail
						{
							RecordCode = type,
							Description = descriptions[type]
						});
				}
			}

			await db.SaveChangesAsync();

			// 2. Avoid inserting duplicate demo headers.
			if (await db.CrjHeaders.AnyAsync())
			{
				Console.WriteLine(
					"CRJ seed skipped: header records already exist.");
				return;
			}

			// 3. Create four headers and three details per header.
			for (var i = 0; i < types.Length; i++)
			{
				var type = types[i];

				var header = new CrjHeader
				{
					RecordCode = type,

					// Compatible with PostgreSQL timestamp
					// without time zone.
					TimeStamp = DateTime.SpecifyKind(
						DateTime.UtcNow,
						DateTimeKind.Unspecified
					),

					UserId = "demo.user"
				};

				for (var j = 1; j <= 3; j++)
				{
					var amount = 100m * (i + 1) + j * 25.50m;

					header.Details.Add(new CrjDetail
					{
						FileId = $"FILE-{i + 1:00}{j:000}",
						JobName = $"{type}-{j:000}",
						RcvdDateType = j == 1 ? "UPLD" : "EML",

						DepositDate = new DateTime(
							2026, 9, Math.Min(10 + j, 28)),

						Days = 0,
						Hours = 12.5m + j,
						DueAmount = amount,

						AmountRecd =
							amount - (j == 2 ? 5m : 0m),

						AdjHrs = j == 2 ? 2.5m : 0m,
						AdjDue = j == 2 ? 17.30m : 0m,
						AdjPaid = 0m,

						// Only one Interest property is used.
						Interest = j == 3 ? 1.25m : 0m,

						LiqDamgs = 0m,
						LegalFees = 0m,
						Principal = 0m,
						Claims = 0m,
						Cost = 0m,
						Legal = 0m,

						TotalDeposit =
							amount - (j == 2 ? 5m : 0m),

						SystemDifference =
							j == 2 ? -5m : 0m,

						EmployerNo = $"{4710 + i}A",
						EmployerName = $"Demo Employer {i + 1}",
						ReportType = type,
						WorkMonth = "8/26",
						Comments = "Seeded demonstration record",
						UnderPaid = j == 2 ? -5m : 0m,
						SideNotes = "Sample data",

						BatchedBy = "Demo User",
						BatchedByDate = new DateTime(2026, 9, 19),

						KeyedBy = "Demo User",
						KeyedByDate = new DateTime(2026, 9, 19),

						CheckedBy = "Demo Reviewer",
						CheckedByDate = new DateTime(2026, 9, 20)
					});
				}

				db.CrjHeaders.Add(header);
			}

			// 4. Save all headers and details.
			await db.SaveChangesAsync();

			Console.WriteLine(
				"CRJ seed completed: 4 headers and 12 detail records.");
		}
		catch (DbUpdateException ex)
		{
			Console.WriteLine("CRJ database save failed.");

			Console.WriteLine(
				$"Database error: {ex.InnerException?.Message ?? ex.Message}");

			throw;
		}
	}
}
