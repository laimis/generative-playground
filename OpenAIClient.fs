namespace generative_playground

module OpenAIClient =
    open OpenAI.Chat

    let private model = OpenAI.Models.Model.GPT4
    let mutable private api:OpenAI.OpenAIClient = null

    let init apiKey =
        match apiKey with
        | x when System.String.IsNullOrWhiteSpace(x) ->
            System.Console.WriteLine("Please set the OpenAI API key")
            raise (System.Exception("API key not set"))
        | _ -> 
            api <- new OpenAI.OpenAIClient(new OpenAI.OpenAIAuthentication(apiKey))

    let generateResponse prompt =
        task {
            let message = Message(Role.User, prompt)
            let prompts = new ChatRequest([message], model)
            return! api.ChatEndpoint.GetCompletionAsync(prompts)
        }