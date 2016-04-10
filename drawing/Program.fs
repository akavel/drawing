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
        c.Refresh()
    override c.OnPaint(e:PaintEventArgs) =
        //System.Diagnostics.Debug.WriteLine("OnPaint")
        base.OnPaint(e)
        let g = e.Graphics
        g.DrawLine(Pens.Blue, 0, 0, c.Width, c.Height)
        let rect = new Rectangle(100,100,200,200)
        g.DrawEllipse(Pens.Black, rect)
        g.DrawRectangle(Pens.Red, rect)
    
    override c.OnMouseClick(e:MouseEventArgs) =
        System.Diagnostics.Debug.WriteLine(sprintf "btn=%A, clicks=%A" e.Button e.Clicks)


let canvas = new Canvas(Dock=DockStyle.Fill)
form.Controls.Add(canvas)

[<STAThread>]
Application.Run(form)