
dotnet clean "src/ImminentCrash.sln"
#dotnet restore "src/ImminentCrash.sln"
#dotnet build "src/ImminentCrash.sln" --no-restore -c "Release"

$FolderName = "Published"
if (Test-Path $FolderName) {
 
    Remove-Item $FolderName -Force -Recurse
}
New-Item -Path $FolderName -Type Directory

dotnet publish "src/ImminentCrash.Client/ImminentCrash.Client.csproj" -o "Published/Client" -c "Release"
dotnet publish "src/ImminentCrash.Server/ImminentCrash.Server.csproj" -o "Published/Server" -c "Release"

Remove-Item "Published/Server/appsettings.Development.json"
Remove-Item "Published/Client/wwwroot/appsettings.Development*"