﻿open System
open System.Drawing
open System.Drawing.Drawing2D
open System.Runtime.InteropServices
open System.Windows.Forms

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

// TODO: right-click or finger-click opens a menu with 3 sliders for choosing color in hcl space
// TODO: the tool-window stays on top, can be easily closed, and also has color picker
// TODO: add F# REPL
// TODO: in future, make fullscreen: FormBorderStyle=FormBorderStyle.None; but find out a way to revert this
let form = new Form(Visible=true, Text="Drawing App", WindowState=FormWindowState.Maximized)

type Canvas() =
    inherit Control()
    let bitmap = new Bitmap(640, 480)
    do
        for x = 0 to bitmap.Width-1 do
            for y = 0 to bitmap.Height-1 do
                bitmap.SetPixel(x, y, Color.Black)
        let b = bitmap
        let g = Graphics.FromImage(bitmap)
        g.DrawLine(Pens.Yellow, 0, b.Height-1, b.Width-1, 0)
    override c.OnResize(e:EventArgs) =
        // Make sure to repaint the whole window when resized
        c.Refresh()
    override c.OnPaint(e:PaintEventArgs) =
        System.Diagnostics.Debug.WriteLine("OnPaint {0}x{1}", c.ClientSize.Width, c.ClientSize.Height)
        base.OnPaint(e)
        let ys = float c.ClientSize.Width / float bitmap.Width
        let xs = float c.ClientSize.Height / float bitmap.Height
        let rescale n = n * int (min xs ys) |> int
        let g = e.Graphics
        g.InterpolationMode <- InterpolationMode.NearestNeighbor
        g.DrawImage(bitmap, 0, 0, rescale bitmap.Width, rescale bitmap.Height)
        g.DrawLine(Pens.Blue, 0, 0, c.Width, c.Height)
        let rect = new Rectangle(100,100,200,200)
        g.DrawEllipse(Pens.Black, rect)
        g.DrawRectangle(Pens.Red, rect)
    
let canvas = new Canvas(Dock=DockStyle.Fill)
form.Controls.Add(canvas)

type Toolbox() =
    inherit Form(ShowInTaskbar=false, FormBorderStyle=FormBorderStyle.SizableToolWindow)
    override t.OnFormClosing(e) =
        // Just hide the window when user clicks the [x] button
        if e.CloseReason = CloseReason.UserClosing then 
            t.Hide()
            e.Cancel <- true

let toolbox = new Toolbox(Visible=true, Text="testing", TopMost=true)
canvas.MouseClick.Add(fun e -> toolbox.Visible <- not toolbox.Visible)

// from: https://blogs.msdn.microsoft.com/cjacks/2006/04/12/converting-from-hsb-to-rgb-in-net/
let ahsbToColor a h s b =
    // TODO: verify that args are in proper ranges:
    // a [0,255]
    // h [0,360.]
    // s [0,1.]
    // b [0,1.]
    let max, min =
        if b > 0.5
        then b-b*s+s, b+b*s-s
        else b+b*s,   b-b*s
    let sextant = int (h / 60.)
    let h =
        (if h >= 300.
         then h - 360.
         else h)  / 60. - 2. * float ((sextant+1) % 6 / 2)
    let mid =
        if sextant % 2 = 0
        then min + h*(max-min)
        else min - h*(max-min)
    let i n = int (n * 255.)
    let rgb r g b = Color.FromArgb(a, i r, i g, i b)
    match sextant with
    | 1 -> rgb mid max min
    | 2 -> rgb min max mid
    | 3 -> rgb min mid max
    | 4 -> rgb mid min max
    | 5 -> rgb max min mid
    | _ -> rgb max mid min

// TODO: maybe use HSB instead of LCH? should be much simpler, Color object seems to have conversion methods
// Based on http://easyrgb.com/index.php?X=MATH&H=10#text10
// via http://stackoverflow.com/a/7561257/98528
// googled based on https://en.wikipedia.org/wiki/Lab_color_space#Cylindrical_representation:_CIELCh_or_CIEHLC
// via https://bl.ocks.org/mbostock/3014589
// via https://bl.ocks.org/mbostock/3e115519a1b495e0bd95
// + also based on https://github.com/d3/d3-color/blob/1bd8cce7f7bf18eefdbadcbe78774bf0e07a2b39/src/lab.js
type CIELCH = CIELCH of L:float * C:float * H:float  // H=[0,360] C=[0,100] L=[0,150] // TODO: verify if ok
let (|CIELab|) (CIELCH (l,c,h)) =
    let deg2rad = System.Math.PI/180.0
    let hRad = h * deg2rad
    (l, cos(hRad)*c, sin(hRad)*c)
let (|XYZ|) (CIELab (L, a, b)) =
    let t0 = 4./29.
    let t1 = 6./29.
    let t2 = 3.*t1*t1
    let convert n = 
        if n > t1
        then n*n*n
        else t2*(n-t0)
    let tmpy = (L+16.)/116.
    let tmpx = tmpy + a/500.
    let tmpz = tmpy - b/200.
    (0.95047 * convert tmpx, 1. * convert tmpy, 1.08883 * convert tmpz)
let (|RGB|) (XYZ (x, y, z)) =
    let conv1 fx fy fz = (fx*x + fy*y + fz*z)*0.01
    let conv2 n = 
        if n > 0.0031308
        then 1.055 * n**(1./2.4) - 0.055
        else 12.92 * n
    (conv1 324.06 -153.72 -49.86 |> conv2,
     conv1 -96.89  187.58   4.15 |> conv2,
     conv1   5.57  -20.40 105.70 |> conv2)

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