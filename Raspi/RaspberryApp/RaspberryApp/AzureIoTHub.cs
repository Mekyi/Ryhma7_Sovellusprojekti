using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Devices.Tpm;
using Newtonsoft.Json;

namespace RaspberryApp
{
    class AzureIoTHub
    {
        private static void CreateClient()
        {
            if (deviceClient == null)
            {
                Microsoft.Devices.Tpm.TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM
                string hubUri = myDevice.GetHostName();
                string deviceId = myDevice.GetDeviceId();
                string sasToken = myDevice.GetSASToken();

                var deviceClient = DeviceClient.Create(
                    hubUri,
                    AuthenticationMethodFactory.
                        CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Mqtt);
            }
        }

        static DeviceClient deviceClient = null;

        //
        // Note: this connection string is specific to the device "$deviceId$". To configure other devices,
        // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
        //
        const string deviceConnectionString = "HostName=$iotHubUri$;DeviceId=$deviceId$;SharedAccessKey=$deviceKey$";


        //
        // To monitor messages sent to device "kraaa" use iothub-explorer as follows:
        //    iothub-explorer monitor-events --login HostName=$iotHubUri$;SharedAccessKeyName=service;SharedAccessKey=$servicePrimaryKey$ "$deviceId$"
        //

        // Refer to http://aka.ms/azure-iot-hub-vs-cs-2017-wiki for more information on Connected Service for Azure IoT Hub

        public static async Task SendDeviceToCloudMessageAsync(Measurement measurement)
        {
            TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM
            string hubUri = myDevice.GetHostName();
            string deviceId = myDevice.GetDeviceId();
            string sasToken = myDevice.GetSASToken();

            var deviceClient = DeviceClient.Create(
                hubUri,
                AuthenticationMethodFactory.
                    CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Amqp);

            measurement.DeviceId = deviceId;

            string json = JsonConvert.SerializeObject(measurement);

            var msg = new Message(Encoding.UTF8.GetBytes(json));

            await deviceClient.SendEventAsync(msg);
        }


        public static async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
            CreateClient();

            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    await deviceClient.CompleteAsync(receivedMessage);
                    return messageData;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    //$directMethod$$deviceTwin$}
    }
}

