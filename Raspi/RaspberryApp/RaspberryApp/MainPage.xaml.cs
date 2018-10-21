using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Emmellsoft.IoT.Rpi.SenseHat;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RaspberryApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await CollectSensorData(new Progress<Measurement>(m =>
            {
                txtTest.Text = $"{m.Temperature:F2} degrees, {m.Humidity:F2} RH%, {m.Pressure:F2} mbar";
                Debug.WriteLine(txtTest.Text);
            }));
        }

        private async Task CollectSensorData(IProgress<Measurement> progress)
        {
            using (ISenseHat senseHat = await SenseHatFactory.GetSenseHat())
            {
                for (; ; )
                {
                    try
                    {
                        senseHat.Sensors.HumiditySensor.Update();
                        senseHat.Sensors.PressureSensor.Update();
                        var measurement = new Measurement()
                        {
                            Temperature = senseHat.Sensors.Temperature ?? 0,
                            Humidity = senseHat.Sensors.Humidity ?? 0,
                            Pressure = senseHat.Sensors.Pressure ?? 0
                        };

                        progress.Report(measurement);

                        await AzureIoTHub.SendDeviceToCloudMessageAsync(measurement);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception: " + e.Message);
                    }
                    await Task.Delay(1000);
                }
            }
        }

    }


}
