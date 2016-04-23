module Colors

open System.Drawing

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

