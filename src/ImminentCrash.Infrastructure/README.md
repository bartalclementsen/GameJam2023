# ImminentCrash.Infrastructure

To work with ef in dotnet core (after 2.0) you must install the dotnet-ef tools. Run this command in powershell

````powershell
dotnet tool install --global dotnet-ef
````

ref: https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/dotnet

The connection string for the database ef tools for this project is hardcoded in the **DbContextFactory**.
This is only used while developing. It is strongly recommended to use your own **local** database when working with migrations.

## How to add a migration

1. Open powershell to the project folder of **ImminentCrash.Infrastructure**
2. Run the command **dotnet ef migrations add "AddPersonImage"**

**NB!** Try and have as few migrations as possible