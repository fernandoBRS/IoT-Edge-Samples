:: Install IoT Edge control script
pip install -U azure-iot-edge-runtime-ctl

:: Configure the IoT Edge control
iotedgectl setup --connection-string "<device connection string>" --auto-cert-gen-force-no-passwords

:: Start the IoT Edge runtime
iotedgectl start