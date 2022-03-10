# Notification service
Service to manage notifications.

![.NET build and test](https://github.com/darianferrer/NotificationService/actions/workflows/dotnet.yml/badge.svg)

## How to run
First thing is to clone the repository. After that's completed, you can use .NET commands to run the service

#### .NET
* Download and install [.NET 6 SDK](https://dotnet.microsoft.com/download)
* From `NotificationService` run `dotnet run`
* Open a browser and navigate to https://localhost:5000/swagger to start using the API

## Integration Tests
To run the tests:
* Make sure that .NET 6 SDK is installed
* [Docker](https://www.docker.com/get-started) is required for the integration tests
* Run `dotnet test` from the solution or the test project folder 
