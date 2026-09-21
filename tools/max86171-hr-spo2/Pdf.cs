using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// Minimal PDF writer - no external library. Uses the standard Helvetica fonts, which every PDF reader has,
// so the file opens on any machine with nothing installed.
//
// Page space is A4 in points (595 x 842), origin bottom-left.
class Pdf {
  public const double PageWidth = 595, PageHeight = 842;
  static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

  readonly List<string> pages = new List<string>();
  StringBuilder page;

  public void NewPage() { Flush(); page = new StringBuilder(); }
  void Flush() { if (page != null) pages.Add(page.ToString()); page = null; }

  public void Text(double x, double y, double size, string s, bool bold = false, double grey = 0) {
    page.AppendFormat(Inv, "BT /{0} {1} Tf {2:0.##} g {3:0.##} {4:0.##} Td ({5}) Tj ET\n",
      bold ? "F2" : "F1", N(size), grey, x, y, Escape(s));
  }

  public void Line(double x1, double y1, double x2, double y2, double width, double grey) {
    page.AppendFormat(Inv, "{0:0.##} w {1:0.##} G {2:0.##} {3:0.##} m {4:0.##} {5:0.##} l S\n", width, grey, x1, y1, x2, y2);
  }

  public void Polyline(IList<double[]> pts, double width, double r, double g, double b) {
    if (pts.Count < 2) return;
    page.AppendFormat(Inv, "{0:0.##} w {1:0.###} {2:0.###} {3:0.###} RG\n", width, r, g, b);
    page.AppendFormat(Inv, "{0:0.##} {1:0.##} m\n", pts[0][0], pts[0][1]);
    for (int i = 1; i < pts.Count; i++) page.AppendFormat(Inv, "{0:0.##} {1:0.##} l\n", pts[i][0], pts[i][1]);
    page.Append("S\n");
  }

  public void FillRect(double x, double y, double w, double h, double grey) {
    page.AppendFormat(Inv, "{0:0.##} g {1:0.##} {2:0.##} {3:0.##} {4:0.##} re f\n", grey, x, y, w, h);
  }

  public void Save(string path) {
    Flush();
    var objects = new List<string>();
    // Objects: 1 catalog, 2 pages, 3 font, 4 font bold, 5 spare, then per page a content stream (6, 8, ...)
    // followed by its page object (7, 9, ...). The Kids array must list the page objects, not the streams.
    var kids = new StringBuilder();
    for (int i = 0; i < pages.Count; i++) kids.AppendFormat(Inv, "{0} 0 R ", 7 + 2 * i);

    objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
    objects.Add("<< /Type /Pages /Count " + pages.Count + " /Kids [" + kids.ToString().Trim() + "] >>");
    objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
    objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
    objects.Add("<< >>"); // placeholder so page objects start at 'first'

    foreach (var content in pages) {
      objects.Add("<< /Length " + Encoding.ASCII.GetByteCount(content) + " >>\nstream\n" + content + "endstream");
      int contentObj = objects.Count; // 1-based index of the stream just added
      objects.Add(string.Format(Inv,
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0:0.##} {1:0.##}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {2} 0 R >>",
        PageWidth, PageHeight, contentObj));
    }

    var sb = new StringBuilder();
    sb.Append("%PDF-1.4\n");
    var offsets = new List<int>();
    foreach (var body in objects) {
      offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
      sb.Append(offsets.Count + " 0 obj\n" + body + "\nendobj\n");
    }
    int xref = Encoding.ASCII.GetByteCount(sb.ToString());
    sb.Append("xref\n0 " + (objects.Count + 1) + "\n0000000000 65535 f \n");
    foreach (var off in offsets) sb.Append(off.ToString("0000000000") + " 00000 n \n");
    sb.Append("trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");

    File.WriteAllBytes(path, Encoding.ASCII.GetBytes(sb.ToString()));
  }

  static string N(double v) { return v.ToString("0.##", Inv); }

  // PDF text strings: escape the delimiters, and drop anything outside plain ASCII.
  static string Escape(string s) {
    var sb = new StringBuilder();
    foreach (char c in s ?? "") {
      if (c == '(' || c == ')' || c == '\\') sb.Append('\\').Append(c);
      else if (c >= 32 && c < 127) sb.Append(c);
      else sb.Append(' ');
    }
    return sb.ToString();
  }
}
