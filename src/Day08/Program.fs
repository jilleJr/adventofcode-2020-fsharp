open System.IO
open System.Runtime.CompilerServices

let readLines filename =
    File.ReadAllLines filename

type OpCode =
    | Nop
    | Acc
    | Jmp

let splitOp (line: string) =
    // Using String.item[Range] is more forgiving than String.Substring
    // as it returns empty string when indices are out of bounds
    line.[0..2], line.[4..]

let parseOp line =
    match splitOp line with
    | "nop", nop -> Nop, int nop
    | "acc", acc -> Acc, int acc
    | "jmp", jmp -> Jmp, int jmp
    | _ -> failwithf "Unable to parse op: '%s'" line

[<IsReadOnly; Struct>]
type ProgramState(pc: int, acc: int) =
    member _.Pc = pc
    member _.Acc = acc
    static member Empty = ProgramState(0,0)
    override this.ToString() = sprintf "{ Pc = %i; Acc = %i }" this.Pc this.Acc

let applyOp (state: ProgramState) op =
    match op with
    | Nop, _ -> ProgramState(state.Pc + 1, state.Acc)
    | Acc, acc -> ProgramState(state.Pc + 1, state.Acc + acc)
    | Jmp, jmp -> ProgramState(state.Pc + jmp, state.Acc)

let pair v = (v, v)

let walkProgram ops =
    Seq.unfold (fun (state: ProgramState) ->
        Array.tryItem state.Pc ops
        |> Option.map (applyOp state >> pair)
    ) ProgramState.Empty

module Seq =
    let foldWhileSome folder state source =
        source
        |> Seq.scan (fun prevState item ->
            folder (Option.get prevState) item
        ) (Some state)
        |> Seq.takeWhile Option.isSome
        |> Seq.map Option.get
        |> Seq.last

type RunState =
    | NotStarted
    | Running
    | InfiniteLoopDetected
    | RanToCompletetion

let stopWhenLoopDetected states =
    let _, lastState, stoppedReason =
        states
        |> Seq.foldWhileSome (fun (pcs, _, lastRunState) (state: ProgramState) ->
            match lastRunState with
            | NotStarted
            | Running ->
                let newRunState = if Set.contains state.Pc pcs then InfiniteLoopDetected else Running
                Some (Set.add state.Pc pcs, state, newRunState)
            | _ -> None
        ) (Set.empty, ProgramState(), NotStarted)
    match stoppedReason with
    | Running -> lastState, RanToCompletetion
    | _ -> lastState, stoppedReason

let withOneOpChanged replaceOp withOp (ops: (OpCode * int)[]) =
    ops
    |> Seq.indexed
    |> Seq.filter (fun (_, (op, _)) -> op = replaceOp)
    |> Seq.map (fun (opIndex, (_, n)) ->
        opIndex, Seq.init ops.Length (fun i ->
            if i = opIndex then
                withOp, n
            else
                ops.[i]
        )
    )

let runAndPrintWithOneOpChanged replaceOp withOp ops =
    let programs = ops |> withOneOpChanged replaceOp withOp |> Array.ofSeq
    let completions =
        programs
        |> Array.map (fun (i, opsSeq) ->
            let finalState, stoppedReason = opsSeq |> Array.ofSeq |> walkProgram |> stopWhenLoopDetected
            (i, finalState, stoppedReason)
        )
        |> Seq.filter (fun (_, _, stoppedReason) -> stoppedReason = RanToCompletetion)
        |> Array.ofSeq
    printfn " With %A->%A, %i/%i attempts succeeded." replaceOp withOp completions.Length programs.Length
    completions
    |> Array.iter (fun (i, finalState, _) ->
        printfn "  ops.[%i] <- %A succeeded with ending state: %A" i withOp finalState
    )

[<EntryPoint>]
let main args =
    let filename = Array.tryItem 0 args |> Option.defaultValue "./input.txt"
    let lines = readLines filename
    printfn "Read %i lines from %s" lines.Length filename

    let ops = Array.Parallel.map parseOp lines
    printfn "Parsed %i ops: %A" ops.Length ops

    printfn ""
    printfn "Part 1:"
    let statesSeq = walkProgram ops
    let finalState, stoppedReason = stopWhenLoopDetected statesSeq
    printfn " Last state: %A" finalState
    printfn " Stopped reason: %A" stoppedReason

    printfn ""
    printfn "Part 2:"
    runAndPrintWithOneOpChanged Nop Jmp ops
    runAndPrintWithOneOpChanged Jmp Nop ops

    0
