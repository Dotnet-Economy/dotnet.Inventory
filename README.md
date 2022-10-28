# dotnet.Inventory

Dotnet Economy Inventory microservice

## Create and publish package

```powershell
$version="1.0.4"
$owner="Dotnet-Economy"
$gh_pat="[PAT HERE]"

dotnet pack src/dotnet.Inventory.Contracts/ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/dotnet.Inventory -o ../packages

dotnet nuget push ../packages/dotnet.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

## Build the docker image

```powershell
$env:GH_OWNER="Dotnet-Economy"
$env:GH_PAT="[PAT HERE]"
$appname="dotneteconomy"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$appname.azurecr.io/dotnet.inventory:$version" .
```

## Run the docker image

```powershell
$cosmosDbConnString="[CONN STRING HERE]"
$serviceBusConnString="[CONN STRING HERE]"
docker run -it --rm -p 5002:5002 --name inventory -e MongoDbSettings__ConnectionString=$cosmosDbConnString -e ServiceBusSettings__ConnectionString=$serviceBusConnString -e ServiceSettings__MessageBroker="SERVICEBUS" dotnet.inventory:$version
```

## Publishing the docker image

```powershell
az acr login --name $appname
docker push "$appname.azurecr.io/dotnet.inventory:$version"
```

## Creating the pod managed identity

```powershell
$namespace="inventory"

az identity create -g $appname -n $namespace
$IDENTITY_RESOURCE_ID=az identity show -g $appname -n $namespace --query id -otsv

az aks pod-identity add -g $appname --cluster-name $appname --namespace $namespace -n $namespace --identity-resource-id $IDENTITY_RESOURCE_ID
```

## Granting acess to Key Vault secrets

```powershell
$IDENTITY_CLIENT_ID=az identity show -g $appname -n $namespace --query clientId -otsv
az keyvault set-policy -n $appname --secret-permissions get list --spn $IDENTITY_CLIENT_ID
```
