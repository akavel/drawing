open System
open System.Drawing
open System.Windows.Forms

// TODO: right-click or finger-click opens a menu with 3 sliders for choosing color in hcl space
// TODO: the tool-window stays on top, can be easily closed, and also has color picker
// TODO: add F# REPL
// TODO: in future, make fullscreen: FormBorderStyle=FormBorderStyle.None; but find out a way to revert this
let form = new Form(Visible=true, Text="Drawing App", WindowState=FormWindowState.Maximized)

type Canvas() =
    inherit Control()
    override c.OnResize(e:EventArgs) =
        // Make sure to repaint the whole window when resized
        c.Refresh()
    override c.OnPaint(e:PaintEventArgs) =
        //System.Diagnostics.Debug.WriteLine("OnPaint")
        base.OnPaint(e)
        let g = e.Graphics
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

// Based on http://easyrgb.com/index.php?X=MATH&H=10#text10
// via http://stackoverflow.com/a/7561257/98528
// googled based on https://en.wikipedia.org/wiki/Lab_color_space#Cylindrical_representation:_CIELCh_or_CIEHLC
// via https://bl.ocks.org/mbostock/3014589
// via https://bl.ocks.org/mbostock/3e115519a1b495e0bd95
// + also based on https://github.com/d3/d3-color/blob/1bd8cce7f7bf18eefdbadcbe78774bf0e07a2b39/src/lab.js
type Color = 
    | CIELCH of L:float * C:float * H:float  // H=[0,360] C=[0,100] L=[0,150] // TODO: verify if ok
    | CIELab of L:float * a:float * b:float
    | XYZ of X:float * Y:float * Z:float
    | RGB of R:float * G:float * B:float
let lchToLab (CIELCH (l, c, h)) =
    let deg2rad = System.Math.PI/180.0
    let hRad = h * deg2rad
    CIELab (l, cos(hRad)*c, sin(hRad)*c)
let labToXyz (CIELab (L, a, b)) =
    let t0 = 4./29.
    let t1 = 6./29.
    let t2 = 3.*t1*t1
    let convert factor n = (if n > t1
                            then n*n*n
                            else t2*(n-t0)) * factor * 0.01
    let tmpy = (L+16.)/116.
    let tmpx = tmpy + a/500.
    let tmpz = tmpy - b/200.
    let xfact = 95.047
    let yfact = 100.000
    let zfact = 108.883
    XYZ (convert xfact tmpx, convert yfact tmpy, convert zfact tmpz)
let xyz2rgb (XYZ (x, y, z)) =
    let conv1 fx fy fz = (fx*x + fy*y + fz*z)*0.01
    let conv2 n = if n > 0.0031308
                  then 1.055 * Math.Pow(n, 1./2.4) - 0.055
                  else 12.92 * n
    RGB (conv1 324.06 -153.72 -49.86 |> conv2,
         conv1 -96.89  187.58   4.15 |> conv2,
         conv1   5.57  -20.40 105.70 |> conv2)

let toolboxLayout = new TableLayoutPanel(Dock=DockStyle.Fill)
toolbox.Controls.Add(toolboxLayout)
toolboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle()) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle()) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle()) |> ignore
let colorDisplay = new Panel(Dock=DockStyle.Fill, BackColor=Color.Green)
toolboxLayout.Controls.Add(colorDisplay)
let s1::s2::s3::[] = [1..3] |> List.map (fun _ ->
    let s = new TrackBar(Dock=DockStyle.Fill, Maximum=10000, Value=5000, TickStyle=TickStyle.None)
    toolboxLayout.Controls.Add(s)
    s)
let recolor _ =
    let s (s:TrackBar) = float s.Value / float s.Maximum
    let lch = CIELCH (150.*s s3, 100.*s s2, 360.*s s1)
    System.Diagnostics.Debug.WriteLine(sprintf "%A" lch)
    let (RGB (r, g, b)) = lch |> lchToLab |> labToXyz |> xyz2rgb
    System.Diagnostics.Debug.WriteLine(sprintf "rgb %A %A %A" r g b)
    let i0 f = int (f*255.0)
    let i f = 
        let ii = i0 f
        // FIXME: ad-hoc fix by MC; is this OK?
        if ii < 0 then 0
          elif ii >= 255 then 255
          else ii
    colorDisplay.BackColor <- Color.FromArgb(i r, i g, i b)
s1.ValueChanged.Add(recolor)
s2.ValueChanged.Add(recolor)
s3.ValueChanged.Add(recolor)
recolor ()

[<STAThread>]
Application.Run(form)