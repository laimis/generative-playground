module generative_playground.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe

// ---------------------------------
// Models
// ---------------------------------

type Question =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

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
            div [] [
                p [] [ encodedText safetyRating.category ]
                p [] [ encodedText safetyRating.probability ]
            ]

        let candidateToHtml (candidate:BardClient.Candidate) =

            let safetyRatingDivs = candidate.safetyRatings |> List.map safetyRatingToHtml
            let outputDiv = Markdig.Markdown.ToHtml(candidate.output) |> rawText 

            div [] (List.concat [[outputDiv]; safetyRatingDivs])
        
        [
            h1 [] [ encodedText "Answer:" ]
            div [] (response.candidates |> List.map candidateToHtml)
        ]

    let handler : HttpHandler =
        fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
            task {
                let question =
                    ctx.GetFormValue("question")

                let questionText =
                    match question with
                    | Some text -> text
                    | None      -> ""
                    
                let formElements = questionText |> form

                let! response = BardClient.askQuestion questionText

                let responseElements = response.Result |> responseView

                let elements = List.concat [formElements; responseElements]        
                return! (elements |> layout |> htmlView) next ctx
            }
            

// ---------------------------------
// Web app
// ---------------------------------

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> Views.handler
            ]

        POST >=>
            choose [
                route "/" >=> Views.handler
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    
    let builder = new ConfigurationBuilder()
    builder.AddUserSecrets<Question>() |> ignore // had to create this type to get it to work
    let config = builder.Build()
    let bardKey = config["bardkey"]
    // read from user's secrets bardkey
    BardClient.init bardKey

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0