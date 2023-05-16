## What's this?

This repository houses a web app that uses Google's MakerSuite APIs (Google Bard) and also OpenAPI APIs for interacting with generative text models. Right now only MakerSuite is added as I just got access, but OpenAPI should come shortly.

Very crude, single page to ask questions and get back generated responses.

## How to run it

Pre-requesite: have donet framework installed: https://dotnet.microsoft.com/en-us/

Steps:

1. Get MakerSuite key
2. Run `dotnet user-secrets init`
3. Run `dotnet user-secrets set bardkey "<KEYVALUE>"
4. Run `dotnet run`