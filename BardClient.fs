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

    type Candidates = 
        {
            candidates : List<Candidate>
        }

    let client = new HttpClient()

    let mutable key = ""
    let init (apiKey:string) =
        if (System.String.IsNullOrEmpty(apiKey)) then
            System.Console.WriteLine("Please set the API key in BardClient.fs")
            raise (System.Exception("API key not set"))
        else
            System.Console.WriteLine("API key set " + apiKey + "...")
            key <- apiKey

    let generateTextEndpoint = $"https://generativelanguage.googleapis.com/v1beta2/models/text-bison-001:generateText"

    let askQuestion (question:string) =
        task {
            if (question = "") then
                return System.Threading.Tasks.Task.FromResult({ candidates = [] })
            else
                let request = GenerateTextRequest.create question
                let json = System.Text.Json.JsonSerializer.Serialize(request)
                let content: StringContent = new StringContent(json)
                let url = generateTextEndpoint + "?key=" + key
                let! response = client.PostAsync(url, content)
                let! responseText = response.Content.ReadAsStringAsync()
                System.Console.WriteLine(responseText)
                return System.Threading.Tasks.Task.FromResult(
                    System.Text.Json.JsonSerializer.Deserialize<Candidates>(responseText)
                )
        }