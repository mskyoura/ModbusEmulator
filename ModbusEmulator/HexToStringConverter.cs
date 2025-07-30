using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator;

public static class HexToStringConverter
{
    public static string Convert(int value, int quantyOfNumbers) => value.ToString($"X{quantyOfNumbers}");
}
