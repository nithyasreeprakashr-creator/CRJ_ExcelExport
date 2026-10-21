
using CRJSwaggerExcelExport_NET10.Models;
using Microsoft.EntityFrameworkCore;

namespace CRJSwaggerExcelExport_NET10.Data;

public class CrjDbContext(DbContextOptions<CrjDbContext> options)
	: DbContext(options)
{
	public DbSet<CrjHeader> CrjHeaders => Set<CrjHeader>();

	public DbSet<CrjDetail> CrjDetails => Set<CrjDetail>();

	public DbSet<CrjRecordMappingDetail> CrjRecordMappingDetails =>
		Set<CrjRecordMappingDetail>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// CRJ_Record_Mapping_Details
		modelBuilder.Entity<CrjRecordMappingDetail>(e =>
		{
			e.ToTable("CRJ_Record_Mapping_Details");

			e.HasKey(x => x.RecordCode);

			e.Property(x => x.RecordCode)
				.HasColumnName("Record_Code")
				.HasMaxLength(50)
				.IsRequired();

			e.Property(x => x.Description)
				.HasColumnName("Description")
				.HasMaxLength(200);
		});

		// CRJ_Header
		modelBuilder.Entity<CrjHeader>(e =>
		{
			e.ToTable("CRJ_Header");

			e.HasKey(x => x.RecordId);

			e.Property(x => x.RecordId)
				.HasColumnName("Record_ID")
				.UseIdentityByDefaultColumn();

			e.Property(x => x.RecordCode)
				.HasColumnName("Record_Code")
				.HasMaxLength(50)
				.IsRequired();

			e.Property(x => x.TimeStamp)
				.HasColumnName("Time_stamp")
				.HasColumnType("timestamp without time zone");

			e.Property(x => x.UserId)
				.HasColumnName("User_id")
				.HasMaxLength(50);

			e.HasOne(x => x.MappingDetail)
				.WithMany(x => x.Headers)
				.HasForeignKey(x => x.RecordCode)
				.OnDelete(DeleteBehavior.Restrict);
		});

		// CRJ_Details
		modelBuilder.Entity<CrjDetail>(e =>
		{
			e.ToTable("CRJ_Details");

			e.HasKey(x => x.Id);

			e.Property(x => x.Id)
				.HasColumnName("Detail_ID")
				.UseIdentityByDefaultColumn();

			e.Property(x => x.RecordId)
				.HasColumnName("Record_ID");

			e.Property(x => x.FileId)
				.HasColumnName("File_ID")
				.HasMaxLength(20);

			e.Property(x => x.JobName)
				.HasColumnName("Job_Name")
				.HasMaxLength(50);

			e.Property(x => x.RcvdDateType)
				.HasColumnName("RCVD_DATE_TYPE")
				.HasMaxLength(100);

			e.Property(x => x.DepositDate)
				.HasColumnName("Deposit_Date")
				.HasColumnType("date");

			e.Property(x => x.Days)
				.HasColumnName("Days");

			e.Property(x => x.Hours)
				.HasColumnName("Hours")
				.HasPrecision(10, 4);

			e.Property(x => x.DueAmount)
				.HasColumnName("Due_Amount")
				.HasPrecision(10, 4);

			e.Property(x => x.AmountRecd)
				.HasColumnName("Amount_Recd")
				.HasPrecision(10, 4);

			e.Property(x => x.AdjHrs)
				.HasColumnName("ADJ_Hrs")
				.HasPrecision(10, 4);

			e.Property(x => x.AdjDue)
				.HasColumnName("ADJ_Due")
				.HasPrecision(10, 4);

			e.Property(x => x.AdjPaid)
				.HasColumnName("ADJ_Paid")
				.HasPrecision(10, 4);

			// One Interest column only; Interest2 removed.
			e.Property(x => x.Interest)
				.HasColumnName("Interest")
				.HasPrecision(10, 4);

			e.Property(x => x.LiqDamgs)
				.HasColumnName("Liq_Damgs")
				.HasPrecision(10, 4);

			e.Property(x => x.LegalFees)
				.HasColumnName("Legal_Fees")
				.HasPrecision(10, 4);

			e.Property(x => x.Principal)
				.HasColumnName("Principal")
				.HasPrecision(10, 4);

			e.Property(x => x.Claims)
				.HasColumnName("Claims")
				.HasPrecision(10, 4);

			e.Property(x => x.Cost)
				.HasColumnName("Cost")
				.HasPrecision(10, 4);

			e.Property(x => x.Legal)
				.HasColumnName("Legal")
				.HasPrecision(10, 4);

			e.Property(x => x.TotalDeposit)
				.HasColumnName("Total_Deposit")
				.HasPrecision(10, 4);

			e.Property(x => x.SystemDifference)
				.HasColumnName("System Difference")
				.HasPrecision(10, 4);

			e.Property(x => x.EmployerNo)
				.HasColumnName("Employer_No")
				.HasMaxLength(50);

			e.Property(x => x.EmployerName)
				.HasColumnName("Employer_Name")
				.HasMaxLength(200);

			e.Property(x => x.ReportType)
				.HasColumnName("Report_Type")
				.HasMaxLength(100);

			e.Property(x => x.WorkMonth)
				.HasColumnName("Work_Month")
				.HasMaxLength(20);

			e.Property(x => x.Comments)
				.HasColumnName("Comments")
				.HasColumnType("text");

			e.Property(x => x.UnderPaid)
				.HasColumnName("Under_Paid")
				.HasPrecision(10, 4);

			e.Property(x => x.SideNotes)
				.HasColumnName("SIDE_NOTES")
				.HasColumnType("text");

			e.Property(x => x.BatchedBy)
				.HasColumnName("BATCHED_BY")
				.HasMaxLength(200);

			e.Property(x => x.BatchedByDate)
				.HasColumnName("BATCHED_BY_DATE")
				.HasColumnType("date");

			e.Property(x => x.KeyedBy)
				.HasColumnName("KEYED_BY")
				.HasMaxLength(200);

			e.Property(x => x.KeyedByDate)
				.HasColumnName("KEYED_BY_DATE")
				.HasColumnType("date");

			e.Property(x => x.CheckedBy)
				.HasColumnName("CHECKED_BY")
				.HasMaxLength(200);

			e.Property(x => x.CheckedByDate)
				.HasColumnName("CHECKED_BY_DATE")
				.HasColumnType("date");

			e.HasOne(x => x.Header)
				.WithMany(x => x.Details)
				.HasForeignKey(x => x.RecordId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
