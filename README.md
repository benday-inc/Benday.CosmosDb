# Benday.CosmosDb

A collection of classes for implementing the domain model and repository patterns with [Azure Cosmos Db](https://azure.microsoft.com/en-us/products/cosmos-db).
These classes are built using the [Azure Cosmos DB libraries for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/cosmosdb?view=azure-dotnet) and aim to 
simplify using hierarchical partition keys and to make sure that query operations are created to use those partition keys correctly.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
https://www.slidespeaker.ai  
info@benday.com  
YouTube: https://www.youtube.com/@_benday  

## Key features

Key features:
* Interfaces and base classes for implementing the [repository pattern](https://martinfowler.com/eaaCatalog/repository.html) with CosmosDb
* Interfaces and base classes for implementing the [domain model pattern](https://en.wikipedia.org/wiki/Domain_model) with CosmosDb
* Help you to write LINQ queries against CosmosDb without having to worry whether you're using the right partition keys
* Support for configuring repositories for use in ASP.NET Core projects
* Support for [hierarchical partition keys](https://learn.microsoft.com/en-us/azure/cosmos-db/hierarchical-partition-keys)
* Logging of query performance and [request units](https://learn.microsoft.com/en-us/azure/cosmos-db/request-units) 
* Detect and warn when you have cross-partition queries 
* Helper classes and methods for registering types and handling connection configuration

* Got ideas for Azure DevOps utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/Benday.CosmosDb/issues*. *Want to contribute? Submit a pull request.*

* [Source code](https://github.com/benday-inc/Benday.CosmosDb)  
* [Repository API Documentation](api/Benday.CosmosDb.Repositories.html)  
* [Domain Model API Documentation](api/Benday.CosmosDb.DomainModels.html)  
* [NuGet Package](https://www.nuget.org/packages/Benday.CosmosDb/)
