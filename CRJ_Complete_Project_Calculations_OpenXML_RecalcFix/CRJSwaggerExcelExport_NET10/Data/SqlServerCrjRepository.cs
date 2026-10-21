using System.Data;
using Microsoft.Data.SqlClient;
using CRJSwaggerExcelExport_NET10.Models;

namespace CRJSwaggerExcelExport_NET10.Data;

// SQL Server implementation intentionally uses ADO.NET, not Entity Framework.
public sealed class SqlServerCrjRepository(IConfiguration configuration)
{
    private SqlConnection CreateConnection() => new(configuration.GetConnectionString("SqlServerDatabase")
        ?? throw new InvalidOperationException("Missing SqlServerDatabase connection string."));

    public async Task<bool> CanConnectAsync(CancellationToken ct)
    {
        try { await using var connection = CreateConnection(); await connection.OpenAsync(ct); return true; }
        catch (SqlException) { return false; }
    }

    public async Task<List<CrjHeader>> GetRecordsAsync(CancellationToken ct)
    {
        var headers = new List<CrjHeader>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        const string headerSql = @"SELECT h.[Record_ID], h.[Record_Code], h.[Time_stamp], h.[User_id], m.[Description]
FROM [CRJ_Header] h LEFT JOIN [CRJ_Record_Mapping_Details] m ON m.[Record_Code] = h.[Record_Code]
ORDER BY h.[Record_ID]";
        await using (var command = new SqlCommand(headerSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var header = new CrjHeader
                {
                    RecordId = Convert.ToInt32(reader["Record_ID"]),
                    RecordCode = Convert.ToString(reader["Record_Code"]) ?? "",
                    TimeStamp = reader["Time_stamp"] is DBNull ? default : Convert.ToDateTime(reader["Time_stamp"]),
                    UserId = reader["User_id"] is DBNull ? null! : Convert.ToString(reader["User_id"])!,
                    MappingDetail = reader["Description"] is DBNull ? null : new CrjRecordMappingDetail
                    {
                        RecordCode = Convert.ToString(reader["Record_Code"]) ?? "",
                        Description = Convert.ToString(reader["Description"]) ?? ""
                    }
                };
                headers.Add(header);
            }
        }
        var lookup = headers.ToDictionary(h => h.RecordId);
        const string detailSql = @"SELECT
    d.[Detail_ID] AS [Id],
    d.[Record_ID] AS [RecordId],
    d.[File_ID] AS [FileId],
    d.[Job_Name] AS [JobName],
    d.[RCVD_DATE_TYPE] AS [RcvdDateType],
    d.[Deposit_Date] AS [DepositDate],
    d.[Days] AS [Days],
    d.[Hours] AS [Hours],
    d.[Due_Amount] AS [DueAmount],
    d.[Amount_Recd] AS [AmountRecd],
    d.[ADJ_Hrs] AS [AdjHrs],
    d.[ADJ_Due] AS [AdjDue],
    d.[ADJ_Paid] AS [AdjPaid],
    d.[Interest] AS [Interest],
    d.[Liq_Damgs] AS [LiqDamgs],
    d.[Legal_Fees] AS [LegalFees],
    d.[Principal] AS [Principal],
    d.[Claims] AS [Claims],
    d.[Cost] AS [Cost],
    d.[Legal] AS [Legal],
    d.[Total_Deposit] AS [TotalDeposit],
    d.[System Difference] AS [SystemDifference],
    d.[Employer_No] AS [EmployerNo],
    d.[Employer_Name] AS [EmployerName],
    d.[Report_Type] AS [ReportType],
    d.[Work_Month] AS [WorkMonth],
    d.[Comments] AS [Comments],
    d.[Under_Paid] AS [UnderPaid],
    d.[SIDE_NOTES] AS [SideNotes],
    d.[BATCHED_BY] AS [BatchedBy],
    d.[BATCHED_BY_DATE] AS [BatchedByDate],
    d.[KEYED_BY] AS [KeyedBy],
    d.[KEYED_BY_DATE] AS [KeyedByDate],
    d.[CHECKED_BY] AS [CheckedBy],
    d.[CHECKED_BY_DATE] AS [CheckedByDate]
FROM [CRJ_Details] d ORDER BY d.[Record_ID], d.[Detail_ID]";
        await using (var command = new SqlCommand(detailSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var detail = new CrjDetail
                {
                Id = GetValue<int>(reader, "Id") ?? 0,
                RecordId = GetValue<int>(reader, "RecordId") ?? 0,
                FileId = GetString(reader, "FileId"),
                JobName = GetString(reader, "JobName"),
                RcvdDateType = GetString(reader, "RcvdDateType"),
                DepositDate = GetValue<DateTime>(reader, "DepositDate"),
                Days = GetValue<int>(reader, "Days"),
                Hours = GetValue<decimal>(reader, "Hours"),
                DueAmount = GetValue<decimal>(reader, "DueAmount"),
                AmountRecd = GetValue<decimal>(reader, "AmountRecd"),
                AdjHrs = GetValue<decimal>(reader, "AdjHrs"),
                AdjDue = GetValue<decimal>(reader, "AdjDue"),
                AdjPaid = GetValue<decimal>(reader, "AdjPaid"),
                Interest = GetValue<decimal>(reader, "Interest"),
                LiqDamgs = GetValue<decimal>(reader, "LiqDamgs"),
                LegalFees = GetValue<decimal>(reader, "LegalFees"),
                Principal = GetValue<decimal>(reader, "Principal"),
                Claims = GetValue<decimal>(reader, "Claims"),
                Cost = GetValue<decimal>(reader, "Cost"),
                Legal = GetValue<decimal>(reader, "Legal"),
                TotalDeposit = GetValue<decimal>(reader, "TotalDeposit"),
                SystemDifference = GetValue<decimal>(reader, "SystemDifference"),
                EmployerNo = GetString(reader, "EmployerNo"),
                EmployerName = GetString(reader, "EmployerName"),
                ReportType = GetString(reader, "ReportType"),
                WorkMonth = GetString(reader, "WorkMonth"),
                Comments = GetString(reader, "Comments"),
                UnderPaid = GetValue<decimal>(reader, "UnderPaid"),
                SideNotes = GetString(reader, "SideNotes"),
                BatchedBy = GetString(reader, "BatchedBy"),
                BatchedByDate = GetValue<DateTime>(reader, "BatchedByDate"),
                KeyedBy = GetString(reader, "KeyedBy"),
                KeyedByDate = GetValue<DateTime>(reader, "KeyedByDate"),
                CheckedBy = GetString(reader, "CheckedBy"),
                CheckedByDate = GetValue<DateTime>(reader, "CheckedByDate"),
                };
                if (lookup.TryGetValue(detail.RecordId, out var header))
                {
                    detail.Header = header;
                    header.Details.Add(detail);
                }
            }
        }
        return headers;
    }

    private static string? GetString(SqlDataReader reader, string column) =>
        reader[column] is DBNull ? null : Convert.ToString(reader[column]);

    private static T? GetValue<T>(SqlDataReader reader, string column) where T : struct =>
        reader[column] is DBNull ? null : (T)Convert.ChangeType(reader[column], typeof(T));
}
