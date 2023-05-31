namespace generative_playground

module Handlers =
    
    open Giraffe

    let mutable private inMemoryModels : BardClient.Models option = None

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

            let view = Views.questionView "" models
            let layout = Views.layout view
            return! (layout |> htmlView) next ctx
        }

    let postHandler : HttpHandler =
        fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
            task {
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

                let! bardModels = getModels() 

                let! bardResponse = BardClient.generateResponse questionText temparture

                 // openai too close, exclude until we get that option questionText
                let! openAiResponse =
                    match useOpenAI with
                    | false -> System.Threading.Tasks.Task.FromResult(new OpenAI.Chat.ChatResponse())
                    | true -> OpenAIClient.generateResponse questionText
                    
                let view = Views.render questionText bardModels bardResponse openAiResponse
                return! (view |> htmlView) next ctx
            }