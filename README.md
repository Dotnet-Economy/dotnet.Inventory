# dotnet.Inventory

Dotnet Economy Inventory microservice

## Create and publish package

```powershell
$version="1.0.2"
$owner="Dotnet-Economy"
$gh_pat="[PAT HERE]"

dotnet pack src/dotnet.Inventory.Contracts/ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/dotnet.Inventory -o ../packages

dotnet nuget push ../packages/dotnet.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```
