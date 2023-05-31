namespace generative_playground

module Views =
    open Giraffe.ViewEngine
    open OpenAI.Chat

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "LLMs" ]
                
                meta [
                    _name "viewport"
                    _content "width=device-width, initial-scale=1"
                ]
                
                link [
                    _rel "stylesheet"
                    _type "text/css"
                    _href "/node_modules/bulma/css/bulma.min.css"]
            ]
            body [] [
                div [ _class "container"] content
            ]
        ]

    let questionView (text:string) (bardModels:BardClient.Models) =
        let formElement =
            form [ 
                _action "/"
                _method "POST"
                ] [
                div [] [
                    div [_class "field"] [
                        textarea [
                            _id "question"
                            _name "question"
                            _class "textarea"
                            ] [
                                rawText text
                            ]
                    ]
                    div [_class "field"] [
                        label [
                            _class "checkbox"
                        ] [
                            input [
                                _type "checkbox"
                                _name "useOpenAI"
                            ]
                            rawText "Use OpenAI"
                        ]
                    ]
                    div [_class "field"] [
                        label [
                            _for "temperature"
                            _class "label"
                        ] [
                            encodedText "Temperature (from 0 to 1, optional)"
                        ]
                        input [
                            _id "temperature"
                            _name "temperature"
                            _class "input"
                            _type "number"
                            _value ""
                        ]
                    ]
                    div [_class "field"] [
                        label [] [
                            encodedText "Model"
                        ]
                        div [] (bardModels.models |> List.map (fun model -> div [] [ model.name |> encodedText ]))
                    ]
                    div [_class "field"] [
                        button [
                            _class "button is-primary"
                            _type "submit"
                        ] [ encodedText "Generate" ]
                    ]
                ]
            ]
        
        [
            div [_class "content"] [
                h1 [] [ encodedText "Enter Prompt" ]
                formElement
            ]
        ]

    let bardResponseView (response:BardClient.BardResponse) =
        let safetyRatingToHtml (safetyRating:BardClient.SafetyRating) =
            match safetyRating.probability with
            | "NEGLIGIBLE" -> None
            | probability ->     
                let elem = span [ _style $"background-color: orange"] [
                    encodedText safetyRating.category
                    encodedText probability
                ]
                Some elem

        let citationToHtml (citation:BardClient.CitationSource) =
            li [] [
                a [ _href citation.uri ] [
                    encodedText citation.uri
                ]
            ]

        let candidateToHtml (candidate:BardClient.Candidate) =

            let safetyRatingElements =
                div [ _style "font-size: small"] (candidate.safetyRatings |> List.map safetyRatingToHtml |> List.filter Option.isSome |> List.map Option.get)

            let citationElements =
                div [] (candidate.citationSources |> List.map citationToHtml)

            let outputDiv =
                div [] [
                    Markdig.Markdown.ToHtml(candidate.output) |> rawText 
                ]

            div [_class "box"] [
                outputDiv
                safetyRatingElements
                citationElements
            ]

        match response with
        | BardClient.BardResponse(candidates) ->
            match candidates.filters with
            | None -> 
                match candidates.candidates with
                | [] -> []
                | candidates ->
                    [
                        div [_class "content"] [
                            h1 [] [ encodedText $"Bard ({candidates.Length})" ]
                            div [] (candidates |> List.distinctBy (fun c -> c.output) |> List.map candidateToHtml)
                        ]
                    ]
            | Some filters ->

                let safetyFeedbackElements (safetyFeedback:BardClient.SafetyFeedback) =
                    div [] [
                        div [] [encodedText $"rating category: {safetyFeedback.rating.category}"]
                        div [] [encodedText $"rating probability: {safetyFeedback.rating.probability}"]
                        div [] [encodedText $"setting category: {safetyFeedback.setting.category}"]
                        div [] [encodedText $"setting threshold: {safetyFeedback.setting.threshold}"]
                    ]

                let safetyFeedback = 
                    div [] [
                        div [] [
                            encodedText "Safety Feedback"
                        ]
                        div [] (candidates.safetyFeedback |> List.map safetyFeedbackElements)
                    ]

                [
                    div [_class "content"] [
                        h1 [] [ encodedText $"Bard filter kicked in" ]
                        div [] [
                            div [] [
                                encodedText "Filtered"
                            ]
                            div [] (filters |> List.map (fun f -> encodedText f.reason))
                        ]
                        safetyFeedback
                    ]
                ]
        | BardClient.BardError(bardError) ->
            [
                let code = bardError.error.code
                let message = bardError.error.message
                let status = bardError.error.status

                div [_class "content"] [
                    h1 [] [ encodedText $"Bard Error ({code})" ]
                    div [] [
                        div [] [encodedText $"Message: {message}"]
                        div [] [encodedText $"Status: {status}"]
                    ]
                ]
            ]

    let openAiResponseView (response:ChatResponse) =
        let choiceToHtml (choice:Choice) =

            let finishReason =
                div [ _class "is-size-6"] [$"finish reason: {choice.FinishReason}" |> encodedText]

            let outputDiv =
                div [] [
                    Markdig.Markdown.ToHtml(choice.Message) |> rawText 
                ]

            div [_class "box"] [
                outputDiv
                finishReason
            ]

        match response.Choices with
        | null -> []
        | _ ->
            [
                div [_class "content"] [
                    h1 [] [ encodedText $"OpenAI ({response.Choices.Count})" ]
                    div [] (response.Choices |> List.ofSeq |> List.map choiceToHtml)
                    div [ _class "is-size-6"] [
                        encodedText $"Processing time: {response.ProcessingTime}ms"
                    ]
                ]
            ]

    let render
        questionText
        bardModels
        (bardResponse:BardClient.BardResponse)
        (openAiResponse:ChatResponse) =
        let questionElements = bardModels |> questionView questionText
        let bardResponseElements = bardResponse |> bardResponseView
        let openAiResponseElements = openAiResponse |> openAiResponseView

        let columns = div [ _class "columns mt-10" ] [
            div [ _class "column is-one-quarter" ] questionElements
            div [ _class "column" ] bardResponseElements
            div [ _class "column" ] openAiResponseElements
         ]

        [columns] |> layout