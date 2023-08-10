# IoT Edge
Contents about Develop IoT Edge custom module by VSCode.

## IaC
- This IaC builds Azure IoT Edge custom module.
- This procedure requires the following prerequisites
  - Your own Windows
  - Nested virtualization
  - Visual Studio Code
  - Azure CLI
  - Docker Desktop for Windows

### Variables
- [Define and check variables in advance.](../README.md)
- [Define and check variables in advance.](./README.md)

#### Define
```Bash
AcrName=osscjpdevinfra
```

#### Check
```Bash
echo $AcrName
```

### Creating

#### Azure IoT Hub
[Create IoT Hub in advance.](../README.md)

#### Azure IoT Edge
[Create IoT Edge in advance.](./README.md)

#### Azure Container Registry
- Create Azure Container Registry.
```Bash
az acr create --name $AcrName --resource-group $hmsRgName --sku Basic
RegistryName=$(az acr show --name $AcrName --query loginServer --output tsv)
echo $RegistryName
```

#### Checking
- [Check IoT Hub in advance.](../README.md)
- [Check IoT Edge in advance.](./README.md)
- Check Azure Container Registry.  
This command is executed at the local CMD.
```CMD
az login
set AcrName=osscjpdevinfra
az acr show --name %AcrName% --query loginServer --output tsv
for /f "usebackq delims=" %A in (`az acr show --name %AcrName% --query loginServer --output tsv`) do set RegistryName=%A
set RegistryName
echo %RegistryName%
az acr login --name %RegistryName%
docker pull mcr.microsoft.com/hello-world
docker tag mcr.microsoft.com/hello-world %RegistryName%/hello-world:v1
docker push %RegistryName%/hello-world:v1
az acr repository list --name %RegistryName% --output table
az acr repository show-tags --name %RegistryName% --repository hello-world --output table
```

- Login to Azure Container Registry by token.  
This command is executed at the local CMD.
```CMD
az acr login --name %RegistryName% --expose-token
for /f "usebackq delims=" %A in (`az acr login --name %RegistryName% --expose-token --query accessToken --output tsv`) do set AccessToken=%A
set AccessToken
echo %AccessToken%
docker login %RegistryName% -u 00000000-0000-0000-0000-000000000000 -p %AccessToken%
```

- Login to Azure Container Registry by uid&pwd.  
This command is executed at the local CMD.
```CMD
az acr update --name %RegistryName% --admin-enabled true
az acr credential show --name %RegistryName% --query "passwords[0].value"
for /f "usebackq delims=" %A in (`az acr credential show --name %RegistryName% --query "passwords[0].value"`) do set Password=%A
set Password
echo %Password%
docker login %RegistryName% -u %AcrName% -p %Password%
```

### Development

#### Environment
This command is executed at the local CMD.
```CMD
pip list
pip freeze > uninstall.txt
pip uninstall -y -r uninstall.txt
pip install -U iotedgedev
```

#### Solution
- This command is executed at the local CMD.
```CMD
mkdir iotedgesolution
cd iotedgesolution
iotedgedev solution init --template csharp
```
- The iotedgedev solution init script prompts you to complete several steps including:
  - Authenticate to Azure
  - Choose an Azure subscription
  - Choose or create a resource group
  - Choose or create an Azure IoT Hub
  - Choose or create an Azure IoT Edge device
- Reference
  - https://learn.microsoft.com/ja-jp/azure/iot-edge/tutorial-develop-for-linux
  - https://learn.microsoft.com/ja-jp/azure/iot-edge/tutorial-develop-for-linux

#### Deployment
This shell is executed at the VSCode GitBash terminal.

- Login to Azure Container Registry by uid&pwd.  
```Bash
az login
AcrName=osscjpdevinfra
echo $AcrName
RegistryName=$(az acr show --name $AcrName --query loginServer --output tsv)
echo $RegistryName
Password=$(az acr credential show --name $RegistryName --query "passwords[0].value" | sed 's/"//g')
echo $Password

az acr login -n $RegistryName
docker login $RegistryName -u $AcrName -p $Password
```

- Build & push & set edge module container. 
```Bash
subscriptionID=$(az account show --query id --output tsv)
userPrincipalName=$(az ad signed-in-user show --query userPrincipalName --output tsv)
location=westus2
hmsRgName=HmsRG
iotHubName=OsscJpDevInfra
iotEdgeID=myedge1

hubConnectionString=$( \
  az iot hub connection-string show \
  -n $iotHubName \
  --key primary \
  --query connectionString -o tsv)

docker build --rm -f "./modules/filtermodule/Dockerfile.amd64.debug" -t osscjpdevinfra.azurecr.io/filtermodule:0.0.1-amd64 "./modules/filtermodule"
docker push osscjpdevinfra.azurecr.io/filtermodule:0.0.1-amd64
az iot edge set-modules --hub-name $iotHubName --device-id $iotEdgeID --content ./deployment.template.json --login $hubConnectionString
```

#### Checking
```PowerShell
Connect-EflowVm
```

```Bash
sudo iotedge list
```

### Reference
- Azure IoT Edgeチュートリアル - マイクロソフト系技術情報 Wiki https://techinfoofmicrosofttech.osscons.jp/index.php?Azure%20IoT%20Edge%E3%83%81%E3%83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB
