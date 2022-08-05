# Stream Analytics
Contents about Stream Analytics.

## IaC
This IaC builds Azure Stream Analytics.

### Variables

#### Define
```Bash
location=westus2
hmsRgName=HmsRG
iotHubName=OsscJpDevInfra
iotDeviceId=mydevice1
streamAnalyticsJobName=hub2storage
storageAccountName=osscjpdevinfra
```

#### Check
```Bash
echo $location
echo $hmsRgName
echo $iotHubName
echo $iotDeviceId
echo $streamAnalyticsJobName
echo $storageAccountName
```

### Creating
You can also upload and use files in Cloud Shell.

```Bash

az stream-analytics input create \
  --resource-group $hmsRgName \
  --job-name $streamAnalyticsJobName \
  --name input2storage \
  --type Stream \
  --datasource datasource.json \
  --serialization serialization.json

az stream-analytics output create \
  --resource-group $hmsRgName \
  --job-name $streamAnalyticsJobName \
  --name output2storage \
  --datasource datasink.json \
  --serialization serialization.json

az stream-analytics transformation create \
  --resource-group $hmsRgName \
  --job-name $streamAnalyticsJobName \
  --name transformation2storage \
  --streaming-units "1" \
  --transformation-query "SELECT * INTO output2storage FROM input2storage"

az stream-analytics job start \
  --resource-group $hmsRgName \
  --name $streamAnalyticsJobName \
  --output-start-mode JobStartTime

    
```

### Reference
- Azure IoT Hubチュートリアル - マイクロソフト系技術情報 Wiki
https://techinfoofmicrosofttech.osscons.jp/index.php?Azure%20IoT%20Hub%E3%83%81%E3%83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB