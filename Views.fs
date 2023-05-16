namespace generative_playground

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "generative_playground" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let form (text:string) =
        [
            h1 [] [ encodedText "Ask your question below:" ]
            form [ 
                _action "/"
                _method "POST"
                ] [
                div [] [
                textarea [
                    _id "question"
                    _name "question"
                    _rows "10";
                    _cols "80"]
                    [
                        rawText text
                    ]
                ]
                button [ _type "submit" ] [ encodedText "Ask" ]
            ]
        ]

    let responseView (response:BardClient.Candidates) =
        let safetyRatingToHtml (safetyRating:BardClient.SafetyRating) =
            match safetyRating.probability with
            | "NEGLIGIBLE" -> None
            | probability ->     
                let elem = span [ _style $"background-color: orange"] [
                    encodedText safetyRating.category
                    encodedText probability
                ]
                Some elem

        let candidateToHtml (candidate:BardClient.Candidate) =

            let safetyRatingElements =
                div [ _style "font-size: small"] (candidate.safetyRatings |> List.map safetyRatingToHtml |> List.filter Option.isSome |> List.map Option.get)

            let outputDiv =
                div [
                    _style "border: 2px solid gray; padding: 10px;"
                ] [
                    Markdig.Markdown.ToHtml(candidate.output) |> rawText 
                ]

            div [
                _style "margin-bottom: 20px;"
            ] [
                outputDiv
                safetyRatingElements
            ]
        
        match response.candidates with
        | [] -> []
        | _ ->
            [
                h1 [] [ encodedText $"Answers ({response.candidates.Length})" ]
                div [] (response.candidates |> List.map candidateToHtml)
            ]

    let render questionText candidates =
        let formElements = questionText |> form

        let responseElements = candidates |> responseView

        let elements = List.concat [formElements; responseElements]

        elements |> layout