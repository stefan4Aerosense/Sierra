using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

// Minimal Excel (.xlsx) writer - enough for a table of readings, with no external library.
//
// Why not just a CSV: this machine's decimal separator is a comma, so Excel reads "0.86" from a CSV as text
// and the columns cannot be charted or averaged. Numbers inside an .xlsx are stored raw, with a full stop,
// and Excel formats them using whatever locale the reader has. Both files are written for every session.
class Xlsx {
  public class Sheet {
    public string Name;
    public List<string> Headers = new List<string>();
    public List<object[]> Rows = new List<object[]>(); // string, double, null (blank)
    public Sheet(string name) { Name = name; }
  }

  static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

  public static void Write(string path, params Sheet[] sheets) {
    if (File.Exists(path)) File.Delete(path);
    using (var zip = ZipFile.Open(path, ZipArchiveMode.Create)) {
      Put(zip, "[Content_Types].xml", ContentTypes(sheets.Length));
      Put(zip, "_rels/.rels",
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>");
      Put(zip, "xl/workbook.xml", Workbook(sheets));
      Put(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Length));
      Put(zip, "xl/styles.xml", Styles());
      for (int i = 0; i < sheets.Length; i++) Put(zip, "xl/worksheets/sheet" + (i + 1) + ".xml", SheetXml(sheets[i]));
    }
  }

  static void Put(ZipArchive zip, string name, string content) {
    var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
    using (var w = new StreamWriter(entry.Open(), new UTF8Encoding(false))) w.Write(content);
  }

  static string ContentTypes(int sheetCount) {
    var sb = new StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
    sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
    sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
    sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
    sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
    for (int i = 1; i <= sheetCount; i++)
      sb.Append("<Override PartName=\"/xl/worksheets/sheet" + i + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
    sb.Append("</Types>");
    return sb.ToString();
  }

  static string Workbook(Sheet[] sheets) {
    var sb = new StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
    for (int i = 0; i < sheets.Length; i++)
      sb.Append("<sheet name=\"" + Esc(sheets[i].Name) + "\" sheetId=\"" + (i + 1) + "\" r:id=\"rId" + (i + 1) + "\"/>");
    sb.Append("</sheets></workbook>");
    return sb.ToString();
  }

  static string WorkbookRels(int sheetCount) {
    var sb = new StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
    for (int i = 1; i <= sheetCount; i++)
      sb.Append("<Relationship Id=\"rId" + i + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet" + i + ".xml\"/>");
    sb.Append("<Relationship Id=\"rId" + (sheetCount + 1) + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
    sb.Append("</Relationships>");
    return sb.ToString();
  }

  // Two styles: 0 = normal, 1 = bold (used for the header row).
  static string Styles() {
    return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
      "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
      "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
      "<borders count=\"1\"><border/></borders>" +
      "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
      "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
      "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
      "</styleSheet>";
  }

  static string SheetXml(Sheet s) {
    var sb = new StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
    int row = 1;
    if (s.Headers.Count > 0) {
      sb.Append("<row r=\"1\">");
      for (int c = 0; c < s.Headers.Count; c++) sb.Append(TextCell(Ref(c, 1), s.Headers[c], 1));
      sb.Append("</row>");
      row = 2;
    }
    foreach (var r in s.Rows) {
      sb.Append("<row r=\"" + row + "\">");
      for (int c = 0; c < r.Length; c++) {
        object v = r[c];
        if (v == null) continue;                       // blank cell: a second with no reading
        if (v is double || v is int) sb.Append("<c r=\"" + Ref(c, row) + "\"><v>" + Convert.ToDouble(v).ToString("0.####", Inv) + "</v></c>");
        else sb.Append(TextCell(Ref(c, row), v.ToString(), 0));
      }
      sb.Append("</row>");
      row++;
    }
    sb.Append("</sheetData></worksheet>");
    return sb.ToString();
  }

  static string TextCell(string reference, string text, int style) {
    return "<c r=\"" + reference + "\"" + (style > 0 ? " s=\"" + style + "\"" : "") + " t=\"inlineStr\"><is><t xml:space=\"preserve\">" + Esc(text) + "</t></is></c>";
  }

  static string Ref(int col, int row) {
    string name = "";
    for (int n = col; ; n = n / 26 - 1) { name = (char)('A' + n % 26) + name; if (n < 26) break; }
    return name + row;
  }

  static string Esc(string v) {
    return v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
  }
}
