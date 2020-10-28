[<AutoOpen>]
module DataGridAnnotationAdorner

open System
open System.Collections
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Media

type DataGridAnnotationAdorner (adornedDataGrid: Grid, control: Control) =
  inherit Adorner(adornedDataGrid)

  do
    base.AddLogicalChild control
    base.AddVisualChild control

    
  override _.MeasureOverride (s: Size) =
    control.Measure s
    control.DesiredSize


  override _.ArrangeOverride (finalSize: Size) =
    let x = (adornedDataGrid.ActualWidth - control.DesiredSize.Width) / 2.0
    let y = (adornedDataGrid.ActualHeight - control.DesiredSize.Height) / 2.0
    let p = Point (x, y)
    let r = Rect (p, finalSize)

    control.Arrange r

    finalSize


  override _.OnRender (drawingContext: DrawingContext) =
    let screenBrush = SolidColorBrush () 
    screenBrush.Color <- Colors.Crimson
    screenBrush.Opacity <- 0.3
    screenBrush.Freeze ()

    let rectangle = Rect (Point (0.0, 0.0), adornedDataGrid.DesiredSize)

    drawingContext.DrawRectangle (screenBrush, null, rectangle)

    base.OnRender drawingContext


  override _.VisualChildrenCount
    with get () = 1

  override _.GetVisualChild (index: int) =
    if (index <> 0) then
      raise (ArgumentOutOfRangeException ("index"))

    control :> Visual

  override _.LogicalChildren
    with get () = (seq { control }).GetEnumerator() :> IEnumerator