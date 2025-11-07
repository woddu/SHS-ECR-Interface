using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WpfApplication1;

public class WorkbookService : IDisposable {
  private record GradeRange(
    double Min,
    double Max,
    int Transmuted
  );

  private readonly static List<GradeRange> gradeTable = [
    new GradeRange (100.00, 100.00, 100 ),
    new GradeRange (98.40, 99.99, 99 ),
    new GradeRange (96.80, 98.39, 98 ),
    new GradeRange (95.20, 96.79, 97 ),
    new GradeRange (93.60, 95.19, 96 ),
    new GradeRange (92.00, 93.59, 95 ),
    new GradeRange (90.40, 91.99, 94 ),
    new GradeRange (88.80, 90.39, 93 ),
    new GradeRange (87.20, 88.79, 92 ),
    new GradeRange (85.60, 87.19, 91 ),
    new GradeRange (84.00, 85.59, 90 ),
    new GradeRange (82.40, 83.99, 89 ),
    new GradeRange (80.80, 82.39, 88 ),
    new GradeRange (79.20, 80.79, 87 ),
    new GradeRange (77.60, 79.19, 86 ),
    new GradeRange (76.00, 77.59, 85 ),
    new GradeRange (74.40, 75.99, 84 ),
    new GradeRange (72.80, 74.39, 83 ),
    new GradeRange (71.20, 72.79, 82 ),
    new GradeRange (69.60, 71.19, 81 ),
    new GradeRange (68.00, 69.59, 80 ),
    new GradeRange (66.40, 67.99, 79 ),
    new GradeRange (64.80, 66.39, 78 ),
    new GradeRange (63.20, 64.79, 77 ),
    new GradeRange (61.60, 63.19, 76 ),
    new GradeRange (60.00, 61.59, 75 ),
    new GradeRange (56.00, 59.99, 74 ),
    new GradeRange (52.00, 55.99, 73 ),
    new GradeRange (48.00, 51.99, 72 ),
    new GradeRange (44.00, 47.99, 71 ),
    new GradeRange (40.00, 43.99, 70 ),
    new GradeRange (36.00, 39.99, 69 ),
    new GradeRange (32.00, 35.99, 68 ),
    new GradeRange (28.00, 31.99, 67 ),
    new GradeRange (24.00, 27.99, 66 ),
    new GradeRange (20.00, 23.99, 65 ),
    new GradeRange (16.00, 19.99, 64 ),
    new GradeRange (12.00, 15.99, 63 ),
    new GradeRange (8.00,  11.99, 62 ),
    new GradeRange (4.00,  7.99,  61 ),
    new GradeRange (0.00,  3.99,  60 )
  ];

  private static uint _maleScoresStartRow = 0u;
  private static uint _femaleScoresStartRow = 0u;
  private static uint _maleNamesStartRow = 0u;
  private static uint _femaleNamesStartRow = 0u;

  private static uint _maleStudentCount = 0u;
  private static uint _femaleStudentCount = 0u;

  public readonly static string[] tracks = [
    "Core Subject (All Tracks)",
    "Academic Track (except Immersion)",
    "Work Immersion/ Culminating Activity (for Academic Track)",
    "TVL/ Sports/ Arts and Design Track"
  ];

  public readonly static double[,] weightedScores = {
    { 0.25, 0.50, 0.25 }, // Core Subject (All Tracks)
    { 0.25, 0.45, 0.30 }, // Academic Track (except Immersion)
    { 0.35, 0.40, 0.25 }, // Work Immersion/ Culminating Activity (for Academic Track)
    { 0.20, 0.60, 0.20 }  // TVL/ Sports/ Arts and Design Track
  };

  private static readonly List<string> requiredSheetNames = [
        "INPUT DATA",
        "1ST",
        "2ND",
        "Final Semestral Grade"
  ];

  public string FilePath { get; private set; }

  public bool Quarter1 { get; private set; } = true;

  public string Track { get; set; }

  public List<string> WrittenWorks { get; private set; } = [];

  public List<string> PerformanceTasks { get; private set; } = [];

  public string MissingSheets { get; set; }

  public string Exam { get; private set; }

  public double[] WeightedScores { get; set; } = { weightedScores[0, 0], weightedScores[0, 1], weightedScores[0, 2] };
  public bool HasFileLoaded => !string.IsNullOrWhiteSpace(FilePath);

  public bool IsFileECR() {
    using (var doc = SpreadsheetDocument.Open(FilePath, false)) {
      var wbPart = doc.WorkbookPart;
      var sheetNames = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .Select(s => s.Name)
          .ToList();

      MissingSheets = string.Join(
          ", ",
          requiredSheetNames
          .Where(req => !sheetNames.Contains(req))
          .ToList()
      );

      // Check if all required sheets are present
      return requiredSheetNames.All(req => sheetNames.Contains(req));
    }
  }

  private static int GetTransmutedGrade(double initialGrade, List<GradeRange> table) {
    var match = table.FirstOrDefault(r => initialGrade >= r.Min && initialGrade <= r.Max);
    return match?.Transmuted ?? 0; // 0 if not found
  }

  public int GetComputedGrade(List<string> writtenWorksScores, List<string> performanceTaskScores, string examScore) {

    int trackIndex = Array.IndexOf(tracks, Track);

    double writtenWorksTotal = WrittenWorks
        .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0)
        .Sum();

    double wwPercentageScore = writtenWorksTotal > 0
        ? writtenWorksScores
            .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0)
            .Sum() / writtenWorksTotal * 100.0
        : 0;    
    
    double wwWeightedScore = wwPercentageScore * weightedScores[trackIndex,0];

    double performanceTasksTotal = PerformanceTasks
        .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0)
        .Sum();
    double ptPercentageScore = performanceTasksTotal > 0
        ? performanceTaskScores
            .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0)
            .Sum() / performanceTasksTotal * 100.0
        : 0;
    double ptWeightedScore = ptPercentageScore * weightedScores[trackIndex, 1];

    double examTotal = double.TryParse(Exam, NumberStyles.Any, CultureInfo.InvariantCulture, out var et) ? et : 0;
    double examPercentageScore = examTotal > 0
        ? (double.TryParse(examScore, NumberStyles.Any, CultureInfo.InvariantCulture, out var es) ? es : 0) / examTotal * 100.0
        : 0;

    double examWeightedScore = examPercentageScore * weightedScores[trackIndex, 2];

    double initialGrade = wwWeightedScore + ptWeightedScore + examWeightedScore;

    return GetTransmutedGrade(initialGrade, gradeTable);
  }

  public void LoadWorkbook(string path) {
    FilePath = path;
  }

  public void SetScoresOfItem(uint index, List<string> maleScores, List<string> femaleScores, ScoreType scoreType = ScoreType.WrittenWorks) {
    string targetColumn = scoreType switch {
      ScoreType.WrittenWorks => ColNumberToName(ColNameToNumber("F") + (int)index),
      ScoreType.PerformanceTasks => ColNumberToName(ColNameToNumber("S") + (int)index),
      ScoreType.Exam => ColNumberToName(ColNameToNumber("AF")),
      _ => throw new ArgumentOutOfRangeException(nameof(scoreType))
    };

    using SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true);
    WorkbookPart wbPart = doc.WorkbookPart;
    Sheet sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));

    if (sheet == null) return;

    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);

    // Write male scores
    for (int i = 0; i < maleScores.Count; i++) {
      uint rowIndex = (uint)(_maleScoresStartRow + i);
      string cellRef = targetColumn + rowIndex;
      InsertCellValue(wsPart.Worksheet.GetFirstChild<SheetData>(), cellRef, maleScores[i]);
    }

    // Write female scores
    for (int i = 0; i < femaleScores.Count; i++) {
      uint rowIndex = (uint)(_femaleScoresStartRow + i);
      string cellRef = targetColumn + rowIndex;
      InsertCellValue(wsPart.Worksheet.GetFirstChild<SheetData>(), cellRef, femaleScores[i]);
    }

    // Force recalculation if needed
    var calcProps = wbPart.Workbook.CalculationProperties;
    if (calcProps == null) {
      calcProps = new CalculationProperties();
      wbPart.Workbook.Append(calcProps);
    }
    calcProps.FullCalculationOnLoad = true;
    calcProps.ForceFullCalculation = true;
    calcProps.CalculationOnSave = true;
    wsPart.Worksheet.Save();
    wbPart.Workbook.Save();

  }


  public (List<string> male, List<string> female) GetScoresOfItem(uint index, ScoreType scoreType = ScoreType.WrittenWorks) {
    List<string> male = new List<string>();
    List<string> female = new List<string>();

    string targetColumn;
    targetColumn = scoreType switch {
      ScoreType.WrittenWorks  => ColNumberToName(ColNameToNumber("F") + (int)index),
      ScoreType.PerformanceTasks  => ColNumberToName(ColNameToNumber("S") + (int)index),
      ScoreType.Exam => ColNumberToName(ColNameToNumber("AF")),
      _ => throw new ArgumentOutOfRangeException(nameof(scoreType))
    };


    using SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true);

    WorkbookPart wbPart = doc.WorkbookPart;
    Sheet sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));

    if (sheet == null) return (male, female);

    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
    SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

    foreach (Row row in sheetData.Elements<Row>()) {
      if (row.RowIndex >= _maleScoresStartRow && row.RowIndex < (_maleScoresStartRow + _maleStudentCount)) {
        Cell cell = row.Elements<Cell>()
                       .FirstOrDefault(c => c.CellReference.Value.StartsWith(targetColumn));
        if (cell != null) {
          string value = GetCellValue(doc, cell);
          male.Add(value);
        }
      }
    }

    foreach (Row row in sheetData.Elements<Row>()) {
      if (row.RowIndex >= _femaleScoresStartRow && row.RowIndex < (_femaleScoresStartRow + _femaleStudentCount)) {
        Cell cell = row.Elements<Cell>()
                       .FirstOrDefault(c => c.CellReference.Value.StartsWith(targetColumn));
        if (cell != null) {
          string value = GetCellValue(doc, cell);
          female.Add(value);
        }
      }
    }

    return (male, female);
  }

  public void ChangeTrack(int index) {
    using SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true);

    WorkbookPart wbPart = doc.WorkbookPart;
    Sheet sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == requiredSheetNames[0]);
    if (sheet == null) return;
    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
    SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
    InsertCellValue(sheetData, "AE8", tracks[index]);
    // Force recalculation if needed
    var calcProps = wbPart.Workbook.CalculationProperties;
    if (calcProps == null) {
      calcProps = new CalculationProperties();
      wbPart.Workbook.Append(calcProps);
    }
    calcProps.FullCalculationOnLoad = true;
    calcProps.ForceFullCalculation = true;
    calcProps.CalculationOnSave = true;
    wsPart.Worksheet.Save();
    wbPart.Workbook.Save();

    Track = tracks[index];

    WeightedScores = index >= 0
        ? [weightedScores[index, 0], weightedScores[index, 1], weightedScores[index, 2]]
        : [weightedScores[0, 0], weightedScores[0, 1], weightedScores[0, 2]];

  }

  public void ChangeQuarter() {
    Quarter1 = !Quarter1;
    using SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, false);
    ReadHighestPossibleScores(doc);
  }

  public (List<string> maleNames, List<string> femaleNames) ReadAllNames() {
    using SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, false);

    WorkbookPart wbPart = doc.WorkbookPart;
    Sheet sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == requiredSheetNames[1]);

    if (sheet == null) return ([], []);

    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
    SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

    foreach (Row row in sheetData.Elements<Row>()) {
      // Build the cell reference for column A
      string cellRef = $"A{row.RowIndex}";
      Cell cell = row.Elements<Cell>()
                     .FirstOrDefault(c => c.CellReference == cellRef);

      string val = GetCellValue(doc, cell);

      if (string.Equals(val.Trim(), "male", StringComparison.OrdinalIgnoreCase)) {
        _maleScoresStartRow = (uint)row.RowIndex + 1u;
      } else if (string.Equals(val.Trim(), "female", StringComparison.OrdinalIgnoreCase)) {
        _femaleScoresStartRow = (uint)row.RowIndex + 1u;
      }
    }

    var (maleNames, femaleNames) = ReadNames(doc);
    ReadHighestPossibleScores(doc);    

    return (maleNames, femaleNames);
  }

  public static (List<string> maleNames, List<string> femaleNames) ReadNames(SpreadsheetDocument doc) {
    string columnLetter = "B";

    var males = new List<string>();
    var females = new List<string>();

    WorkbookPart wbPart = doc.WorkbookPart;
    Sheet sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == requiredSheetNames[0]);

    if (sheet == null) return (males, females);

    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
    SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

    foreach (Row row in sheetData.Elements<Row>()) {
      // Build the cell reference for column B
      string cellRef = $"A{row.RowIndex}";
      Cell cell = row.Elements<Cell>()
                     .FirstOrDefault(c => c.CellReference == cellRef);

      string val = GetCellValue(doc, cell);


      if (string.Equals(val.Trim(), "male", StringComparison.OrdinalIgnoreCase)) {
        _maleNamesStartRow = row.RowIndex + 1;
      } else if (string.Equals(val.Trim(), "female", StringComparison.OrdinalIgnoreCase)) {
        _femaleNamesStartRow = row.RowIndex + 1;
      }
    }

    var maleEndRow = _femaleNamesStartRow - 2;
    var femaleEndRow = _femaleNamesStartRow + 70;
    _maleStudentCount = 0u;
    for (uint rowIndex = _maleNamesStartRow; rowIndex <= maleEndRow; rowIndex++) {
      string cellRef = $"{columnLetter}{rowIndex}";
      Cell cell = sheetData.Descendants<Cell>()
                           .FirstOrDefault(c => c.CellReference == cellRef);

      string val = GetCellValue(doc, cell);
      if (!string.IsNullOrWhiteSpace(val)) {
        males.Add(val);
        _maleStudentCount++;
      }
    }
    _femaleStudentCount = 0u;
    for (uint rowIndex = _femaleNamesStartRow; rowIndex <= femaleEndRow; rowIndex++) {
      string cellRef = $"{columnLetter}{rowIndex}";
      Cell cell = sheetData.Descendants<Cell>()
                           .FirstOrDefault(c => c.CellReference == cellRef);

      string val = GetCellValue(doc, cell);
      if (!string.IsNullOrWhiteSpace(val)) {
        females.Add(val);
        _femaleStudentCount++;
      }
    }
    
    return (males, females);
  }
  
  public List<string> AppendAndSortNames(string newValue, bool male = true) {
    string columnLetter = "B";
    uint startRow = male ? _maleNamesStartRow : _femaleNamesStartRow;
    uint endRow = male ? _maleNamesStartRow + _maleStudentCount : _femaleNamesStartRow + _femaleStudentCount;

    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true)) {
      WorkbookPart wbPart = doc.WorkbookPart;
      Sheet sheet = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .FirstOrDefault(s => s.Name == requiredSheetNames[0]);

      if (sheet == null) return [];

      WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
      SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

      // 1️⃣ Read existing values
      List<string> values = [];
      for (uint rowIndex = startRow; rowIndex <= endRow; rowIndex++) {
        string cellRef = $"{columnLetter}{rowIndex}";
        Cell cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == cellRef);
        string val = GetCellValue(doc, cell);
        if (!string.IsNullOrWhiteSpace(val))
          values.Add(val);
      }

      // 2️⃣ Append new value
      if (!string.IsNullOrWhiteSpace(newValue))
        values.Add(newValue);

      // 3️⃣ Sort alphabetically
      values = values.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();

      // 4️⃣ Find a style to reuse
      uint? styleIndex = sheetData.Descendants<Cell>()
          .FirstOrDefault(c => GetColumnName(c.CellReference) == columnLetter && c.StyleIndex != null)
          ?.StyleIndex;

      // 5️⃣ Write sorted values back
      uint writeRow = startRow;
      foreach (var val in values) {
        if (writeRow > endRow) break;

        Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == writeRow);
        if (row == null) {
          row = new Row() { RowIndex = writeRow };
          sheetData.Append(row);
        }

        string cellRef = $"{columnLetter}{writeRow}";
        Cell cell = row.Elements<Cell>()
                       .FirstOrDefault(c => c.CellReference == cellRef);

        if (cell == null) {
          cell = new Cell() {
            CellReference = cellRef,
            StyleIndex = styleIndex
          };
          Cell refCell = row.Elements<Cell>()
                            .FirstOrDefault(c => string.Compare(c.CellReference.Value, cellRef, true) > 0);
          row.InsertBefore(cell, refCell);
        }

        // ✅ Update value without replacing the cell object
        cell.CellValue = new CellValue(val);
        cell.DataType = CellValues.String;

        writeRow++;
      }

      // 6️⃣ Clear remaining cells
      for (; writeRow <= endRow; writeRow++) {
        Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == writeRow);
        if (row != null) {
          string cellRef = $"{columnLetter}{writeRow}";
          Cell cell = row.Elements<Cell>()
                         .FirstOrDefault(c => c.CellReference == cellRef);
          if (cell != null) {
            cell.CellValue = null;
            cell.DataType = null; // keep style, clear value
          }
        }
      }

      // 7️⃣ Force Excel to recalc formulas on open
      var calcProps = wbPart.Workbook.CalculationProperties;
      if (calcProps == null) {
        calcProps = new CalculationProperties();
        wbPart.Workbook.Append(calcProps);
      }
      calcProps.FullCalculationOnLoad = true;
      calcProps.ForceFullCalculation = true;
      calcProps.CalculationOnSave = true;

      wsPart.Worksheet.Save();
      wbPart.Workbook.Save();

      var (males, females) = ReadNames(doc);
      return male ? males : females;
    }
  }

  public void ReadHighestPossibleScores(SpreadsheetDocument doc) {
    WrittenWorks.Clear();
    PerformanceTasks.Clear();
    uint fixedRow = 11u;
    WorkbookPart wbPart = doc.WorkbookPart;
    Sheet sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));

    if (sheet == null) return;

    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
    SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();


    Cell cell;
    string val;

    foreach (Row row in sheetData.Elements<Row>()) {
      // Build the cell reference for column A
      string cellRef = $"A{row.RowIndex}";
      cell = row.Elements<Cell>()
                     .FirstOrDefault(c => c.CellReference == cellRef);

      val = GetCellValue(doc, cell);

      if (string.Equals(val.Trim(), "male", StringComparison.OrdinalIgnoreCase)) {
        _maleScoresStartRow = (uint)row.RowIndex + 1u;
      } else if (string.Equals(val.Trim(), "female", StringComparison.OrdinalIgnoreCase)) {
        _femaleScoresStartRow = (uint)row.RowIndex + 1u;
      }
    }

    int startIdx, endIdx;
    string[,] Cols = {
                { "F", "O" },
                { "S", "AB" }
            };

    for (int i = 0; i < 2; i++) {
      startIdx = ColNameToNumber(Cols[i, 0]);
      endIdx = ColNameToNumber(Cols[i, 1]);

      for (int colIdx = startIdx; colIdx <= endIdx; colIdx++) {
        string colName = ColNumberToName(colIdx);
        string cellRef = $"{colName}{fixedRow}";

        cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == cellRef);

        val = GetCellValue(doc, cell); // ← your existing helper
                                       //if (!string.IsNullOrWhiteSpace(val)) {
        if (i == 0) {
          WrittenWorks.Add(val);
        } else if (i == 1) {
          PerformanceTasks.Add(val);
        }
      }
    }

    cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == "AF11");
    Exam = GetCellValue(doc, cell);

    sheet = wbPart.Workbook.Sheets
        .OfType<Sheet>()
        .FirstOrDefault(s => s.Name == (requiredSheetNames[0]));

    wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
    sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

    cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == "AE8");
    Track = GetCellValue(doc, cell);
    int index = Array.IndexOf(tracks, Track);

    WeightedScores = index >= 0
        ? [ weightedScores[index, 0], weightedScores[index, 1], weightedScores[index, 2] ]
        : [ weightedScores[0, 0], weightedScores[0, 1], weightedScores[0, 2] ];

  }

  public (List<string> writtenWorks, List<string> performanceTasks, string exam, string grade) ReadStudentScores(
    uint row, bool isMale
  ) {
    if (isMale) {
      row += _maleScoresStartRow;
    } else {
      row += _femaleScoresStartRow;
    }
    var values1 = new List<string>();
    var values2 = new List<string>();
    string examVal = "";
    string gradeVal = "";
    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, false)) {
      WorkbookPart wbPart = doc.WorkbookPart;
      Sheet sheet = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));

      if (sheet == null) return (values1, values2, examVal, gradeVal);

      WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
      SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

      int startIdx, endIdx;
      string[,] Cols = {
        { "F", "O" },
        { "S", "AB" }
      };
      Cell cell;
      string val;

      for (int i = 0; i < 2; i++) {
        startIdx = ColNameToNumber(Cols[i, 0]);
        endIdx = ColNameToNumber(Cols[i, 1]);

        for (int colIdx = startIdx; colIdx <= endIdx; colIdx++) {
          string colName = ColNumberToName(colIdx);
          string cellRef = $"{colName}{row}";

          cell = sheetData.Descendants<Cell>()
                               .FirstOrDefault(c => c.CellReference == cellRef);

          val = GetCellValue(doc, cell);
          if (i == 0) {
            values1.Add(val);
          } else {
            values2.Add(val);
          }

        }
      }
      cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == $"AF{row}");
      examVal = GetCellValue(doc, cell);
      cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == $"AI{row}");
      gradeVal = GetCellValue(doc, cell);
      return (values1, values2, examVal, gradeVal);
    }
  }

  public void EditHighestPossibleScore(List<string> scores, bool isWrittenWork = true) {
    uint fixedRow = 11u;

    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true)) {
      WorkbookPart wbPart = doc.WorkbookPart;
      Sheet sheet = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));

      if (sheet == null) return;

      WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
      SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

      string[,] Cols = {
            { "F", "O" },  // WrittenWorks
            { "S", "AB" }  // PerformanceTasks
        };

      int rangeIndex = isWrittenWork ? 0 : 1;
      int startIdx = ColNameToNumber(Cols[rangeIndex, 0]);
      int endIdx = ColNameToNumber(Cols[rangeIndex, 1]);

      int scoreIndex = 0;

      for (int colIdx = startIdx; colIdx <= endIdx && scoreIndex < scores.Count; colIdx++, scoreIndex++) {
        string colName = ColNumberToName(colIdx);
        string cellRef = $"{colName}{fixedRow}";

        // Find or create the cell
        Cell cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == cellRef);
        if (cell == null) {
          Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == fixedRow);
          if (row == null) {
            row = new Row() { RowIndex = fixedRow };
            sheetData.Append(row);
          }
          cell = new Cell() { CellReference = cellRef };
          row.Append(cell);
        }

        // Set the score
        cell.CellValue = new CellValue(scores[scoreIndex]);
        cell.DataType = CellValues.Number; // or String if needed
      }

      // Force recalculation if needed
      var calcProps = wbPart.Workbook.CalculationProperties;
      if (calcProps == null) {
        calcProps = new CalculationProperties();
        wbPart.Workbook.Append(calcProps);
      }
      calcProps.FullCalculationOnLoad = true;
      calcProps.ForceFullCalculation = true;
      calcProps.CalculationOnSave = true;

      wsPart.Worksheet.Save();
      wbPart.Workbook.Save();

      ReadHighestPossibleScores(doc);
    }
  }

  public void EditExamScore(string newValue) {
    string cellRef = $"AF11";
    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true)) {
      WorkbookPart wbPart = doc.WorkbookPart;
      Sheet sheet = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));
      if (sheet == null) return;
      WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
      SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

      Cell cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == cellRef);     
      
      cell.CellValue = new CellValue(newValue);
      cell.DataType = new EnumValue<CellValues>(CellValues.Number);

      // Force recalculation if needed
      var calcProps = wbPart.Workbook.CalculationProperties;
      if (calcProps == null) {
        calcProps = new CalculationProperties();
        wbPart.Workbook.Append(calcProps);
      }
      calcProps.FullCalculationOnLoad = true;
      calcProps.ForceFullCalculation = true;
      calcProps.CalculationOnSave = true;
      wsPart.Worksheet.Save();
      wbPart.Workbook.Save();
      Exam = newValue;
    }
  }

  public void EditStudentScore(List<string> scores, uint studentRow, bool isMale, bool isWrittenWork = true) {
    if (isMale) {
      studentRow += _maleScoresStartRow;
    } else {
      studentRow += _femaleScoresStartRow;
    }
    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true)) {
      WorkbookPart wbPart = doc.WorkbookPart;
      Sheet sheet = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));

      if (sheet == null) return;

      WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
      SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();

      string[,] Cols = {
            { "F", "O" },  // WrittenWorks
            { "S", "AB" }  // PerformanceTasks
        };

      int rangeIndex = isWrittenWork ? 0 : 1;
      int startIdx = ColNameToNumber(Cols[rangeIndex, 0]);
      int endIdx = ColNameToNumber(Cols[rangeIndex, 1]);

      int scoreIndex = 0;

      for (int colIdx = startIdx; colIdx <= endIdx && scoreIndex < scores.Count; colIdx++, scoreIndex++) {
        string colName = ColNumberToName(colIdx);
        string cellRef = $"{colName}{studentRow}";

        // Find or create the cell
        Cell cell = sheetData.Descendants<Cell>()
                             .FirstOrDefault(c => c.CellReference == cellRef);
        if (cell == null) {
          Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == studentRow);
          if (row == null) {
            row = new Row() { RowIndex = studentRow };
            sheetData.Append(row);
          }
          cell = new Cell() { CellReference = cellRef };
          row.Append(cell);
        }

        // Set the score
        cell.CellValue = new CellValue(scores[scoreIndex]);
        cell.DataType = new EnumValue<CellValues>(CellValues.Number); // or String if needed
      }

      // Force recalculation if needed
      var calcProps = wbPart.Workbook.CalculationProperties;
      if (calcProps == null) {
        calcProps = new CalculationProperties();
        wbPart.Workbook.Append(calcProps);
      }
      calcProps.FullCalculationOnLoad = true;
      calcProps.ForceFullCalculation = true;
      calcProps.CalculationOnSave = true;

      doc.WorkbookPart.Workbook.CalculationProperties.CalculationMode = CalculateModeValues.Auto;
      doc.WorkbookPart.Workbook.CalculationProperties.CalculationOnSave = true;
      doc.WorkbookPart.Workbook.CalculationProperties.ForceFullCalculation = true;

      wsPart.Worksheet.Save();
      wbPart.Workbook.Save();

      ReadHighestPossibleScores(doc);
    }
  }

  public void EditStudentExam(string newValue, uint row, bool isMale) {
    if (isMale) {
      row += _maleScoresStartRow;
    } else {
      row += _femaleScoresStartRow;
    }
    string cellRef = $"AF{row}";
    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(FilePath, true)) {
      WorkbookPart wbPart = doc.WorkbookPart;
      Sheet sheet = wbPart.Workbook.Sheets
          .OfType<Sheet>()
          .FirstOrDefault(s => s.Name == (Quarter1 ? requiredSheetNames[1] : requiredSheetNames[2]));
      if (sheet == null) return;
      WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id);
      SheetData sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
      InsertCellValue(sheetData, cellRef, newValue);
      // Force recalculation if needed
      var calcProps = wbPart.Workbook.CalculationProperties;
      if (calcProps == null) {
        calcProps = new CalculationProperties();
        wbPart.Workbook.Append(calcProps);
      }
      calcProps.FullCalculationOnLoad = true;
      calcProps.ForceFullCalculation = true;
      calcProps.CalculationOnSave = true;

      doc.WorkbookPart.Workbook.CalculationProperties.CalculationMode = CalculateModeValues.Auto;
      doc.WorkbookPart.Workbook.CalculationProperties.CalculationOnSave = true;
      doc.WorkbookPart.Workbook.CalculationProperties.ForceFullCalculation = true;


      wsPart.Worksheet.Save();
      wbPart.Workbook.Save();
    }
  }

  private int ColNameToNumber(string colName) {
    int sum = 0;
    foreach (char c in colName.ToUpper()) {
      sum *= 26;
      sum += (c - 'A' + 1);
    }
    return sum;
  }

  private string ColNumberToName(int colNumber) {
    string colName = "";
    while (colNumber > 0) {
      int rem = (colNumber - 1) % 26;
      colName = (char)('A' + rem) + colName;
      colNumber = (colNumber - 1) / 26;
    }
    return colName;
  }

  // Helpers
  private static string GetColumnName(string cellReference) =>
      new string(cellReference.Where(char.IsLetter).ToArray());

  private static string GetCellValue(SpreadsheetDocument doc, Cell cell) {
    if (cell == null || cell.CellValue == null)
      return "";

    if (cell.CellFormula != null && cell.CellValue != null)
      return cell.CellValue.InnerText;

    string value = cell.CellValue.InnerText;

    if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString) {
      return doc.WorkbookPart.SharedStringTablePart.SharedStringTable
                .Elements<SharedStringItem>()
                .ElementAt(int.Parse(value))
                .InnerText;
    }
    return value;
  }


  private void InsertCellValue(SheetData sheetData, string cellReference, string value, bool isNumeric = true) {
    string rowNumber = new string(cellReference.Where(char.IsDigit).ToArray());
    uint rowIndex = uint.Parse(rowNumber);

    Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
    if (row == null) {
      row = new Row() { RowIndex = rowIndex };
      sheetData.Append(row);
    }

    Cell cell = row.Elements<Cell>()
                   .FirstOrDefault(c => c.CellReference?.Value == cellReference);

    if (cell == null) {
      // Insert in correct order
      Cell refCell = null;
      foreach (Cell existingCell in row.Elements<Cell>()) {
        if (string.Compare(existingCell.CellReference.Value, cellReference, true) > 0) {
          refCell = existingCell;
          break;
        }
      }

      cell = new Cell() { CellReference = cellReference };
      row.InsertBefore(cell, refCell);
    }

    cell.CellValue = new CellValue(value);
    cell.DataType = new EnumValue<CellValues>(isNumeric ? CellValues.Number : CellValues.String);
  }


  public void Dispose() {
    // Nothing to dispose unless you keep a persistent open document
  }
}
