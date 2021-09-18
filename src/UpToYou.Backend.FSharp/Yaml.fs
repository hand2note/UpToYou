namespace UpToYou.Backend.FSharp

open System

[<StructuredFormatDisplay("{StructuredFormatDisplay}")>]
type Yaml = 
    | YString of string
    | YNumber of float
    | YBool of bool
    | YList of Yaml list
    | YObject of Map<string option, Yaml>
    member private t.StructuredFormatDisplay =
        match t with
        | YString s -> ("\"" + s + "\"")
        | YNumber f -> f.ToString()
        | YBool   b -> b.ToString()
        | YList   l -> l.ToString()
        | YObject m -> (Map.toList m :> obj).ToString()

type YamlParsingException (msg:string) =
    inherit Exception(msg)

module internal ParseYaml = 
    open FParsec
    open FParsecPrettyDebug
    
    let inline parsingException msg = raise (YamlParsingException (msg))

    [<StructuredFormatDisplay("{StructuredFormatDisplay}")>]
    type internal YamlParseContext = 
        { mutable Debug: DebugInfo } 
        interface IFParsecPrettyDebugableContext with
            member this.Debug with get() = this.Debug and set(value) = this.Debug <- value
        static member Default = { Debug = {Message = null; Indent = 0};  }

    let (yvalue: Parser<Yaml, _>), yvalueRef = createParserForwardedToRef()

    let ws = many (pchar ' ')

    let emptyLine = ws >>? newline
    let newEmptyLines = many1 emptyLine <!> "newEmptyLines"
    
    let getWhiteSpaces n = [0..n] |> List.fold (fun s x -> if x = 0 then "" else s + " ") ""
    let spacesExact n = pstring (getWhiteSpaces n) >>. notFollowedByEof

    //YBool, YNumber, YString
    let ytrue = stringReturn "true" (YBool true) 
    let yfalse = stringReturn "false" (YBool false)
    let ybool = ytrue <|> yfalse  
    let ynumber = (pfloat .>>? (followedBy eof <|> followedBy (pchar ' ' <|> newline))) |>> YNumber
    let ystring = notEmpty (restOfLine false) |>> (fun x-> x.Trim())  |>> YString
    let yscalar = choice [ybool; ynumber; ystring] <!> "yscalar"
    
    let getCurrentIndent: Parser<int, _> = fun stream -> Reply(int stream.Position.Column-1)
    let indent x = spacesExact x
    
    //YList
    let hyphen = pchar '-' <!> "hyphen"
    let listItem = hyphen >>. ws >>. yvalue <!> "listItem"
    let ylist = lookAhead hyphen >>. getCurrentIndent >>= (fun x -> sepBy1 listItem (newEmptyLines >>? indent x)) |>> YList <!> "ylist"

    //Key-Value
    let isDelimeter c = c = ':'
    let keyDelimeter = pchar ':'
    let isKeyChar c = c <> ':' && c <> ''' && c <> '\n' && c <> '\r' 

    let someKey = manySatisfy isKeyChar .>>? followedBy keyDelimeter .>> keyDelimeter <!> "someKey"
    
    let key = getCurrentIndent >>= (fun x -> (someKey |>> Some) <|> (((lookAhead (skipRestOfLine false >>. newEmptyLines >>. indent x) |>> (fun x-> Option<string>.None))) <!> "noneKey"))
    let scalarKeyValue = key .>>.? (ws >>. yscalar) <!> "scalarKeyValue"
    let objectKeyValue = getCurrentIndent >>= (fun x -> key .>>.? (ws >>? newEmptyLines >>? (indent (x+1)) >>. ws >>. choice [ yvalue; ylist ])) <!> "objectKeyValue"
    let keyValue=  objectKeyValue  <|> scalarKeyValue <!> "keyValue"
    
    //YObject
    let yobject = getCurrentIndent >>= (fun x -> sepBy1 keyValue (newEmptyLines >>? indent x)) |>> (Map.ofList >> YObject) <!> "yobject"

    let item  = choice [ ylist; yobject ;  yscalar; ] 
    do yvalueRef := item  <!> "yvalue"
    let yaml =  yobject  .>> eof 

    let parseYaml input = runParserOnString yaml YamlParseContext.Default "" input
    let parse parser input = runParserOnString parser YamlParseContext.Default  "" input
    let parseResult parser input  = 
          match parse parser input with 
          | Success (x,_,_) -> x 
          | Failure (error, _, _) ->  parsingException(error)

module YamlParser = 
    open FParsec
    open ParseYaml

    let parseYamlResult input  = match parseYaml input with Success (x,_,_) -> FSharp.Core.Ok x | Failure (error, _, _) ->  FSharp.Core.Error [error]

    let parseYObjectResult input = 
        match parseResult yobject input with
        | YObject obj -> obj 
        | _ -> parsingException ("Expecting a yaml object in the root")

module YamlMapper = 
    open ParseYaml

    let findYString key map = 
        map 
        |> Map.find (Some key) 
        |> function 
            | YString x -> x
            | _ -> parsingException (sprintf "%s should be a scalar string" key )

    let tryFindYString key map = 
        map 
        |> Map.tryFind (Some key) 
        |> function 
            | None -> None
            | Some x -> 
                    match x with  
                    | YString x -> Some x
                    | _ -> parsingException (sprintf "%s should be a scalar string" key)

    let tryFindNoneKey map = 
        map 
        |> Map.tryFind Option<string>.None 
        |> function 
            | None -> Ok None
            | Some x -> match x with 
                        | YString x -> Ok (Some x )
                        | _ -> Error ["Expecting a scalar string"]

    let findNoneKeyL label map = 
        map
        |> Map.tryFind Option<string>.None 
        |> function 
            | None -> parsingException(sprintf "%s key not found" label)
            | Some x -> match x with 
                        | YString x -> x
                        | _ -> parsingException (sprintf "%s should be a scalar string" label)

    let findOptionalYString key map = 
        map 
        |> tryFindYString key 
        |> function Some x -> x | None -> findNoneKeyL key map

    
    let tryFindYList key map = 
        map 
        |> Map.tryFind (Some key)
        |> function 
            | None -> None
            | Some x -> 
                match x with 
                | YList list -> Some list
                | _ -> parsingException ( sprintf "%s should be a list" key)

    let tryFindListOfYObjects key map =    
        map 
        |> tryFindYList key
        |> function
           | None -> None
           | Some ylist -> 
             ylist |> List.map (function YObject yobject -> yobject | _ -> parsingException (sprintf "Expecting an object %s list item" key)) |> Some

    let findListOfYObjects key map =
        map |> tryFindListOfYObjects key |> function Some x -> x | None -> parsingException (sprintf "List %s not found" key)

    let findYList key map : Yaml list = tryFindYList key map |> someOrThrow (YamlParsingException(sprintf "%s not found" key)) 

    let tryFindListOfYString key map = 
        map 
        |> tryFindYList key
        |> function
           | None -> None
           | Some ylist -> 
                ylist |> List.map (function YString x -> x | _ -> (sprintf "Expecting a string %s list item" key)) |> Some

    let findListOfYString key map = 
        map |> tryFindListOfYString key |> function Some x -> x | None -> parsingException (sprintf "List %s not found" key)


    let tryFindYBool key map = 
           map 
           |> Map.tryFind (Some key )
           |> noneOr (function YBool b -> Some b | _ -> parsingException (sprintf "%s should be a bool" key))

    let tryFindYNumber key map = 
        map |> Map.tryFind (Some key) |> noneOr (function YNumber x -> Some x | _ -> parsingException (sprintf "%s should be a number" key))

    let tryFindYObject key map = 
        map 
        |> Map.tryFind (Some key) |> noneOr (function YObject x -> Some x | _ -> parsingException (sprintf "%s should be an object" key))
               
        