using System.ComponentModel.DataAnnotations;

namespace CRJSwaggerExcelExport_NET10.Models;

public class CrjRecordMappingDetail
{
    [Key, MaxLength(50)]
    public string RecordCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public List<CrjHeader> Headers { get; set; } = [];
}
