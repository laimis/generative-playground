namespace generative_playground

module BardClient =
    open System.Net.Http

    type Prompt =
        {
            text : string
        }

    type GenerateTextRequest = 
        {
            prompt : Prompt
            temperature : float
            candidate_count: int
        }

        static member create (question:string) =
            {
                prompt = { text = question }
                temperature = 0.8
                candidate_count = 5
            }

    type SafetyRating =
        {
            category : string
            probability : string
        }

    type CitationSource =
        {
            startIndex : int
            endIndex : int
            uri : string
            license : string
        }

    type CitationMetadata =
        {
            citationSources : List<CitationSource>
        }

    type Candidate =
        {
            output : string
            safetyRatings : List<SafetyRating>
            citationMetadata : Option<CitationMetadata>
        }

        member this.citationSources =
            match this.citationMetadata with
            | Some metadata -> metadata.citationSources
            | None -> []

    type Filter =
        {
            reason : string
        }

    type SafetyFeedbackRating =
        {
            category : string
            probability : string
        }

    type SafetyFeedbackSetting =
        {
            category : string
            threshold : string
        }

    type SafetyFeedback =
        {
            rating : SafetyFeedbackRating
            setting : SafetyFeedbackSetting
        }

    type Candidates = 
        {
            filters: List<Filter> option
            safetyFeedback: List<SafetyFeedback>
            candidates : List<Candidate>
        }

    type BardErrorDetails =
        {
            code : int
            message : string
            status : string
        }

    type BardError =
        {
            error : BardErrorDetails
        }

    type BardResponse =
        | BardResponse of Candidates
        | BardError of BardError

    let private client = new HttpClient()

    let mutable private key = ""
    let init (apiKey:string) =
        match apiKey with
        | x when System.String.IsNullOrWhiteSpace(x) -> 
            System.Console.WriteLine("Please set the API key in BardClient.fs")
            raise (System.Exception("API key not set"))
        | _ ->
            key <- apiKey

    let generateTextEndpoint = $"https://generativelanguage.googleapis.com/v1beta2/models/text-bison-001:generateText"

    let generateResponse (prompt:string) =
        task {
            if (prompt = "") then
                let candidates = []
                return BardResponse({ candidates = candidates; filters = None; safetyFeedback = [] })
            else
                let request = GenerateTextRequest.create prompt
                let json = System.Text.Json.JsonSerializer.Serialize(request)
                let content: StringContent = new StringContent(json)
                let url = generateTextEndpoint + "?key=" + key
                let! response = client.PostAsync(url, content)
                let! responseText = response.Content.ReadAsStringAsync()
                System.Console.WriteLine(responseText)

                match response.IsSuccessStatusCode with
                | false -> 
                    let error = System.Text.Json.JsonSerializer.Deserialize<BardError>(responseText)
                    return BardError(error)
                | true ->
                    let candidates = System.Text.Json.JsonSerializer.Deserialize<Candidates>(responseText)
                    return BardResponse(candidates)
        }