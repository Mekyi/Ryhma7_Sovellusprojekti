using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Devices.Tpm;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace IoTCore.RaspberrySenseHatApp
{
    internal static class AzureIoTHub
    {
        private static readonly TimeSpan RefreshClientBefore = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan SasTokenValidity = TimeSpan.FromHours(2);
        private static DeviceClient _deviceClient;
        private static DateTime _validityExpiryTime;
        private static string _deviceId;
        //
        // This sample assumes the device has been connected to Azure with the IoT Dashboard
        //
        // Refer to http://aka.ms/azure-iot-hub-vs-cs-wiki for more information on Connected Service for Azure IoT Hub

        private static string GetDeviceId()
        {
            var device = new TpmDevice(0);
            return device.GetDeviceId();
        }

        public static async Task SendDeviceToCloudMessageAsync(Measurement measurement)
        {
            if (_deviceId == null)
            {
                _deviceId = GetDeviceId();
            }

            DeviceClient client = GetDeviceClient();

            measurement.DeviceId = _deviceId;

            string json = JsonConvert.SerializeObject(measurement);

            var message = new Message(Encoding.UTF8.GetBytes(json));
            await client.SendEventAsync(message);
        }

        public static async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
            var myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
            string hubUri = myDevice.GetHostName();
            string deviceId = myDevice.GetDeviceId();
            string sasToken = myDevice.GetSASToken((uint)SasTokenValidity.TotalSeconds);

            var deviceClient = DeviceClient.Create(
                hubUri,
                AuthenticationMethodFactory.
                    CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Amqp);

            while (true)
            {
                Message receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    byte[] bytes = receivedMessage.GetBytes();
                    if (bytes != null && bytes.Length > 0)
                    {
                        string messageData = Encoding.ASCII.GetString(bytes);
                        await deviceClient.CompleteAsync(receivedMessage);
                        return messageData;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static DeviceClient GetDeviceClient()
        {
            if (_deviceClient == null)
            {
                RefreshDeviceClient();
            }
            else
            {
                TimeSpan remainingUntilExpiry = _validityExpiryTime - DateTime.Now;
                if (remainingUntilExpiry <= RefreshClientBefore)
                {
                    RefreshDeviceClient();
                }
            }

            return _deviceClient;
        }

        private static void RefreshDeviceClient()
        {
            var myDevice = new TpmDevice(0); // Use logical device 0 on the TPM
            string hubUri = myDevice.GetHostName();
            string deviceId = myDevice.GetDeviceId();
            string sasToken = myDevice.GetSASToken((uint)SasTokenValidity.TotalSeconds);
            _validityExpiryTime = DateTime.Now + SasTokenValidity;
            _deviceClient = DeviceClient.Create(
                hubUri,
                AuthenticationMethodFactory.
                    CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Amqp);
        }
    }
}