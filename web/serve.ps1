# Serves this folder on http://localhost:8123 - localhost counts as a secure context, so Web Bluetooth works.
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add('http://localhost:8123/')
$listener.Start()
Write-Host "serving $root on http://localhost:8123/"
$types = @{ '.html' = 'text/html'; '.js' = 'text/javascript'; '.css' = 'text/css'; '.png' = 'image/png'; '.json' = 'application/json' }
while ($listener.IsListening) {
  $ctx = $listener.GetContext()
  $rel = $ctx.Request.Url.AbsolutePath.TrimStart('/')
  if ($rel -eq '') { $rel = 'app.html' }
  $path = Join-Path $root $rel
  if (Test-Path $path -PathType Leaf) {
    $bytes = [IO.File]::ReadAllBytes($path)
    $ext = [IO.Path]::GetExtension($path).ToLower()
    $ctx.Response.ContentType = if ($types.ContainsKey($ext)) { $types[$ext] } else { 'application/octet-stream' }
    $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    Write-Host "200 $rel"
  } else {
    $ctx.Response.StatusCode = 404
    Write-Host "404 $rel"
  }
  $ctx.Response.Close()
}
