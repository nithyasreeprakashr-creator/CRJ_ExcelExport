using System.Globalization;
using System.Text.RegularExpressions;
using CRJSwaggerExcelExport_NET10.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace CRJSwaggerExcelExport_NET10.Services;

public class ExcelExportService(
    CrjDbContext db,
    SqlServerCrjRepository sqlServer,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    private static readonly string[] TargetSheets =
    [
        "WELFARE",
        "HOUSING",
        "TRAINING",
        "LEGAL"
    ];

    public async Task<byte[]> BuildWorkbookAsync(
        CancellationToken cancellationToken = default)
    {
        bool isSqlServer = string.Equals(
            configuration["Database:Provider"],
            "SqlServer",
            StringComparison.OrdinalIgnoreCase);

        var records = isSqlServer
            ? (await sqlServer.GetRecordsAsync(cancellationToken))
                .SelectMany(header => header.Details)
                .ToList()
            : await db.CrjDetails
                .AsNoTracking()
                .Include(detail => detail.Header)
                .OrderBy(detail => detail.RecordId)
                .ThenBy(detail => detail.Id)
                .ToListAsync(cancellationToken);

        string[] possiblePaths =
        [
            Path.Combine(environment.ContentRootPath, "Templates", "CRJTemplate.xlsx"),
            Path.Combine(AppContext.BaseDirectory, "Templates", "CRJTemplate.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CRJTemplate.xlsx")
        ];

        string? templatePath = possiblePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);

        if (templatePath is null)
        {
            throw new FileNotFoundException(
                "CRJTemplate.xlsx was not found. Checked paths: " +
                string.Join(" | ", possiblePaths));
        }

        // Work on a copy in memory so the original template is never modified.
        await using var output = new MemoryStream();
        await using (var source = new FileStream(
            templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            await source.CopyToAsync(output, cancellationToken);
        }

        output.Position = 0;

        try
        {
            using (var document = SpreadsheetDocument.Open(output, true))
            {
                var workbookPart = document.WorkbookPart
                    ?? throw new InvalidOperationException("The Excel workbook has no workbook part.");

                var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList()
                    ?? throw new InvalidOperationException("The Excel workbook has no worksheets.");

                var worksheetParts = new Dictionary<string, WorksheetPart>(StringComparer.OrdinalIgnoreCase);

                foreach (var sheet in sheets)
                {
                    string name = sheet.Name?.Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(name) || sheet.Id?.Value is null)
                        continue;

                    if (workbookPart.GetPartById(sheet.Id.Value) is WorksheetPart worksheetPart)
                        worksheetParts[name] = worksheetPart;
                }

                var missingSheets = TargetSheets.Where(name => !worksheetParts.ContainsKey(name)).ToArray();
                if (missingSheets.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Required worksheets are missing from CRJTemplate.xlsx: " +
                        string.Join(", ", missingSheets) + ". Worksheets found: " +
                        string.Join(", ", worksheetParts.Keys));
                }

                foreach (string sheetName in TargetSheets)
                {
                    var worksheetPart = worksheetParts[sheetName];
                    var sheetRecords = records
                        .Where(item => string.Equals(
                            NormalizeType(item.ReportType ?? item.Header?.RecordCode),
                            sheetName,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    const uint firstDataRow = 10;

                    for (int i = 0; i < sheetRecords.Count; i++)
                    {
                        uint row = firstDataRow + (uint)i;
                        var item = sheetRecords[i];

                        // Input columns. Formula columns S, T and Z are intentionally not overwritten.
                        SetCellValue(worksheetPart, $"A{row}", item.RcvdDateType);
                        SetCellValue(worksheetPart, $"B{row}", item.DepositDate);
                        SetCellValue(worksheetPart, $"C{row}", item.FileId);
                        SetCellValue(worksheetPart, $"D{row}", item.Days);
                        SetCellValue(worksheetPart, $"E{row}", item.Hours);
                        SetCellValue(worksheetPart, $"F{row}", item.DueAmount);
                        SetCellValue(worksheetPart, $"G{row}", item.AmountRecd);
                        SetCellValue(worksheetPart, $"H{row}", item.AdjHrs);
                        SetCellValue(worksheetPart, $"I{row}", item.AdjDue);
                        SetCellValue(worksheetPart, $"J{row}", item.AdjPaid);
                        SetCellValue(worksheetPart, $"K{row}", item.Interest);
                        SetCellValue(worksheetPart, $"L{row}", item.LiqDamgs);
                        SetCellValue(worksheetPart, $"M{row}", item.LegalFees);
                        SetCellValue(worksheetPart, $"N{row}", item.Principal);
                        // Column O has no corresponding field in the current model.
                        SetCellValue(worksheetPart, $"O{row}", null);
                        SetCellValue(worksheetPart, $"P{row}", item.Claims);
                        SetCellValue(worksheetPart, $"Q{row}", item.Cost);
                        SetCellValue(worksheetPart, $"R{row}", item.Legal);

                        EnsureFormula(worksheetPart, row, "S", $"SUM(J{row}:R{row},G{row})");
                        EnsureFormula(worksheetPart, row, "T", $"(G{row}-F{row})+(J{row}-I{row})");
                        EnsureFormula(worksheetPart, row, "Z", $"G{row}-F{row}");

                        SetCellValue(worksheetPart, $"U{row}", item.EmployerNo);
                        SetCellValue(worksheetPart, $"V{row}", item.EmployerName);
                        SetCellValue(worksheetPart, $"W{row}", item.ReportType ?? item.Header?.RecordCode);
                        SetCellValue(worksheetPart, $"X{row}", item.WorkMonth);
                        SetCellValue(worksheetPart, $"Y{row}", item.Comments);
                        SetCellValue(worksheetPart, $"AA{row}", item.SideNotes);
                        SetCellValue(worksheetPart, $"AB{row}", item.BatchedBy);

						// Remove background fill colours from data rows A:AG.
						

					}
					// Remove conditional formatting rules from the worksheet.
					worksheetPart.Worksheet
						.Elements<ConditionalFormatting>()
						.ToList()
						.ForEach(rule => rule.Remove());
					worksheetPart.Worksheet.Save();
                }

                // Ask desktop Excel to recalculate formulas when the workbook is opened.
                var calcProperties = workbookPart.Workbook.CalculationProperties;
                if (calcProperties is null)
                {
                    calcProperties = new CalculationProperties();
                    workbookPart.Workbook.CalculationProperties = calcProperties;
                }

                calcProperties.CalculationMode = CalculateModeValues.Auto;
                calcProperties.FullCalculationOnLoad = true;
                calcProperties.ForceFullCalculation = true;
                workbookPart.Workbook.Save();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Could not update the CRJ Excel template '{Path.GetFileName(templatePath)}'. " +
                "The template has been left unchanged. Details: " + ex.Message,
                ex);
        }

        return output.ToArray();
    }

    private static void EnsureFormula(
        WorksheetPart worksheetPart,
        uint rowIndex,
        string column,
        string fallbackFormula)
    {
        string reference = $"{column}{rowIndex}";
        var cell = GetOrCreateCell(worksheetPart, reference);

        // Preserve existing formulas, but clear any cached result from the template.
        // Cached results can be stale (for example, zero displayed as a dash) after
        // input values change. The workbook calculation flags below ask spreadsheet
        // applications to calculate these formulas again when the file is opened.
        if (cell.CellFormula is not null && !string.IsNullOrWhiteSpace(cell.CellFormula.Text))
        {
            cell.CellValue = null;
            cell.DataType = null;
            return;
        }

        string? formulaToCopy = null;
        uint formulaSourceRow = 0;

        for (uint r = rowIndex - 1; r >= 10; r--)
        {
            var previous = FindCell(worksheetPart, $"{column}{r}");
            if (previous?.CellFormula is not null && !string.IsNullOrWhiteSpace(previous.CellFormula.Text))
            {
                formulaToCopy = previous.CellFormula.Text;
                formulaSourceRow = r;
                break;
            }

            if (r == 10)
                break;
        }

        string formula = formulaToCopy is null
            ? fallbackFormula
            : ShiftFormulaRows(formulaToCopy, (int)rowIndex - (int)formulaSourceRow);

        cell.CellFormula = new CellFormula(formula.TrimStart('='));
        cell.CellValue = null;
        cell.InlineString = null;
        cell.DataType = null;
    }

    private static string ShiftFormulaRows(string formula, int rowOffset)
    {
        return Regex.Replace(
            formula,
            @"(?<![A-Z0-9_])(?<column>\$?[A-Z]{1,3})(?<absolute>\$?)(?<row>\d+)",
            match =>
            {
                if (match.Groups["absolute"].Value == "$")
                    return match.Value;

                if (!int.TryParse(match.Groups["row"].Value, out int oldRow))
                    return match.Value;

                int newRow = oldRow + rowOffset;
                return newRow > 0
                    ? match.Groups["column"].Value + match.Groups["absolute"].Value + newRow
                    : match.Value;
            },
            RegexOptions.CultureInvariant);
    }

    private static void SetCellValue(WorksheetPart worksheetPart, string cellReference, object? value)
    {
        var cell = GetOrCreateCell(worksheetPart, cellReference);

        // Data cells are input cells; clear any previous value before writing new data.
        cell.CellValue = null;
        cell.InlineString = null;
        cell.CellFormula = null;
        cell.DataType = null;

        if (value is null)
            return;

        switch (value)
        {
            case DateTime dateTime:
                cell.DataType = CellValues.Number;
                cell.CellValue = new CellValue(dateTime.ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case DateOnly dateOnly:
                cell.DataType = CellValues.Number;
                cell.CellValue = new CellValue(dateOnly.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture));
                break;
            case decimal number:
                SetNumber(cell, (double)number);
                break;
            case int integer:
                SetNumber(cell, integer);
                break;
            case long longValue:
                SetNumber(cell, longValue);
                break;
            case double dbl:
                SetNumber(cell, dbl);
                break;
            case float single:
                SetNumber(cell, single);
                break;
            case bool boolean:
                cell.DataType = CellValues.Boolean;
                cell.CellValue = new CellValue(boolean ? "1" : "0");
                break;
            default:
                string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new InlineString(
                    new DocumentFormat.OpenXml.Spreadsheet.Text(text)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    });
                break;
        }
    }

    private static void SetNumber(Cell cell, double value)
    {
        cell.DataType = CellValues.Number;
        cell.CellValue = new CellValue(value.ToString("G17", CultureInfo.InvariantCulture));
    }

    private static Cell GetOrCreateCell(WorksheetPart worksheetPart, string cellReference)
    {
        var worksheet = worksheetPart.Worksheet;
        var sheetData = worksheet.GetFirstChild<SheetData>()
            ?? worksheet.AppendChild(new SheetData());

        uint rowIndex = GetRowIndex(cellReference);
        var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);

        if (row is null)
        {
            row = new Row { RowIndex = rowIndex };
            var nextRow = sheetData.Elements<Row>()
                .FirstOrDefault(r => r.RowIndex is not null && r.RowIndex.Value > rowIndex);

            if (nextRow is null)
                sheetData.Append(row);
            else
                sheetData.InsertBefore(row, nextRow);
        }

        var existing = row.Elements<Cell>().FirstOrDefault(c =>
            string.Equals(c.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return existing;

        int targetColumn = GetColumnIndex(cellReference);
        var nextCell = row.Elements<Cell>().FirstOrDefault(c =>
            c.CellReference is not null && GetColumnIndex(c.CellReference.Value!) > targetColumn);

        var newCell = new Cell { CellReference = cellReference };
        if (nextCell is null)
            row.Append(newCell);
        else
            row.InsertBefore(newCell, nextCell);

        return newCell;
    }

    private static Cell? FindCell(WorksheetPart worksheetPart, string cellReference)
    {
        uint rowIndex = GetRowIndex(cellReference);
        var row = worksheetPart.Worksheet.GetFirstChild<SheetData>()?
            .Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);

        return row?.Elements<Cell>().FirstOrDefault(c =>
            string.Equals(c.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase));
    }

    private static uint GetRowIndex(string cellReference)
    {
        string digits = new(cellReference.Where(char.IsDigit).ToArray());
        return uint.Parse(digits, CultureInfo.InvariantCulture);
    }

    private static int GetColumnIndex(string cellReference)
    {
        int column = 0;
        foreach (char c in cellReference.TakeWhile(char.IsLetter))
            column = column * 26 + char.ToUpperInvariant(c) - 'A' + 1;
        return column;
    }

	private static void RemoveRowBackgroundColor(
		WorksheetPart worksheetPart,
		uint rowIndex)
	{
		var worksheet = worksheetPart.Worksheet;
		var sheetData = worksheet.GetFirstChild<SheetData>();

		var row = sheetData?
			.Elements<Row>()
			.FirstOrDefault(r => r.RowIndex?.Value == rowIndex);

		if (row is null)
			return;

		var workbookPart = worksheetPart
			.GetParentParts()
			.OfType<WorkbookPart>()
			.FirstOrDefault();

		var stylesheet = workbookPart?
			.WorkbookStylesPart?
			.Stylesheet;

		if (stylesheet?.CellFormats is null ||
			stylesheet.Fills is null)
			return;

		// Find the existing default no-fill style.
		uint noFillStyleIndex = 0;

		var formats = stylesheet.CellFormats
			.Elements<CellFormat>()
			.ToList();

		for (int i = 0; i < formats.Count; i++)
		{
			var format = formats[i];

			if (format.FillId?.Value == 0 &&
				format.FontId?.Value == 0 &&
				format.BorderId?.Value == 0 &&
				format.NumberFormatId?.Value == 0)
			{
				noFillStyleIndex = (uint)i;
				break;
			}
		}

		foreach (var cell in row.Elements<Cell>())
		{
			// Remove cell-specific fill by using the default style.
			cell.StyleIndex = noFillStyleIndex;
		}

		worksheet.Save();
	}

	private static string NormalizeType(string? value)
    {
        string type = (value ?? string.Empty).Trim();
        foreach (string target in TargetSheets)
        {
            if (string.Equals(type, target, StringComparison.OrdinalIgnoreCase))
                return target;
        }
        return type.ToUpperInvariant();
    }
}
