using System;
using System.Threading;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Storage;
// pdfshot <in.pdf> <out-prefix> [maxPages] - renders PDF pages to PNG with the renderer built into Windows,
// so a generated report can be checked visually without installing any PDF tooling.
class PdfShot {
  static void Main(string[] a) { var t = new Thread(() => Run(a)); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(); }

  // Same spin-wait the BLE client uses: no async/await machinery needed for a one-shot tool.
  static T W<T>(IAsyncOperation<T> op) {
    var t0 = DateTime.Now;
    while (op.Status == AsyncStatus.Started) { if ((DateTime.Now - t0).TotalSeconds > 30) throw new TimeoutException(); Thread.Sleep(5); }
    if (op.Status != AsyncStatus.Completed) throw new Exception("failed: " + op.Status + " 0x" + op.ErrorCode.HResult.ToString("X8"));
    return op.GetResults();
  }
  static void W(IAsyncAction op) {
    var t0 = DateTime.Now;
    while (op.Status == AsyncStatus.Started) { if ((DateTime.Now - t0).TotalSeconds > 30) throw new TimeoutException(); Thread.Sleep(5); }
    if (op.Status != AsyncStatus.Completed) throw new Exception("failed: " + op.Status);
  }

  static void Run(string[] a) {
    string inPath = System.IO.Path.GetFullPath(a[0]), prefix = System.IO.Path.GetFullPath(a[1]);
    int max = a.Length > 2 ? int.Parse(a[2]) : 2;
    var doc = W(PdfDocument.LoadFromFileAsync(W(StorageFile.GetFileFromPathAsync(inPath))));
    Console.WriteLine("pages: " + doc.PageCount);
    for (uint i = 0; i < doc.PageCount && i < max; i++) {
      using (var p = doc.GetPage(i)) {
        string outPath = prefix + (i + 1) + ".png";
        if (!System.IO.File.Exists(outPath)) System.IO.File.WriteAllBytes(outPath, new byte[0]);
        var outFile = W(StorageFile.GetFileFromPathAsync(outPath));
        using (var s = W(outFile.OpenAsync(FileAccessMode.ReadWrite))) {
          W(p.RenderToStreamAsync(s, new PdfPageRenderOptions { DestinationWidth = 1100 }));
        }
        Console.WriteLine("wrote " + outPath);
      }
    }
  }
}
