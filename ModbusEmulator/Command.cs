using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator;

public sealed class Command
{
    public bool IsGroup { get; init; }
    public string Id { get; init; } = string.Empty;
    public string Func { get; init; } = string.Empty;
    public string StartAddress { get; init; } = string.Empty;

    public int RegisterQuantity { get; init; }

    public int? DataBytesQuantity { get; init; }

    public int? CounterValue { get; init; }

    public Dictionary<int, RelayData> RelaysData { get; init; } = new();

    /// <summary>
    /// Парсит команду из строки в старом формате Modbus.
    /// </summary>
    /// <param name="data">Строка команды</param>
    /// <returns>Объект команды или null при ошибке парсинга</returns>
    public static Command? Parse(string data)
    {
        try
        {
            if (string.IsNullOrEmpty(data) || !data.StartsWith(':'))
                return null;
            data = data.Replace(":", "").Replace("\r\n", "");
            if (data.Length < 14)
                return null;
            var lrc = data[^2..];
            data = data.Substring(0, data.Length - 2);
            if (String.Compare(lrc, LRCCalculator.Calculate(data)) != 0)
                return null;
            var id = data.Substring(0, 2);
            var func = data.Substring(2, 2);
            var startAddress = data.Substring(4, 4);
            var registerQuantity = int.Parse(data.Substring(8, 4), System.Globalization.NumberStyles.HexNumber);
            if (data.Length > 12)
            {
                Dictionary<int, RelayData> relaysData = new();
                var dataBytesQuantity = int.Parse(data.Substring(12, 2), System.Globalization.NumberStyles.HexNumber);
                var counter = int.Parse(data.Substring(14, 4), System.Globalization.NumberStyles.HexNumber);
                for (int cnt = 0, index = 18; cnt < 3; cnt++)
                {
                    if (data.Substring(index, 8).Any(ch => ch != 'F'))
                    {
                        var relayStatus = (RelayStatus)int.Parse(data.Substring(index, 2));
                        index += 2;
                        var delay = int.Parse(data.Substring(index, 4)) * 0.1;
                        index += 4;
                        var duration = int.Parse(data.Substring(index, 2));
                        index += 2;
                        RelayData relayData = new()
                        {
                            RelayStatus = relayStatus,
                            Delay = delay,
                            Duration = duration
                        };
                        relaysData.Add(cnt, relayData);
                    }
                    else index += 8;

                }
                return new Command()
                {
                    StartAddress = startAddress,
                    CounterValue = counter,
                    DataBytesQuantity = dataBytesQuantity,
                    Func = func,
                    Id = id,
                    IsGroup = id == "FF",
                    RegisterQuantity = registerQuantity,
                    RelaysData = relaysData
                };
            }
            else
            {
                return new Command
                {
                    Id = id,
                    Func = func,
                    StartAddress = startAddress,
                    RegisterQuantity = registerQuantity
                };
            }




                
        }
        catch (Exception)
        {
            return null;
        }
    }
}
