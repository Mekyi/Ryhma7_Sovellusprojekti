namespace IoTCore.RaspberrySenseHatApp
{
    internal class Measurement
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public string DeviceId { get; set; }
    }
}
