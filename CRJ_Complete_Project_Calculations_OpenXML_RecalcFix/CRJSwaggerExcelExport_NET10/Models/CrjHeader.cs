using System.ComponentModel.DataAnnotations;

namespace CRJSwaggerExcelExport_NET10.Models;

public class CrjHeader
{
    public int RecordId { get; set; }

    [Required, MaxLength(50)]
    public string RecordCode { get; set; } = string.Empty;

    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string UserId { get; set; } = "demo.user";

    public CrjRecordMappingDetail? MappingDetail { get; set; }
    public List<CrjDetail> Details { get; set; } = [];
}
