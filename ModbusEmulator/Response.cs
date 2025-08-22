using System.Text;

namespace ModbusEmulator;

public sealed class Response
{
    public required string Id { get; init; }
    public required FuncType Func {  get; init; }
    public string FirmwareVersion = "0009";
    public double Voltage = 12.30324;
    public int CmdNumber { get; init; }
    public int Input = 0;
    public RelayStatus[] RelaysStatus = [RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF];
    public int PreSendDelay { get; init; }

    public override string ToString()
    {
        return Func switch
        {
            FuncType.Read => FormatReadResponse(),
            FuncType.Write => FormatWriteResponse(),
            _ => String.Empty
        };
    }

    private string VoltageToHex()
    {
        int value = (int)((Voltage - 0.6) / 0.06612);
        return value.ToString("X4");
    }

    private string FormatWriteResponse()
    {
        // Формат: :<ID>10<startAddr><regCount><LRC>\r\n
        string payload = $"{Id}100000000C";
        string lrc = LRCCalculator.Calculate(payload);
        return $":{payload}{lrc}\n\r";
    }

    private string FormatReadResponse()
    {
        // Формат: :<ID>04<byteCount><data><LRC>\r\n

        //генерация данных регистров
        var data = new StringBuilder();
        data.Append(FirmwareVersion);
        data.Append(VoltageToHex());
        data.Append(RelayStatusToHex());
        data.Append(CmdNumber.ToString("X4"));
        
        // Добавляем 8 байт нулей для резервных регистров (0014-0017)
        data.Append("0000000000000000");

        string byteCount = (data.Length / 2).ToString("X2");
        string payload = $"{Id}04{byteCount}{data}";
        string lrc = LRCCalculator.Calculate(payload);
        return $":{payload}{lrc}\n\r";
    }

    private string RelayStatusToHex()
    {
        // Формирование битового поля состояния согласно формату xxxx1321
        // где 1 - вход, 3 - состояние Реле 3, 2 - состояние Реле 2, 1 - состояние Реле 1
        int status = 0;
        
        // Устанавливаем бит входа (если Input = 1)
        if (Input == 1)
            status |= 0x08;
        
        // Устанавливаем биты реле (1, 2, 3)
        if (RelaysStatus[0] == RelayStatus.ON) // Реле 1
            status |= 0x01;
        if (RelaysStatus[1] == RelayStatus.ON) // Реле 2  
            status |= 0x02;
        if (RelaysStatus[2] == RelayStatus.ON) // Реле 3
            status |= 0x04;

        return status.ToString("X4");
    }
}