using System;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
// imgconv <in> <out.png> [alphaThreshold] - decodes any format Windows knows (incl. WebP) and writes a PNG,
// turning near-white pixels transparent so a logo can sit on a dark background.
class ImgConv {
  static void Main(string[] a) { var t = new Thread(() => Run(a)); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(); }
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
    string inPath = System.IO.Path.GetFullPath(a[0]), outPath = System.IO.Path.GetFullPath(a[1]);
    int thresh = a.Length > 2 ? int.Parse(a[2]) : 235;
    var src = W(StorageFile.GetFileFromPathAsync(inPath));
    var dec = W(BitmapDecoder.CreateAsync(W(src.OpenAsync(FileAccessMode.Read))));
    var pixels = W(dec.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
      new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage));
    var px = pixels.DetachPixelData();
    int cleared = 0;
    for (int i = 0; i + 3 < px.Length; i += 4) {
      if (px[i] >= thresh && px[i + 1] >= thresh && px[i + 2] >= thresh) { px[i + 3] = 0; cleared++; }
    }
    Console.WriteLine("{0} x {1}, {2} pixels made transparent", dec.PixelWidth, dec.PixelHeight, cleared);
    if (!System.IO.File.Exists(outPath)) System.IO.File.WriteAllBytes(outPath, new byte[0]);
    var dst = W(StorageFile.GetFileFromPathAsync(outPath));
    using (var s = W(dst.OpenAsync(FileAccessMode.ReadWrite))) {
      s.Size = 0;
      var enc = W(BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, s));
      enc.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, dec.PixelWidth, dec.PixelHeight, 96, 96, px);
      W(enc.FlushAsync());
    }
    Console.WriteLine("wrote " + outPath);
  }
}
