[<AutoOpen>]
module private Common

let someOrThrow ex value = match value with Some x -> x | None -> raise ex
let noneOr f value = match value with None -> None | Some x -> f x