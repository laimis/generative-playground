namespace generative_playground

module History =

    type Entry =
        {
            Question : string
            Answer : string
            Temperature : float option
        }

    let mutable private history : Entry list = []

    let addEntry (entry:Entry) =
        history <- entry :: history

    let getHistory () =
        history