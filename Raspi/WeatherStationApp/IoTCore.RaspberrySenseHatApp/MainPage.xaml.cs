using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Emmellsoft.IoT.Rpi.SenseHat;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using Emmellsoft.IoT.Rpi.SenseHat.Fonts.SingleColor;

namespace IoTCore.RaspberrySenseHatApp
{
    public sealed partial class MainPage : Page
    {
        public static volatile bool Closing;
        TinyFont tinyFont = new TinyFont();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Task dataTask = CollectSensorDataAsync(new Progress<Measurement>(m =>
            {
                measurementText.Text = $"{m.Temperature:F2} degrees, {m.Humidity:F2} RH%, {m.Pressure:F2} mbar";
            }));
            Task msgTask = ReceiveMessagesAsync();

            await Task.WhenAll(dataTask, msgTask);
        }

        private async Task CollectSensorDataAsync(IProgress<Measurement> progress)
        {
            using (ISenseHat senseHat = await SenseHatFactory.GetSenseHat())
            {
                while (Closing == false)
                {
                    try
                    {
                        senseHat.Sensors.HumiditySensor.Update();
                        senseHat.Sensors.PressureSensor.Update();

                        var measurement = new Measurement()
                        {
                            Temperature = senseHat.Sensors.Temperature ?? 0,
                            Humidity = senseHat.Sensors.Humidity ?? 0,
                            Pressure = senseHat.Sensors.Pressure ?? 0,
                        };

                        #region SenseHAT LED
                        senseHat.Display.Clear();

                        int temperature = (int)Math.Round(measurement.Temperature);
                        string temperatureText = temperature.ToString();
                        tinyFont.Write(senseHat.Display, temperatureText, Colors.Aqua);
                        senseHat.Display.Update();
                        #endregion

                        progress.Report(measurement);

                        await AzureIoTHub.SendDeviceToCloudMessageAsync(measurement);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception: " + e.Message);
                    }
                    await Task.Delay(5000);
                }
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            while (Closing == false)
            {
                try
                {
                    string message = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
                    Debug.WriteLine("Received message: " + message);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception: " + e.Message);
                }
            }
        }
    }
}
