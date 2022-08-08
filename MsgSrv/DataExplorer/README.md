# Data Explorer
Contents about Data Explorer.

## IaC
This IaC builds Azure Data Explorer.

### Variables

#### Define
```Bash
location=westus2
hmsRgName=HmsRG
clusterName="osscjpdevinfra"
databaseName="osscjpdevinfra"
centralUrlPrefix="osscjpdevinfra"
```

#### Check
```Bash
echo $location
echo $hmsRgName
echo $clusterName
echo $databaseName
echo $centralUrlPrefix
```

### Creating
```Bash
az extension add -n kusto

# Create the Azure Data Explorer cluster
# This command takes at least 10 minutes to run
az kusto cluster create \
  --cluster-name $clusterName \
  --resource-group $hmsRgName \
  --location $location  \
  --sku name="Standard_D11_v2"  tier="Standard" \
  --enable-streaming-ingest=true \
  --enable-auto-stop=true

# Create a database in the cluster
az kusto database create \
  --cluster-name $clusterName \
  --database-name $databaseName \
  --read-write-database location=$location soft-delete-period=P365D hot-cache-period=P31D \
  --resource-group $hmsRgName

# Create and assign a managed identity to use
# when authenticating from IoT Central.
# This assumes your IoT Central was created in the default
# IOTC resource group.
MI_JSON=$(az iot central app identity assign \
  --name $centralUrlPrefix \
  --resource-group IOTC --system-assigned)

## Assign the managed identity permissions to use the database.
az kusto database-principal-assignment create \
  --cluster-name $clusterName \
  --database-name $databaseName \
  --principal-id $(jq -r .principalId <<< $MI_JSON) \
  --principal-assignment-name $centralUrlPrefix \
  --resource-group $hmsRgName \
  --principal-type App \
  --tenant-id $(jq -r .tenantId <<< $MI_JSON) \
  --role Admin

echo "Azure Data Explorer URL: $(az kusto cluster show --name $clusterName --resource-group $hmsRgName --query uri -o tsv)" 

```

### Reference
- Azure IoT Centralチュートリアル - マイクロソフト系技術情報 Wiki  
https://techinfoofmicrosofttech.osscons.jp/index.php?Azure%20IoT%20Central%E3%83%81%E3%83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB