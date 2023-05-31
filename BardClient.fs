namespace generative_playground

module BardClient =
    open System.Net.Http

    // {
    //   "name": "models/chat-bison-001",
    //   "version": "001",
    //   "displayName": "Chat Bison",
    //   "description": "Chat-optimized generative language model.",
    //   "inputTokenLimit": 4096,
    //   "outputTokenLimit": 1024,
    //   "supportedGenerationMethods": [
    //     "generateMessage"
    //   ],
    //   "temperature": 0.25,
    //   "topP": 0.95,
    //   "topK": 40
    // },

    type Model =
        {
            name : string
            version : string
            displayName : string
            description : string
            inputTokenLimit : int
            outputTokenLimit : int
            supportedGenerationMethods : List<string>
            temperature : float option
            topP : float option
            topK : int option
        }

    type Models =
        {
            models : List<Model>
        }

    type Prompt =
        {
            text : string
        }

    type GenerateTextRequest = 
        {
            prompt : Prompt
            temperature : float option
            candidate_count: int
        }

        static member create (question:string) temperature =
            {
                prompt = { text = question }
                temperature = temperature
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

    type private HttpResponse = (HttpResponseMessage * string)

    let private client = new HttpClient()

    let mutable private key = ""
    let private baseUrl = "https://generativelanguage.googleapis.com/v1beta2"

    let init (apiKey:string) =
        match apiKey with
        | x when System.String.IsNullOrWhiteSpace(x) -> 
            System.Console.WriteLine("Please set the API key in BardClient.fs")
            raise (System.Exception("API key not set"))
        | _ ->
            key <- apiKey

    let toUrl endpoint =
        $"{baseUrl}{endpoint}?key={key}"

    let private post endpoint data =
        task {
            let json = System.Text.Json.JsonSerializer.Serialize(data)
            let content: StringContent = new StringContent(json)
            let url = endpoint |> toUrl
            let! response = client.PostAsync(url, content)
            let! responseText = response.Content.ReadAsStringAsync()
            System.Console.WriteLine(responseText)
            return HttpResponse(response, responseText)
        }

    let private get endpoint =
        task {
            let url = endpoint |> toUrl
            let! response = client.GetAsync(url)
            let! responseText = response.Content.ReadAsStringAsync()
            System.Console.WriteLine(responseText)
            return HttpResponse(response, responseText)
        }

    let getModels() =
        task {
            let! (_, responseText) = get "/models"
            return System.Text.Json.JsonSerializer.Deserialize<Models>(responseText)
        }

    let generateResponse (prompt:string) temperature =
        task {
            let request = GenerateTextRequest.create prompt temperature

            let! (response, responseText) = post "/models/text-bison-001:generateText" request

            match response.IsSuccessStatusCode with
            | false -> 
                let error = System.Text.Json.JsonSerializer.Deserialize<BardError>(responseText)
                return BardError(error)
            | true ->
                let candidates = System.Text.Json.JsonSerializer.Deserialize<Candidates>(responseText)
                return BardResponse(candidates)
        }