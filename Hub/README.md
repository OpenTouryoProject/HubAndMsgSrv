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
iotDeviceId=mydevice1
```

#### Check
```Bash
echo $subscriptionID
echo $userPrincipalName
echo $location
echo $hmsRgName
echo $iotHubName
echo $iotDeviceId
```

### Creating

```Bash
az group create --name $hmsRgName --location $location

az iot hub create \
  --name $iotHubName \
  --resource-group $hmsRgName \
  --location $location \
  --sku F1

# Copy and save the connection string.
az iot hub show-connection-string --hub-name $iotHubName

az iot hub device-identity create \
  --device-id $iotDeviceId \
  --hub-name $iotHubName

# Copy and save the connection string.
az iot hub device-identity show-connection-string \
  --device-id $iotDeviceId \
  --hub-name $iotHubName
  --output table

```

### Reference
- Azure IoT Hubチュートリアル - マイクロソフト系技術情報 Wiki
https://techinfoofmicrosofttech.osscons.jp/index.php?Azure%20IoT%20Hub%E3%83%81%E3%83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB