using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator;

public sealed class RelayData
{
    public RelayStatus RelayStatus { get; init; } = RelayStatus.OFF;

    public double? Delay { get; init; } = null;

    public double? Duration { get; init; } = null;
}
