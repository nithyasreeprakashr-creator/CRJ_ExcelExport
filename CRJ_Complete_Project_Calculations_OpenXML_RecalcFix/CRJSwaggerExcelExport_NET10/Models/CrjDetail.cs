
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRJSwaggerExcelExport_NET10.Models;

[Table("CRJ_Details")]
public class CrjDetail
{
	// Internal primary key for each detail row.
	[Key]
	[Column("Detail_ID")]
	public int Id { get; set; }

	// Foreign key to CRJ_Header.
	[Column("Record_ID")]
	public int RecordId { get; set; }

	[MaxLength(20)]
	[Column("File_ID")]
	public string? FileId { get; set; }

	[MaxLength(50)]
	[Column("Job_Name")]
	public string? JobName { get; set; }

	[MaxLength(100)]
	[Column("RCVD_DATE_TYPE")]
	public string? RcvdDateType { get; set; }

	[Column("Deposit_Date", TypeName = "date")]
	public DateTime? DepositDate { get; set; }

	[Column("Days")]
	public int? Days { get; set; }

	[Column("Hours", TypeName = "numeric(10,4)")]
	public decimal? Hours { get; set; }

	[Column("Due_Amount", TypeName = "numeric(10,4)")]
	public decimal? DueAmount { get; set; }

	[Column("Amount_Recd", TypeName = "numeric(10,4)")]
	public decimal? AmountRecd { get; set; }

	[Column("ADJ_Hrs", TypeName = "numeric(10,4)")]
	public decimal? AdjHrs { get; set; }

	[Column("ADJ_Due", TypeName = "numeric(10,4)")]
	public decimal? AdjDue { get; set; }

	[Column("ADJ_Paid", TypeName = "numeric(10,4)")]
	public decimal? AdjPaid { get; set; }

	[Column("Interest", TypeName = "numeric(10,4)")]
	public decimal? Interest { get; set; }

	[Column("Liq_Damgs", TypeName = "numeric(10,4)")]
	public decimal? LiqDamgs { get; set; }

	[Column("Legal_Fees", TypeName = "numeric(10,4)")]
	public decimal? LegalFees { get; set; }

	[Column("Principal", TypeName = "numeric(10,4)")]
	public decimal? Principal { get; set; }

	[Column("Claims", TypeName = "numeric(10,4)")]
	public decimal? Claims { get; set; }

	[Column("Cost", TypeName = "numeric(10,4)")]
	public decimal? Cost { get; set; }

	[Column("Legal", TypeName = "numeric(10,4)")]
	public decimal? Legal { get; set; }

	[Column("Total_Deposit", TypeName = "numeric(10,4)")]
	public decimal? TotalDeposit { get; set; }

	[Column("System Difference", TypeName = "numeric(10,4)")]
	public decimal? SystemDifference { get; set; }

	[MaxLength(50)]
	[Column("Employer_No")]
	public string? EmployerNo { get; set; }

	[MaxLength(200)]
	[Column("Employer_Name")]
	public string? EmployerName { get; set; }

	[MaxLength(100)]
	[Column("Report_Type")]
	public string? ReportType { get; set; }

	[MaxLength(20)]
	[Column("Work_Month")]
	public string? WorkMonth { get; set; }

	[Column("Comments", TypeName = "text")]
	public string? Comments { get; set; }

	[Column("Under_Paid", TypeName = "numeric(10,4)")]
	public decimal? UnderPaid { get; set; }

	[Column("SIDE_NOTES", TypeName = "text")]
	public string? SideNotes { get; set; }

	[MaxLength(200)]
	[Column("BATCHED_BY")]
	public string? BatchedBy { get; set; }

	[Column("BATCHED_BY_DATE", TypeName = "date")]
	public DateTime? BatchedByDate { get; set; }

	[MaxLength(200)]
	[Column("KEYED_BY")]
	public string? KeyedBy { get; set; }

	[Column("KEYED_BY_DATE", TypeName = "date")]
	public DateTime? KeyedByDate { get; set; }

	[MaxLength(200)]
	[Column("CHECKED_BY")]
	public string? CheckedBy { get; set; }

	[Column("CHECKED_BY_DATE", TypeName = "date")]
	public DateTime? CheckedByDate { get; set; }

	public CrjHeader? Header { get; set; }
}
