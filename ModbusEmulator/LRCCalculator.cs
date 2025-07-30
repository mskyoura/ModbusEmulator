using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator;

public static class LRCCalculator
{
    public static string Calculate(string toEncode)
    {
        byte[] bytes = Convert.FromHexString(toEncode);
        int lrc = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            lrc += bytes[i];
        }
        lrc = 256 - (lrc % 256);
        return HexToStringConverter.Convert(lrc, 1);
    }
}
