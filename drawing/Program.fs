open System
open System.Windows.Forms

// TODO: in future, make fullscreen: FormBorderStyle=FormBorderStyle.None; but find out a way to revert this
let form = new Form(Visible=true, Text="Drawing App", WindowState=FormWindowState.Maximized)
let label = new Label(Text="sample label")
form.Controls.Add(label)

[<STAThread>]
Application.Run(form)