# Assemble already-rendered Blender previews without external image dependencies.
Add-Type -AssemblyName System.Drawing
$out = 'C:\Users\Desktop\AppData\Local\Temp\opencode'
$names = @('HotSauce','CoffeeMug','PopTartToaster','CookieCrumbler','SourFizz')
$labels = @('HOT SAUCE','COFFEE MUG','POP-TART TOASTER','COOKIE CRUMBLER','SOUR FIZZ')
$canvas = New-Object System.Drawing.Bitmap 1800,1260
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.Clear([System.Drawing.Color]::FromArgb(236,240,244))
$font = New-Object System.Drawing.Font 'Segoe UI',20,([System.Drawing.FontStyle]::Bold)
$small = New-Object System.Drawing.Font 'Segoe UI',13
$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(30,40,55))
$g.DrawString('SNACK TOWER DEFENSE  /  APPROVED V3 MODELS',$font,$brush,30,16)
for($i=0;$i -lt 5;$i++) {
  $x=($i%3)*600; $y=[math]::Floor($i/3)*600+65
  $im=[System.Drawing.Image]::FromFile("$out\tower_$($names[$i])_model_v3.png")
  $g.DrawImage($im,[int]($x+15),[int]$y,570,540)
  $g.DrawString($labels[$i],$font,$brush,[single]($x+25),[single]($y+544))
  $im.Dispose()
}
$g.DrawString("Five static, low-poly towers`n1 m tall / -Y forward in Blender`nNo faces / no floating effects`n`nCookie disks are nested inside`nthe hopper, side-by-side.",$small,$brush,1230,780)
$path="$out\new_towers_model_contact_sheet_v3.png"
$canvas.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $canvas.Dispose(); $font.Dispose(); $small.Dispose(); $brush.Dispose()
foreach($name in $names){ Start-Process "$out\tower_${name}_model_v3.png" }
Start-Process $path
Write-Output $path
