﻿open System
open System.Drawing
open System.Drawing.Drawing2D
open System.Runtime.InteropServices
open System.Windows.Forms

open Colors

// TODO: try embedding a manifest (http://stackoverflow.com/a/26365481/98528) for High DPI awareness, as
// it seems more "advised" than SetProcessDPIAware (https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266%28v=vs.85%29.aspx#supporting_dynamic_dpi_changes)
type PROCESS_DPI_AWARENESS =
    | Process_DPI_Unaware = 0
    | Process_System_DPI_Aware = 1
    | Process_Per_Monitor_DPI_Aware = 2
[<DllImport("SHCore.dll", SetLastError = true)>]
extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
let ok = SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_System_DPI_Aware)
if ok <> 0 then
    System.Diagnostics.Debug.WriteLine(sprintf "couldn't change DPI awareness: %A" ok)

// TODO: two-finger (or one-finger?) pan & zoom of the canvas on touch-enabled computers
// TODO: try to speed-up bitmap drawing by using Direct2D (e.g. D2D1CreateFactory via pinvoke?) or GDI
// TODO: right-click or finger-click opens a menu with 3 sliders for choosing color in hcl space
// TODO: the tool-window stays on top, can be easily closed, and also has color picker
// TODO: add F# REPL
// TODO: in future, make fullscreen: FormBorderStyle=FormBorderStyle.None; but find out a way to revert this

let form = new Form(Visible=true, Text="Drawing App", WindowState=FormWindowState.Maximized)

// TODO: change the code so that this can move below Canvas, to maintain reading order
type Toolbox() =
    inherit Form(ShowInTaskbar=false, FormBorderStyle=FormBorderStyle.SizableToolWindow)
    override t.OnFormClosing(e) =
        // Just hide the window when user clicks the [x] button
        if e.CloseReason = CloseReason.UserClosing then 
            t.Hide()
            e.Cancel <- true

let toolbox = new Toolbox(Visible=true, Text="testing", TopMost=true)

type Image() =
    let bitmap = new Bitmap(640, 480)
    do
        for x = 0 to bitmap.Width-1 do
            for y = 0 to bitmap.Height-1 do
                bitmap.SetPixel(x, y, Color.Black)
        let b = bitmap
        let g = Graphics.FromImage(bitmap)
        g.DrawLine(Pens.Yellow, 0, b.Height-1, b.Width-1, 0)
    member val Bitmap = bitmap

type Canvas() =
    inherit Control()
    let image = new Image()
    override c.OnResize(e:EventArgs) =
        // Make sure to repaint the whole window when resized
        c.Refresh()
    override c.OnPaint(e:PaintEventArgs) =
        //System.Diagnostics.Debug.WriteLine("OnPaint {0}x{1}", c.ClientSize.Width, c.ClientSize.Height)
        base.OnPaint(e)
        let ys = float c.ClientSize.Width / float image.Bitmap.Width
        let xs = float c.ClientSize.Height / float image.Bitmap.Height
        let rescale n = (float n) * (min xs ys) |> int
        let g = e.Graphics
        g.InterpolationMode <- InterpolationMode.NearestNeighbor
        g.DrawImage(image.Bitmap, 0, 0, rescale image.Bitmap.Width, rescale image.Bitmap.Height)
    // TODO: split "model" from "view"; or painting-related code from the GUI-mandated ceremony code
    override c.OnMouseClick(e:MouseEventArgs) =
        if not toolbox.Visible
        then toolbox.Show()
//        else c.AddVertex(e.X, e.Y)
//    member c.AddVertex(x, y) =        
    
let canvas = new Canvas(Dock=DockStyle.Fill)
form.Controls.Add(canvas)

let toolboxLayout = new TableLayoutPanel(Dock=DockStyle.Fill)
toolbox.Controls.Add(toolboxLayout)
toolboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40.f)) |> ignore
let colorDisplay = new Panel(Dock=DockStyle.Fill, BackColor=Color.Green)
toolboxLayout.Controls.Add(colorDisplay)
let tee = fun sideEffect x -> sideEffect x ; x
let s1::s2::s3::[] = [1..3] |> List.map (fun _ ->
    new HScrollBar(Dock=DockStyle.Fill, Maximum=10000, Value=5000, LargeChange=2000)
    |> tee toolboxLayout.Controls.Add)
toolbox.MinimumSize <- new Size(s1.PreferredSize.Width, 4*s1.PreferredSize.Height+(form.Height-form.ClientRectangle.Height))
toolbox.Height <- 2*(5*s1.PreferredSize.Height+(form.Height-form.ClientRectangle.Height))
toolbox.Width <- toolbox.Height
let recolor _ =
    let s (s:HScrollBar) = float s.Value / float s.Maximum
    let lch = CIELCH (150.*s s3, 100.*s s2, 360.*s s1)
    //System.Diagnostics.Debug.WriteLine(sprintf "%A" lch)
    let (RGB (r, g, b)) = lch
    //System.Diagnostics.Debug.WriteLine(sprintf "rgb %A %A %A" r g b)
    // FIXME: ad-hoc clipping to [0..255] by MC; is this OK?
    let i f = f*255.0 |> int |> max 0 |> min 255
    colorDisplay.BackColor <- Color.FromArgb(i r, i g, i b)
    //colorDisplay.BackColor <- ahsbToColor 255 (360.*s s1) (s s3) (s s2)
s1.ValueChanged.Add(recolor)
s2.ValueChanged.Add(recolor)
s3.ValueChanged.Add(recolor)
recolor ()

[<STAThread>]
Application.Run(form)