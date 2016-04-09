// Based on https://www.youtube.com/watch?v=3PlRyug05hM

open System
open System.Windows.Forms

let form = new Form(Visible=true, Text="Drawing App")
let label = new Label(Text="sample label")
form.Controls.Add(label)

[<STAThread>]
Application.Run(form)