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

let colorDisplay = new Panel(Dock=DockStyle.Fill, BackColor=Color.Green)
let s1 = new TrackBar(Dock=DockStyle.Fill)
let s2 = new TrackBar(Dock=DockStyle.Fill)
let s3 = new TrackBar(Dock=DockStyle.Fill)
let toolboxLayout = new TableLayoutPanel(Dock=DockStyle.Fill)
toolboxLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.f)) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle()) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle()) |> ignore
toolboxLayout.RowStyles.Add(new RowStyle()) |> ignore
toolboxLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.f)) |> ignore
toolboxLayout.Controls.Add(colorDisplay)
toolboxLayout.Controls.Add(s1)
toolboxLayout.Controls.Add(s2)
toolboxLayout.Controls.Add(s3) 
toolbox.Controls.Add(toolboxLayout)

[<STAThread>]
Application.Run(form)