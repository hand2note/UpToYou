module internal FParsecPrettyDebug

open FParsec
open System

type DebugInfo = { Message: string; Indent: int }
    with static member Default = {Message = null; Indent = 0}
type DebugType<'a> = Enter | Leave of Reply<'a>

type IFParsecPrettyDebugableContext = 
    abstract member Debug: DebugInfo with get, set

let addToDebug (stream:CharStream<'C> when 'C:>IFParsecPrettyDebugableContext) label dtype =
       let msgPadLen = 50
   
       let startIndent = stream.UserState.Debug.Indent
       let (str, curIndent, nextIndent) = 
           match dtype with
           | Enter    -> sprintf "> %s" label, startIndent, startIndent+1
           | Leave res ->
               let str = sprintf "< %s (%A)" label res.Status
               let resStr = sprintf "%s" (str.PadRight(msgPadLen-startIndent-1))
               resStr, startIndent-1, startIndent-1
   
       let indentStr =
           if curIndent = 0 then ""
           else "\u251C".PadRight(curIndent, '\u251C')
   
       let posStr = (sprintf "%A: " stream.Position).PadRight(20)
       let posIdentStr = posStr + indentStr
   
       // The %A for res.Result makes it go onto multiple lines - pad them out correctly
       let replaceStr = "\n" + "".PadRight(posStr.Length) + "".PadRight(curIndent, '\u2502').PadRight(msgPadLen)
       let correctedStr = str.Replace("\n", replaceStr)
   
       let fullStr = sprintf "%s%s\n" posIdentStr correctedStr
#if DEBUG
//       Console.Write(fullStr)
#endif
       stream.UserState.Debug <- {
           Message = stream.UserState.Debug.Message + fullStr
           Indent = nextIndent
       }

let (<!>) (p: Parser<_, 'C> when 'C:> IFParsecPrettyDebugableContext) label :Parser<_, 'C>  =
    fun stream ->
        addToDebug stream label Enter
        let reply = p stream
        addToDebug stream label (Leave reply)
        reply

let (<?!>) (p: Parser<_, 'C> when 'C:> IFParsecPrettyDebugableContext) label :Parser<_, 'C> =
    p <?> label <!> label