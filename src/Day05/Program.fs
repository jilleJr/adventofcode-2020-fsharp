open System.IO

type Line = int * int

type Rect = {
    X: Line
    Y: Line
}

type Direction =
    | Front
    | Back
    | Left
    | Right

let readLines filename =
    File.ReadAllLines filename

let parseLine line =
    line
    |> Seq.map (function
        | 'F' -> Front
        | 'B' -> Back
        | 'L' -> Left
        | 'R' -> Right
        | c -> failwithf "Unknown char '%c'" c
    )
    |> Array.ofSeq

/// Example:
///   * 0 7
///   * 0 3
///   * 0 1
///   * 0 0
let takeFirstHalf min max =
    let range = max - min + 1
    if range <= 1 then
        (min, min)
    else
        (min, max - range / 2)

/// Example:
///   * 0 7
///   * 4 7
///   * 6 7
///   * 7 7
let takeSecondHalf min max =
    let range = max - min + 1
    if range <= 1 then
        (max, max)
    else
        (min + range / 2, max)

let partition rect dir =
    match dir with
    | Front -> { rect with Y = takeFirstHalf <|| rect.Y }
    | Back -> { rect with Y = takeSecondHalf <|| rect.Y }
    | Left -> { rect with X = takeFirstHalf <|| rect.X }
    | Right -> { rect with X = takeSecondHalf <|| rect.X }

let (|SinglePoint|_|) = function
    | { X = (x1,x2); Y = (y1,y2) } when x1=x2 && y1=y2 -> Some (x1, y1)
    | _ -> None

let calculateSeatPos dirs =
    dirs
    |> Array.fold partition { X = (0,7); Y = (0,127) }
    |> function
        | SinglePoint point -> Ok point
        | rect -> Error (sprintf "Dirs did not resolve to a single point: %A" rect)

let calculateSeatId = function
    | x, y -> x + y * 8

let printOccupiedSeats seatIds =
    let setOfSeatIds = Set.ofArray seatIds

    printfn "ROW | PID RANGE   | SEATS"
    printf  "----------------------------"
    seq { 0..(7+127*8) }
    |> Seq.iter (fun seatId ->
        if seatId % 8 = 0 then
            printf "\n%3i | 0x%03x-0x%03x | " (seatId / 8) seatId (seatId + 7)

        let c =
            match Set.contains seatId setOfSeatIds with
            | true -> 'x' | false -> '.'

        printf "%c" c
    )

    printfn ""

let missingFromSet set element =
    not (Set.contains element set)

[<EntryPoint>]
let main args =
    let filename = Array.tryItem 0 args |> Option.defaultValue "./input.txt"
    let lines = readLines filename
    printfn "Read %i lines from %s" lines.Length filename

    let seats =
        lines
        |> Array.Parallel.mapi (fun i line ->
            parseLine line
            |> calculateSeatPos
            |> function
                | Ok v -> v
                | Error err -> failwithf "Failed at %i:%s: %s" i line err
        )

    printfn "Found %i points from all seats definitions" seats.Length
    printfn ""

    let seatIds = seats |> Array.Parallel.map calculateSeatId
    let highestSeatId = Array.max seatIds
    printfn "Part 1:"
    printfn " Highest sead ID: %i" highestSeatId

    let setOfSeatIds = Set.ofArray seatIds
    let lowestSeatId = Array.min seatIds

    printfn ""
    printfn "Part 2:"
    let unoccupied =
        seq { lowestSeatId..highestSeatId }
        |> Seq.filter (missingFromSet setOfSeatIds)
        |> List.ofSeq
    printfn " Unoccupied seats: %i" unoccupied.Length
    printfn " Unoccupied seat IDs: %A" unoccupied

    printfn ""
    printfn "(For visualization, here's all the seats:)"
    printOccupiedSeats seatIds

    0
