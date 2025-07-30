using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator;

public sealed class RelayData
{
    public RelayStatus RelayStatus { get; init; } 

    public double Delay { get; init; }

    public int Duration { get; init; }
}
