﻿namespace DataGridWithAdorner

open System
open System.Windows.Controls
open System.Windows.Input

open Elmish
open Elmish.WPF

open DataGridWithAdorner.View


module Cell =
    type Model =
        { Id: int
          CellName: string }

    let init i j k =
        { Id = i*100 + j*10 + k
          CellName = sprintf "CellName %i  %i  %i" i j k }

    let setName s m = { m with CellName = s }

    let bindings() = [
        "LastName" |> Binding.oneWay(fun (_, c) -> c.CellName)
        "SelectedLabel" |> Binding.oneWay (fun (b, _) -> if b then " - Selected" else "")
     ]


module Column =
    type Model =
        { Id: int
          InnerRows: Cell.Model list
          SelectedInnerRow: int option
          NewLastName: string }

    type Msg =
        | Select of int option
        | SetNewLastName of string
        | SaveLastName
        | NoOp

    let init i j =
        { Id = i*10  + j
          InnerRows = [0 .. 2] |> List.map (Cell.init i j) 
          SelectedInnerRow = None
          NewLastName = "" }

    let isSelectMsg = function
    | Select (Some _) -> true
    | _ -> false

    let deselect m =
      { m with SelectedInnerRow = None }
          
    let update msg m =
        match msg with
        | Select id -> { m with SelectedInnerRow = id }
        | SetNewLastName s -> { m with NewLastName = s }
        | SaveLastName ->
            let rows =
              m.InnerRows
              |> List.map (fun r ->
                  if Some r.Id = m.SelectedInnerRow then
                    Cell.setName m.NewLastName r
                  else
                    r)
            { m with InnerRows = rows }
        | NoOp -> m


    let mutable private closeAdorner = (fun () -> ())

    let handlePreviewMouseLeftButtonUp (obj: obj) (_, c) =
      closeAdorner ()

      let e = (obj :?> MouseButtonEventArgs)
      let listView = e.Source :?> ListView
      let grid = listView.Parent :?> Grid
      let selectedItem = c.InnerRows |> List.filter (fun r -> Some r.Id = c.SelectedInnerRow) |> List.head

      let adornerLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(grid);
      if (adornerLayer = null) then
          failwith "datagrid does not have have an adorner layer";

      let control = DataGridAnnotationControl ()
      control.DataContext <- grid.DataContext
      let adorner = DataGridAnnotationAdorner(grid, control)
      adornerLayer.Add(adorner);
        
      closeAdorner <- (fun () -> adornerLayer.Remove adorner)

      NoOp
        

    let bindings () : Binding<('a * Model), Msg> list = [
        "InnerRows" |> Binding.subModelSeq(
            (fun (_, p) -> p.InnerRows),
            (fun ((_, p), c) -> (p.SelectedInnerRow = Some c.Id, c)),
            (fun (_, c) -> c.Id),
            snd,
            Cell.bindings)

        "SelectedInnerRow" |> Binding.subModelSelectedItem("InnerRows", (fun (_, c) -> c.SelectedInnerRow), (fun cId _ -> Select cId))
        "PreviewMouseLeftButtonUp" |> Binding.cmdParam (handlePreviewMouseLeftButtonUp)

        "NewLastName" |> Binding.twoWay((fun (_, m) -> m.NewLastName), SetNewLastName)
        "SaveLastName" |> Binding.cmd SaveLastName
    ]


module OutterRow =

    type Model =
      { Id: int
        OutterRowName: string
        Columns: Column.Model list 
        SelectedColumn: int option}

    type Msg =
        | Select of int option
        | ColumnMsg of int * Column.Msg
             
    let init i =
        { Id = i
          OutterRowName = sprintf "RowName %i" i
          Columns =  [0 .. 3] |> List.map (Column.init i) 
          SelectedColumn = None }

    let isSelectCellMsg = function
      | ColumnMsg (_, msg) -> msg |> Column.isSelectMsg               
      | _ -> false

    let deselectCells m =
      { m with Columns = m.Columns |> List.map Column.deselect }

    let deselectAll m =
      { m with SelectedColumn = None } |> deselectCells
   
    let update msg m =
      match msg with
      | Select id -> { m with SelectedColumn = id }
      | ColumnMsg (rId, msg) ->
          let columns =
            m.Columns
            |> List.map (fun r -> if r.Id = rId then Column.update msg r else r)
          { m with Columns = columns }


    let bindings () :Binding<(bool * Model), Msg> list = [
        "RowTime" |> Binding.oneWay(fun (b, p) -> p.OutterRowName + (if b then " - Selected" else ""))

        "Columns" |> Binding.subModelSeq(
                        (fun (_, p) -> p.Columns),
                        (fun ((b, p), c) -> (b && p.SelectedColumn = Some c.Id, c)),
                        (fun (_, c) -> c.Id),
                        ColumnMsg,
                        Column.bindings)
    ]

    
module App =

   type Model =
      { OutterRows: OutterRow.Model list
        SelectedOutterRow: int option }

   type Msg =
       | Select of int option
       | RowMsg of int * OutterRow.Msg
       | Reset
       | DeselectAll
       | DeselectCells

   let init () =
     {  OutterRows = [0 .. 2] |> List.map OutterRow.init
        SelectedOutterRow = None }

   let deselectCells m =
     { m with OutterRows = m.OutterRows |> List.map OutterRow.deselectCells }

   let deselectAll m =
     { m with SelectedOutterRow = None
              OutterRows = m.OutterRows |> List.map OutterRow.deselectAll }

   let updateOutterRow rId msg m =
     let rows =
       m.OutterRows
       |> List.map (fun r -> if r.Id = rId then OutterRow.update msg r else r )  // OutterRow.reset msg r
     { m with OutterRows = rows }

   let update msg m =
      match msg with
      | Select rId -> { m with SelectedOutterRow = rId }
      | RowMsg (rId, msg) ->
          m
          |> if OutterRow.isSelectCellMsg msg then deselectCells else id
          |> updateOutterRow rId msg
      | Reset -> init ()
      | DeselectAll -> m |> deselectAll
      | DeselectCells -> m |> deselectCells


   let bindings () : Binding<Model,Msg> list = [
        "Rows" |> Binding.subModelSeq(
            (fun m -> m.OutterRows),
            (fun (m, r) -> (m.SelectedOutterRow = Some r.Id, r)),
            (fun (_, r) -> r.Id),
            RowMsg,
            OutterRow.bindings)

        "SelectedRow" |> Binding.subModelSelectedItem("Rows", (fun m -> m.SelectedOutterRow), Select)
  
        "Reset" |> Binding.cmd (fun m -> Reset)  // Reset is a Msg. The Msg is acted upon by update.
        "DeselectAll" |> Binding.cmd (fun m -> DeselectAll)
        "DeselectCells" |> Binding.cmd (fun m -> DeselectCells)
   ]


    [<EntryPoint; STAThread>]
    let main argv =
      Program.mkSimpleWpf init update bindings
      |> Program.runWindowWithConfig
        { ElmConfig.Default with LogConsole = true }      // LogTrace -- send output to visual studio output window.  LogConsole -- send output to console window.
        (MainWindow())
 