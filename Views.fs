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

    let questionView (text:string) =
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
                        button [
                            _class "button is-primary"
                            _type "submit"
                        ] [ encodedText "Ask" ]
                    ]
                ]
            ]
        
        [
            div [_class "content"] [
                h1 [] [ encodedText "Ask a Question" ]
                formElement
            ]
        ]

    let bardResponseView (response:BardClient.Candidates) =
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
        
        match response.candidates with
        | [] -> []
        | _ ->
            [
                div [_class "content"] [
                    h1 [] [ encodedText $"Bard ({response.candidates.Length})" ]
                    div [] (response.candidates |> List.map candidateToHtml)
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

        [
            div [_class "content"] [
                h1 [] [ encodedText $"OpenAI ({response.Choices.Count})" ]
                div [] (response.Choices |> List.ofSeq |> List.map choiceToHtml)
                div [ _class "is-size-6"] [
                    encodedText $"Processing time: {response.ProcessingTime}ms"
                ]
            ]
        ]

    let render questionText bardResponse (openAiResponse:ChatResponse) =
        let questionElements = questionText |> questionView
        let bardResponseElements = bardResponse |> bardResponseView
        let openAiResponseElements = openAiResponse |> openAiResponseView

        let columns = div [ _class "columns mt-10" ] [
            div [ _class "column is-one-quarter" ] questionElements
            div [ _class "column" ] bardResponseElements
            div [ _class "column" ] openAiResponseElements
         ]

        [columns] |> layout