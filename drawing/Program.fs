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

// TODO: fix "holes" between lines sometimes (they disappear after resize & redraw)
// TODO: two-finger (or one-finger?) pan & zoom of the canvas on touch-enabled computers
// TODO: the tool-window has color picker (pipette)
// TODO: add F# REPL
// TODO: in future, make fullscreen: FormBorderStyle=FormBorderStyle.None; but find out a way to revert this
// TODO: in future, consider trying to speed-up bitmap drawing by using Direct2D (e.g. D2D1CreateFactory via pinvoke?) or GDI

let rec pairwise f list =
    match list with
    | [] | [_] -> ()
    | a :: b :: rest ->
        f a b
        pairwise f (b::rest)
let dump msg obj =
    System.Diagnostics.Debug.WriteLine(msg + ": {0}", [obj])
    obj
let points2rect (p1:Point) (p2:Point) =
    Rectangle.FromLTRB(min p1.X p2.X, min p1.Y p2.Y, max p1.X p2.X + 1, max p1.Y p2.Y + 1)

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
    let mutable pen:Pen = Pens.White
    let mutable brush = new SolidBrush(pen.Color)
    let mutable polygon:Point list = []
    let mutable start:Point option = None
    let nearStart (p:Point) =
        let len x y = sqrt (x*x + y*y |> float)
        match start with
        | Some s when len (s.X-p.X) (s.Y-p.Y) <= 5. -> true, s
        | _ -> false, p
    let rec drawLines (points:Point list) (g:Graphics option) =
        let g = match g with
                | Some g -> g
                | None -> Graphics.FromImage(bitmap)
        points |> pairwise (fun p1 p2 -> g.DrawLine(pen, p1, p2))
    do
        let g = Graphics.FromImage(bitmap)
        g.FillRectangle(Brushes.Black, 0, 0, bitmap.Width, bitmap.Height)
    member val Bitmap = bitmap
    member val LastVertex =
        match polygon with
        | p :: rest -> Some p
        | [] -> None
    member img.Lines = polygon
    member img.Color
        with set (c:Color) =
            pen <- new Pen(c)
            brush <- new SolidBrush(c)
            drawLines polygon None
    member img.AddVertex(p:Point) =
        let g = Graphics.FromImage(bitmap)
        let close, p = nearStart p
        match polygon, close with
        | [], _ ->
            start <- Some p
        | last :: rest, false ->
            drawLines [last;p] None
        | _, true ->
            g.FillPolygon(brush, polygon |> List.toArray)
        match close with
        | false ->
            polygon <- p :: polygon
        | true ->
            polygon <- []
        p, close

type Canvas() as c =
    inherit Control()
    do
        c.DoubleBuffered <- true
    let image = new Image()
    let mutable scale = 1.
    let mutable line:(Point*Point) option = None
    let img2win coord = float coord * scale |> int
    let win2img coord = float coord / scale |> int
    let img2winP (p:Point) = new Point(img2win p.X, img2win p.Y)
    let repaint p1 p2 =
        c.Invalidate(points2rect p1 p2)
    member c.Color
        with set (col:Color) =
            image.Color <- col
            image.Lines
            |> List.map img2winP
            |> pairwise repaint
    override c.OnResize(e:EventArgs) =
        let ys = float c.ClientSize.Width / float image.Bitmap.Width
        let xs = float c.ClientSize.Height / float image.Bitmap.Height
        scale <- min xs ys
        // Make sure to repaint the whole window when resized
        c.Refresh()
    override c.OnPaint(e:PaintEventArgs) =
        //System.Diagnostics.Debug.WriteLine("OnPaint {0}x{1}", c.ClientSize.Width, c.ClientSize.Height)
        base.OnPaint(e)
        let g = e.Graphics
        g.InterpolationMode <- InterpolationMode.NearestNeighbor
        g.DrawImage(image.Bitmap, 0, 0, image.Bitmap.Width |> img2win, image.Bitmap.Height |> img2win)
        match line with
        | None -> ()
        | Some (s, e) -> g.DrawLine(Pens.Yellow, s, e)
    override c.OnMouseMove(ev:MouseEventArgs) =
        if toolbox.Visible then
            let p = new Point(ev.X, ev.Y)
            match line with
            | None -> ()
            | Some (s, e) ->
                repaint s e
                line <- Some (s, p)
                repaint s p
    // TODO: split "model" from "view"; or painting-related code from the GUI-mandated ceremony code
    override c.OnMouseClick(e:MouseEventArgs) =
        if not toolbox.Visible
        then toolbox.Show()
        else
            let p = new Point(e.X, e.Y)
            match line with
            | None -> ()
            | Some (s, e) ->
                repaint s e
                repaint s p
            let p, close = image.AddVertex(new Point(e.X |> win2img, e.Y |> win2img))
            let p = img2winP p
            match line with
            | None -> ()
            | Some (s, e) ->
                repaint s p
            match close with
            | true ->
                line <- None
                c.Refresh()
            | false ->
                line <- Some (p, p)
    
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
let recolorPanel _ =
    let s (s:HScrollBar) = float s.Value / float s.Maximum
    let lch = CIELCH (150.*s s3, 100.*s s2, 360.*s s1)
    //System.Diagnostics.Debug.WriteLine(sprintf "%A" lch)
    let (RGB (r, g, b)) = lch
    //System.Diagnostics.Debug.WriteLine(sprintf "rgb %A %A %A" r g b)
    // FIXME: ad-hoc clipping to [0..255] by MC; is this OK?
    let i f = f*255.0 |> int |> max 0 |> min 255
    colorDisplay.BackColor <- Color.FromArgb(i r, i g, i b)
    //colorDisplay.BackColor <- ahsbToColor 255 (360.*s s1) (s s3) (s s2)
let recolor _ =
    recolorPanel ()
    canvas.Color <- colorDisplay.BackColor
s1.ValueChanged.Add(recolor)
s2.ValueChanged.Add(recolor)
s3.ValueChanged.Add(recolor)
recolor ()

[<STAThread>]
Application.Run(form)