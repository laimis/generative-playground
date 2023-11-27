namespace generative_playground

module Handlers =
    
    open Giraffe

    let mutable private inMemoryModels : BardClient.Models option = None

    type private Input =
        {
            Question : string
            Temperature : float option
            UseOpenAI : bool
        } 

    let getModels () =
        task {
            let! models =
                match inMemoryModels with
                | Some models -> System.Threading.Tasks.Task.FromResult(models)
                | None        -> BardClient.getModels()

            return models
        }

    let getHandler : HttpHandler =
        fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) -> task {

            let! models = getModels()

            inMemoryModels <- Some models

            let view = Views.questionView models (History.getHistory()) ""
            let layout = Views.layout view
            return! (layout |> htmlView) next ctx
        }

    let private getInput (ctx: Microsoft.AspNetCore.Http.HttpContext) =
        let question =
            ctx.GetFormValue("question")

        let useOpenAIOption =
            ctx.GetFormValue("useOpenAI")
            |> Option.map (fun value -> value = "on")

        let temperatureOption = ctx.GetFormValue("temperature")

        let questionText =
            match question with
            | Some text -> text
            | None      -> ""

        let useOpenAI = 
            match useOpenAIOption with
            | Some value -> value
            | None       -> false

        let temparture =
            match temperatureOption with
            | Some value -> 
                match value with
                | "" -> None
                | _  -> Some (float value)
            | None       -> None

        { Question = questionText; Temperature = temparture; UseOpenAI = useOpenAI}

    let private appendToHistory input (bardResponse) =
        match bardResponse with
        | BardClient.BardResponse response ->
            let candidate = response.firstCandidate
            match candidate with
            | Some c ->
                History.addEntry { Question = input.Question; Answer = c.output; Temperature = input.Temperature }
            | None -> ()
        | BardClient.BardError _ -> ()

    let generateHandler : HttpHandler =
        fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
            task {
                let input = getInput ctx

                let! bardModels = getModels() 

                let! bardResponse = BardClient.generateResponse (input.Question) (input.Temperature)

                 // openai too close, exclude until we get that option questionText
                let! openAiResponse =
                    match input.UseOpenAI with
                    | false -> System.Threading.Tasks.Task.FromResult(new OpenAI.Chat.ChatResponse())
                    | true -> OpenAIClient.generateResponse (input.Question)

                bardResponse |> appendToHistory input
                    
                let view = Views.render (input.Question) bardModels bardResponse openAiResponse
                return! (view |> htmlView) next ctx
            }
            
    let imageHandler : HttpHandler =
        fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
            task {
                let prompt = ctx.GetFormValue("prompt")
                
                let! view =
                    match prompt with
                    | Some value -> task {
                            let! images = OpenAIClient.generateImage value
                            return Views.renderImages value images   
                        }
                    | None       -> task {
                            return Views.error "No prompt provided"
                        }
                
                return! (view |> htmlView) next ctx
            }

    let loadHistoryEntryHandler : HttpHandler =
        fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
            task {
                let id = ctx.GetFormValue("index")
                
                // parse index value from id
                let index =
                    match id with
                    | Some value -> int value
                    | None       -> 0
                
                let entry = History.getEntry index
                match entry with
                | Some e ->
                    let view = Views.renderHistoryEntry e
                    return! (view |> htmlView) next ctx
                | None ->
                    let layout = Views.error "History entry not found"
                    return! (layout |> htmlView) next ctx
            }