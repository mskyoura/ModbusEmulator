namespace ModbusEmulator
{
    public class EmulatorOptions
    {
        public string PortName { get; init; } = "COM1";
        public int PortTimeout { get; init; } = 500;
        public int MinResponseInterval { get; init; } = 200;
        public string[] DevicesAddress { get; set; } = [];
    }
}