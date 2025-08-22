using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator;

public sealed class Command
{
    public bool IsGroup { get; init; }
    public required string Id { get; init; }
    public required FuncType Func { get; init; } 
    public required string StartAddress { get; init; }

    public required int RegisterQuantity { get; init; }

    public int? DataBytesQuantity { get; init; }

    public int? CounterValue { get; init; }

    public required RelayData[] RelaysData { get; init; }

    public required string[] DevicesId { get; init; }

    public int? TimeSlot { get; init; }

    /// <summary>
    /// Парсит команду из строки в старом формате Modbus.
    /// </summary>
    /// <param name="rawData">Строка команды</param>
    /// <returns>Объект команды или null при ошибке парсинга</returns>
    public static Command? Parse(string rawData, int? counterValue = null)
    {
        try
        {
            if (string.IsNullOrEmpty(rawData) || !rawData.StartsWith(':'))
                return null;
            rawData = rawData.Replace(":", "").Replace("\r\n", "");
            if (rawData.Length < 14)
                return null;
            var lrc = rawData[^2..];
            rawData = rawData.Substring(0, rawData.Length - 2);
            if (String.Compare(lrc, LRCCalculator.Calculate(rawData)) != 0)
                return null;
            var id = rawData.Substring(0, 2);   
            var func = rawData.Substring(2, 2);
            var startAddress = rawData.Substring(4, 4);
            var registerQuantity = int.Parse(rawData.Substring(8, 4), System.Globalization.NumberStyles.HexNumber);
            if (rawData.Length > 12)
            {
                List<RelayData> relaysData = new();
                var dataBytesQuantity = int.Parse(rawData.Substring(12, 2), System.Globalization.NumberStyles.HexNumber);
                var commandData = rawData.Substring(14, rawData.Length - 14);
                if (commandData.Length / 2 != dataBytesQuantity)
                    return null;
                if (dataBytesQuantity >= 14)
                {
                    int index = 0;
                    var counter = int.Parse(commandData.Substring(index, 4), System.Globalization.NumberStyles.HexNumber);
                    index += 4;
                    for (int cnt = 0; cnt < 3; cnt++)
                    {
                        if (commandData.Substring(index, 8).Any(ch => ch != 'F'))
                        {
                            var relayStatus = (RelayStatus)int.Parse(commandData.Substring(index, 2), System.Globalization.NumberStyles.HexNumber);
                            index += 2;
                            var delay = int.Parse(commandData.Substring(index, 2), System.Globalization.NumberStyles.HexNumber);
                            index += 2;
                            var duration = int.Parse(commandData.Substring(index, 4), System.Globalization.NumberStyles.HexNumber);
                            index += 4;
                            RelayData relayData = new()
                            {
                                RelayStatus = relayStatus,
                                Delay = delay,
                                Duration = duration
                            };
                            relaysData.Add(relayData);
                        }
                        else
                        {
                            index += 8;
                            relaysData.Add(new());
                        } 
                            
                    }
                    if (dataBytesQuantity == 24)
                    {
                        List<string> devicesId = new();
                        for (int cnt = 0; cnt < 8; cnt++, index += 2)
                        {
                            var device = commandData.Substring(index, 2);
                            if (device.Any(c => c != '0'))
                            {
                                devicesId.Add(device);
                            }
                        }
                        index += 2;
                        int timeSlot = int.Parse(commandData.Substring(index, 2), System.Globalization.NumberStyles.HexNumber) * 10;
                        return new Command()
                        {
                            StartAddress = startAddress,
                            CounterValue = counter,
                            DataBytesQuantity = dataBytesQuantity,
                            Func = GetCommandFromString(func),
                            Id = id,
                            IsGroup = id == "FF",
                            RegisterQuantity = registerQuantity,
                            RelaysData = relaysData.ToArray(),
                            DevicesId = devicesId.ToArray(),
                            TimeSlot = timeSlot
                        };
                    }
                    return new Command()
                    {
                        StartAddress = startAddress,
                        CounterValue = counter,
                        DataBytesQuantity = dataBytesQuantity,
                        Func = GetCommandFromString(func),
                        Id = id,
                        IsGroup = id == "FF",
                        RegisterQuantity = registerQuantity,
                        RelaysData = relaysData.ToArray(),
                        DevicesId = []
                    };
                }
            }
            else
            {
                return new Command
                {
                    Id = id,
                    Func = GetCommandFromString(func),
                    StartAddress = startAddress,
                    RegisterQuantity = registerQuantity,
                    CounterValue = counterValue ?? 0,
                    RelaysData = [],
                    DevicesId = []
                };
            }   
        }
        catch (Exception)
        {
            
        }
        return null;
    }

    private static FuncType GetCommandFromString(string func)
    {
        return func == "04" ? FuncType.Read : func == "10" ? FuncType.Write :
                            throw new NotSupportedException();
    }
}
