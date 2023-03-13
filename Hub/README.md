# Hub
Contents about IoT Hub.

## IaC
This IaC builds Azure IoT Hub.

### Variables

#### Define
```Bash
subscriptionID=$(az account show --query id --output tsv)
userPrincipalName=$(az ad signed-in-user show --query userPrincipalName --output tsv)
location=westus2
hmsRgName=HmsRG
iotHubName=OsscJpDevInfra
iotDeviceID=mydevice1
pnpDeviceID=my-pnp-device1
```

#### Check
```Bash
echo $subscriptionID
echo $userPrincipalName
echo $location
echo $hmsRgName
echo $iotHubName
echo $iotDeviceID
echo $pnpDeviceID
```

### Creating

#### IoT Hub
In Free Tier you only can use 2 partions.

```Bash
az group create --name $hmsRgName --location $location

az iot hub create \
  --name $iotHubName \
  --resource-group $hmsRgName \
  --location $location \
  --sku F1 --partition-count 2

# Copy and save the connection string.
az iot hub show-connection-string --hub-name $iotHubName
```

#### IoT Device

```Bash
az iot hub device-identity create \
  --device-id $iotDeviceID \
  --hub-name $iotHubName

# Copy and save the connection string.
az iot hub device-identity connection-string show \
  --device-id $iotDeviceID \
  --hub-name $iotHubName \
  --output table
```

#### IoT DPS

##### DPS

```Bash
az iot dps create \
  --resource-group $hmsRgName \
  --name $iotHubName

hubConnectionString=$( \
  az iot hub connection-string show \
  -n $iotHubName \
  --key primary \
  --query connectionString -o tsv)
  
echo $hubConnectionString

az iot dps linked-hub create \
  --dps-name $iotHubName \
  --resource-group $hmsRgName \
  --location $location \
  --connection-string $hubConnectionString
  
# Copy and save the properties.idScope.
az iot dps show --name $iotHubName --query properties.idScope
```

##### PNP Device

```Bash
az iot hub device-identity create \
  --device-id $pnpDeviceID \
  --hub-name $iotHubName

# Copy and save the connection string.
az iot hub device-identity connection-string show \
  --device-id $pnpDeviceID \
  --hub-name $iotHubName \
  --output table

# DPS enrollment create
az iot dps enrollment create \
  --attestation-type symmetrickey \
  --dps-name $iotHubName --resource-group $hmsRgName \
  --enrollment-id $pnpDeviceID --device-id $pnpDeviceID \
  --query '{registrationID:registrationId,primaryKey:attestation.symmetricKey.primaryKey}'
```

### Operation

#### Monitoring
Run in independent sessions (tabs).
```Bash
az iot hub monitor-events --output table -p all -n $iotHubName
```

#### Device simulator

##### Device 2 Cloud
Run in independent sessions (tabs).

- Create device simulate and send D2C message.
```Bash
az iot device simulate -d $iotDeviceID -n $iotHubName
```

##### Cloud 2 Device
Run in independent sessions (tabs).

- Send C2D message
```Bash
az iot device c2d-message send -d $iotDeviceID --data "Hello World" --props "key0=value0;key1=value1" -n $iotHubName
```

- Send direct method
```Bash
az iot hub invoke-device-method --mn MySampleMethod -d $iotDeviceID -n $iotHubName
```

- update and reference device-twin properties
```Bash
az iot hub device-twin update -d $iotDeviceID --desired '{"conditions":{"temperature":{"warning":98, "critical":107}}}' -n $iotHubName
az iot hub device-twin show -d $iotDeviceID --query properties.reported -n $iotHubName
```

#### Sample programs

##### Build project
- [Device](./Device)
  - Please set the value to DeviceConnectionString in advance.
  - It can be executed from the following menu.  
![image](https://user-images.githubusercontent.com/7278770/212090014-03b0fe2d-6ac7-40c1-b446-ec4f4bbb5888.png)
    - SendD2C  
Send D2C message
    - ReceiveC2D  
Receive C2D message
    - UpdateTwinProperties  
Update　twin properties
    - UploadFiles  
Upload files

- [Cloud](./Cloud)
  - Please set the value to HubConnectionString in advance.
  - It can be executed from the following menu.  
![image](https://user-images.githubusercontent.com/7278770/212092547-bb47118e-9276-41cc-8e05-9a1871ba1b2d.png)
    - SendC2D  
Send C2D message
    - UpdateTwinTags  
Update twin tags
    - QueryTwinTags  
Query twin tags
    - ReceiveFileUploadNotification  
Receive file upload notification

##### [Create a storage account and container](https://github.com/OpenTouryoProject/DxCommon/tree/master/AzureIaC/Storage)  
```Bash
storageAccountName=osscjpdevinfra
containerName=dxcmn

az storage account create \
  --name $storageAccountName \
  --resource-group $hmsRgName \
  --location $location \
  --sku Standard_LRS
  
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee $userPrincipalName \
  --scope /subscriptions/${subscriptionID}/resourceGroups/${hmsRgName}/providers/Microsoft.Storage/storageAccounts/${storageAccountName}

az storage container create \
  --account-name $storageAccountName \
  --name $containerName \
  --auth-mode login
```

##### Message routing to a storage account
```Bash
iotHubRoutingName=Device2Storage
iotHubRoutingEPName=2StorageEP
storageAccountConnStr=$(az storage account show-connection-string --name $storageAccountName --query connectionString -o tsv)

az iot hub routing-endpoint create \
  --connection-string $storageAccountConnStr \
  --endpoint-name $iotHubRoutingEPName \
  --endpoint-resource-group $hmsRgName \
  --endpoint-subscription-id $subscriptionID \
  --endpoint-type azurestoragecontainer \
  --hub-name $iotHubName \
  --container $containerName \
  --resource-group $hmsRgName \
  --encoding json

az iot hub route create \
  --name $iotHubRoutingName \
  --hub-name $iotHubName \
  --resource-group $hmsRgName \
  --source devicemessages \
  --endpoint-name $iotHubRoutingEPName \
  --enabled true \
  --condition 'messageType="maintenance"'
```

##### Configure file upload
```Bash
az storage container list --connection-string $storageAccountConnStr

az iot hub update --name $iotHubName \
  --fileupload-storage-connectionstring $storageAccountConnStr \
  --fileupload-storage-container-name $containerName

az iot hub update --name $iotHubName --fileupload-sas-ttl 1

az iot hub update --name $iotHubName \
  --fileupload-notifications true \
  --fileupload-notification-max-delivery-count 10 \
  --fileupload-notification-ttl 1 \
  --fileupload-notification-lock-duration 60

az iot hub show --name $iotHubName --query '[properties.storageEndpoints, properties.enableFileUploadNotifications, properties.messagingEndpoints.fileNotifications]'
```

##### Enriched message routing
```Bash
# the free tier only allows you to set up one endpoint.
iotHubRoutingEPName=2StorageEP

# iot hub message-enrichment create
az iot hub message-enrichment create \
  --key myIotHub \
  --value $iotHubName \
  --endpoints $iotHubRoutingEPName \
  --name $iotHubName

az iot hub message-enrichment create \
  --key DeviceLocation \
  --value '$twin.tags.location' \
  --endpoints $iotHubRoutingEPName \
  --name $iotHubName
  
az iot hub device-twin update \
  --hub-name $iotHubName \
  --device-id $iotDeviceID \
  --tags '{"location": "Plant 43"}'
  
az iot hub device-twin show \
  --hub-name $iotHubName \
  --device-id $iotDeviceID
```

### Reference
- Azure IoT Hubチュートリアル - マイクロソフト系技術情報 Wiki  
https://techinfoofmicrosofttech.osscons.jp/index.php?Azure%20IoT%20Hub%E3%83%81%E3%83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB
