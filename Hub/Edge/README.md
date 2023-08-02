# IoT Edge
Contents about IoT Edge.

## IaC
This IaC builds Azure IoT Edge.

### Variables
[Define and check variables in advance.](../README.md)

#### Define
```Bash
iotEdgeID=myedge1
vmUser=[...]
vmPassword=[...]
```

#### Check
```Bash
echo $iotEdgeID
echo $vmUser
echo $vmPassword
```

### Creating

#### IoT Hub
[Create IoT Hub in advance.](../README.md)

#### IoT Edge
- Register an IoT Edge device
```Bash
az iot hub device-identity create \
  --device-id $iotEdgeID \
  --hub-name $iotHubName \
  --edge-enabled

edgeConnectionString=$( \
  az iot hub device-identity connection-string show \
    --device-id $iotEdgeID \
    --hub-name $iotHubName \
    --output tsv)

echo $edgeConnectionString
```

#### IoT Edge device

##### Linux

###### Deploy the IoT Edge device
```Bash
az deployment group create \
--resource-group $hmsRgName \
--template-uri "https://raw.githubusercontent.com/Azure/iotedge-vm-deploy/1.4/edgeDeploy.json" \
--parameters dnsLabelPrefix=hoge \
--parameters adminUsername=$vmUser \
--parameters deviceConnectionString=$edgeConnectionString \
--parameters authenticationType='password' \
--parameters adminPasswordOrKey=$vmPassword
```

Copy the value of the public SSH entry of the outputs section.

###### View the IoT Edge runtime status on the IoT Edge device.
- SSH connection using public SSH entry.

- Check to see that IoT Edge is running.
```Bash
sudo iotedge system status
```

- If you need to troubleshoot the service, retrieve the service logs.
```Bash
sudo iotedge system logs
```

- View all the modules running on your IoT Edge device.
```Bash
sudo iotedge list
```

###### Deploy a module to the IoT Edge device
- Sign in to the Azure portal and go to your IoT hub.
- From the menu on the left, under Device Management, select Devices.
- Select the device ID of the target IoT Edge device from the list.
- On the upper bar, select Set Modules.
  - Add drop-down menu, and then select Marketplace Module at [Modules].
  - In IoT Edge Module Marketplace, search for and select the [Simulated Temperature Sensor] module.
  - The module is added to the IoT Edge Modules section with the desired running status.
  - Select [Next: Routes] to continue to the next step of the wizard.
  - A route named SimulatedTemperatureSensorToIoTHub was created automatically at [Routes].
  - Select [Next: Review + create] to continue to the next step of the wizard.
  - Review the JSON file, and then select [Create] at [Review and create].
- After you create the module deployment details, the wizard returns you to the device details page.
- View the deployment status on the Modules tab. Wait a few minutes, and then refresh the page.

###### View edge modules on the Edge device
- SSH connection using public SSH entry.

- View all the modules running on the Edge device.
```Bash
sudo iotedge list
```

- View generated data by modules on the Edge device.
```Bash
sudo iotedge logs SimulatedTemperatureSensor -f
```

##### Windows

###### Deploy the IoT Edge device
- Run PowerShell as an administrator.

- Run the following command to enable Hyper-V.
```PowerShell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

- Run each of the following commands to download IoT Edge for Linux on Windows.
```PowerShell
$msiPath = $([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))
Invoke-WebRequest "https://aka.ms/AzEFLOWMSI_1_4_LTS_X64" -OutFile $msiPath
```

- Install IoT Edge for Linux on Windows on your device.
```PowerShell
Start-Process -Wait msiexec -ArgumentList "/i","$([io.Path]::Combine($env:TEMP, 'AzureIoTEdge.msi'))"
```

- You can check the current execution policy in an elevated PowerShell prompt using:
```PowerShell
Get-ExecutionPolicy -List
```

- If the execution policy of local machine is not AllSigned, you can set the execution policy using:
```PowerShell
Set-ExecutionPolicy -ExecutionPolicy AllSigned -Force
```

- Create the IoT Edge for Linux on Windows deployment.
```PowerShell
Deploy-Eflow
```

- Provision your device using the device connection string.
```PowerShell
Provision-EflowVm -provisioningType ManualConnectionString -devConnString "<CONNECTION_STRING_HERE>"
```

###### View the IoT Edge runtime status on the IoT Edge device.
- Log in to your IoT Edge for Linux on Windows virtual machine using the following command in your PowerShell session:
```PowerShell
Connect-EflowVm
```

- Once you are logged in, you can using the Linux command.

- Check to see that IoT Edge is running.
```Bash
sudo iotedge system status
```

- If you need to troubleshoot the service, retrieve the service logs.
```Bash
sudo iotedge system logs
```

- View all the modules running on your IoT Edge device.
```Bash
sudo iotedge list
```

###### Deploy a module to the IoT Edge device
[Follow the same steps as the Linux version.](#deploy-a-module-to-the-iot-edge-device)

###### View edge modules on the Edge device
- Log in to your IoT Edge for Linux on Windows virtual machine using the following command in your PowerShell session:
```PowerShell
Connect-EflowVm
```

- View all the modules running on the Edge device.
```PowerShell
sudo iotedge list
```

- View generated data by modules on the Edge device.
```Bash
sudo iotedge logs SimulatedTemperatureSensor -f
```

### Reference
- Azure IoT Edgeチュートリアル - マイクロソフト系技術情報 Wiki https://techinfoofmicrosofttech.osscons.jp/index.php?Azure%20IoT%20Edge%E3%83%81%E3%83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB
