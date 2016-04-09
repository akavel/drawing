// Based on https://www.youtube.com/watch?v=3PlRyug05hM

open System
open System.Windows.Forms

let form = new Form()
form.Visible <- true
form.Text <- "F# forms"

let label = new Label()
label.Text <- "a label"

form.Controls.Add(label)

[<STAThread>]
Application.Run(form)