namespace generative_playground

module Handlers =
    
    open Giraffe

    let handler : HttpHandler =
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

                let! bardResponse = BardClient.generateResponse questionText temparture

                 // openai too close, exclude until we get that option questionText
                let! openAiResponse =
                    match useOpenAI with
                    | false -> OpenAIClient.generateResponse ""
                    | true -> OpenAIClient.generateResponse questionText
                    
                let view = Views.render questionText bardResponse openAiResponse
                return! (view |> htmlView) next ctx
            }