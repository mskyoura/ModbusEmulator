using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ModbusEmulator.Tests;

/// <summary>
/// Юнит тесты для метода Parse класса Command.
/// </summary>
[TestFixture]
public class CommandParseTests
{
    /// <summary>
    /// Тест парсинга простой команды чтения регистров.
    /// Команда: :AA04001000083A<CR><LF>
    /// </summary>
    [Test]
    public void Parse_SimpleReadCommand_ShouldReturnValidCommand()
    {
        // Arrange
        string input = ":AA04001000083A\r\n";
        
        // Act
        var result = Command.Parse(input);
        
        // Assert
        Assert.That(result, Is.Not.Null, "Команда должна быть успешно распарсена");
        Assert.That(result!.Id, Is.EqualTo("AA"), "ID устройства должен быть AA");
        Assert.That(result.Func, Is.EqualTo(FuncType.Read), "Функциональный код должен быть Read");
        Assert.That(result.StartAddress, Is.EqualTo("0010"), "Стартовый адрес должен быть '0010'");
        Assert.That(result.RegisterQuantity, Is.EqualTo(8), "Количество регистров должно быть 8");
        Assert.That(result.IsGroup, Is.False, "Команда не должна быть групповой");
        Assert.That(result.DataBytesQuantity, Is.Null, "Количество байт данных должно быть null");
        Assert.That(result.CounterValue, Is.EqualTo(0), "Значение счетчика должно быть 0");
        Assert.That(result.RelaysData, Is.Empty, "Данные реле должны быть пустыми");
    }
    
    /// <summary>
    /// Тест парсинга команды записи с данными реле.
    /// Команда: :AB10000000070E0003FFFFFFFF01010000FFFFFFFF33<CR><LF>
    /// </summary>
    [Test]
    public void Parse_WriteCommandWithRelayData_ShouldReturnValidCommand()
    {
        // Arrange
        string input = ":AB10000000070E0003FFFFFFFF01010000FFFFFFFF33\r\n";
        
        // Act
        var result = Command.Parse(input);
        
        // Assert
        Assert.That(result, Is.Not.Null, "Команда должна быть успешно распарсена");
        Assert.That(result!.Id, Is.EqualTo("AB"), "ID устройства должен быть AB");
        Assert.That(result.Func, Is.EqualTo(FuncType.Write), "Функциональный код должен быть Write");
        Assert.That(result.StartAddress, Is.EqualTo("0000"), "Стартовый адрес должен быть '0000'");
        Assert.That(result.RegisterQuantity, Is.EqualTo(7), "Количество регистров должно быть 7");
        Assert.That(result.IsGroup, Is.False, "Команда не должна быть групповой");
        Assert.That(result.DataBytesQuantity, Is.EqualTo(14), "Количество байт данных должно быть 14 (0x0E)");
        Assert.That(result.CounterValue, Is.EqualTo(3), "Значение счетчика должно быть 3");
        
        // Проверяем данные реле
        Assert.That(result.RelaysData, Has.Length.EqualTo(3), "Должно быть 3 элемента данных реле");
        
        // Проверяем первое реле (FFFFFFFF - неактивное)
        var relay0 = result.RelaysData[0];
        Assert.That(relay0.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус первого реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay0.Delay, Is.EqualTo(0), "Задержка первого реле должна быть 0");
        Assert.That(relay0.Duration, Is.EqualTo(0), "Длительность первого реле должна быть 0");
        
        // Проверяем второе реле (01010000)
        var relay1 = result.RelaysData[1];
        Assert.That(relay1.RelayStatus, Is.EqualTo(RelayStatus.ON), "Статус второго реле должен быть ON (1)");
        Assert.That(relay1.Delay, Is.EqualTo(1), "Задержка второго реле должна быть 1");
        Assert.That(relay1.Duration, Is.EqualTo(0), "Длительность второго реле должна быть 0");
        
        // Проверяем третье реле (FFFFFFFF - неактивное)
        var relay2 = result.RelaysData[2];
        Assert.That(relay2.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус третьего реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay2.Delay, Is.EqualTo(0), "Задержка третьего реле должна быть 0");
        Assert.That(relay2.Duration, Is.EqualTo(0), "Длительность третьего реле должна быть 0");
    }
    
    /// <summary>
    /// Тест парсинга групповой команды записи с данными реле.
    /// Команда: :FF10000000070E000400000000FFFFFFFFFFFFFFFFE0<CR><LF>
    /// </summary>
    [Test]
    public void Parse_GroupWriteCommandWithRelayData_ShouldReturnValidCommand()
    {
        // Arrange
        string input = ":FF10000000070E000400000000FFFFFFFFFFFFFFFFE0\r\n";
        
        // Act
        var result = Command.Parse(input);
        
        // Assert
        Assert.That(result, Is.Not.Null, "Команда должна быть успешно распарсена");
        Assert.That(result!.Id, Is.EqualTo("FF"), "ID устройства должен быть FF");
        Assert.That(result.Func, Is.EqualTo(FuncType.Write), "Функциональный код должен быть Write");
        Assert.That(result.StartAddress, Is.EqualTo("0000"), "Стартовый адрес должен быть '0000'");
        Assert.That(result.RegisterQuantity, Is.EqualTo(7), "Количество регистров должно быть 7");
        Assert.That(result.IsGroup, Is.True, "Команда должна быть групповой (FF)");
        Assert.That(result.DataBytesQuantity, Is.EqualTo(14), "Количество байт данных должно быть 14 (0x0E)");
        Assert.That(result.CounterValue, Is.EqualTo(4), "Значение счетчика должно быть 4");
        
        // Проверяем данные реле
        Assert.That(result.RelaysData, Has.Length.EqualTo(3), "Должно быть 3 элемента данных реле");
        
        // Проверяем первое реле (00000000)
        var relay0 = result.RelaysData[0];
        Assert.That(relay0.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус первого реле должен быть OFF (0)");
        Assert.That(relay0.Delay, Is.EqualTo(0), "Задержка первого реле должна быть 0");
        Assert.That(relay0.Duration, Is.EqualTo(0), "Длительность первого реле должна быть 0");
        
        // Проверяем второе реле (FFFFFFFF - неактивное)
        var relay1 = result.RelaysData[1];
        Assert.That(relay1.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус второго реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay1.Delay, Is.EqualTo(0), "Задержка второго реле должна быть 0");
        Assert.That(relay1.Duration, Is.EqualTo(0), "Длительность второго реле должна быть 0");
        
        // Проверяем третье реле (FFFFFFFF - неактивное)
        var relay2 = result.RelaysData[2];
        Assert.That(relay2.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус третьего реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay2.Delay, Is.EqualTo(0), "Задержка третьего реле должна быть 0");
        Assert.That(relay2.Duration, Is.EqualTo(0), "Длительность третьего реле должна быть 0");
    }

    /// <summary>
    /// Тест парсинга некорректной команды (null или пустая строка).
    /// </summary>
    [Test]
    public void Parse_NullOrEmptyInput_ShouldReturnNull()
    {
        // Act & Assert
        Assert.That(Command.Parse(null!), Is.Null, "null строка должна возвращать null");
        Assert.That(Command.Parse(""), Is.Null, "Пустая строка должна возвращать null");
        Assert.That(Command.Parse("   "), Is.Null, "Строка из пробелов должна возвращать null");
    }
    
    /// <summary>
    /// Тест парсинга команды без префикса двоеточия.
    /// </summary>
    [Test]
    public void Parse_CommandWithoutColon_ShouldReturnNull()
    {
        // Arrange
        string input = "AA04001000083A\r\n";
        
        // Act
        var result = Command.Parse(input);
        
        // Assert
        Assert.That(result, Is.Null, "Команда без префикса ':' должна возвращать null");
    }
    
    /// <summary>
    /// Тест парсинга команды с некорректной контрольной суммой LRC.
    /// </summary>
    [Test]
    public void Parse_CommandWithInvalidLRC_ShouldReturnNull()
    {
        // Arrange
        string input = ":AA04001000083B\r\n"; // Некорректная LRC (3B вместо 3A)
        
        // Act
        var result = Command.Parse(input);
        
        // Assert
        Assert.That(result, Is.Null, "Команда с неверной LRC должна возвращать null");
    }
    
    /// <summary>
    /// Тест парсинга команды со слишком короткой длиной.
    /// </summary>
    [Test]
    public void Parse_TooShortCommand_ShouldReturnNull()
    {
        // Arrange
        string input = ":AA0400100008\r\n"; // Слишком короткая команда (без LRC)
        
        // Act
        var result = Command.Parse(input);
        
        // Assert
        Assert.That(result, Is.Null, "Слишком короткая команда должна возвращать null");
    }

    /// <summary>
    /// Тест парсинга групповой команды записи с расширенными данными реле и списком устройств.
    /// Команда: :FF100000000D18000501000000FFFFFFFFFFFFFFFF151400000000000000099C<CR><LF>
    /// </summary>
    [Test]
    public void Parse_GroupCommandWithDeviceListAndPartialRelayData_ShouldReturnValidCommand()
    {
        // Arrange
        string input = ":FF100000000D18000501000000FFFFFFFFFFFFFFFF151400000000000000099C\r\n";

        // Act
        var result = Command.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null, "Команда должна быть успешно распарсена");
        Assert.That(result!.Id, Is.EqualTo("FF"), "ID устройства должен быть FF");
        Assert.That(result.Func, Is.EqualTo(FuncType.Write), "Функциональный код должен быть Write");
        Assert.That(result.StartAddress, Is.EqualTo("0000"), "Стартовый адрес должен быть '0000'");
        Assert.That(result.RegisterQuantity, Is.EqualTo(13), "Количество регистров должно быть 13");
        Assert.That(result.IsGroup, Is.True, "Команда должна быть групповой (FF)");
        Assert.That(result.DataBytesQuantity, Is.EqualTo(24), "Количество байт данных должно быть 24 (0x18)");
        Assert.That(result.CounterValue, Is.EqualTo(5), "Значение счетчика должно быть 5");

        // Проверяем данные реле
        Assert.That(result.RelaysData, Is.Not.Null, "Данные реле не должны быть null");
        Assert.That(result.RelaysData, Has.Length.EqualTo(3), "Должно быть 3 элемента данных реле");

        // Проверяем первое реле (01000000)
        var relay0 = result.RelaysData[0];
        Assert.That(relay0.RelayStatus, Is.EqualTo(RelayStatus.ON), "Статус первого реле должен быть ON (1)");
        Assert.That(relay0.Delay, Is.EqualTo(0), "Задержка первого реле должна быть 0");
        Assert.That(relay0.Duration, Is.EqualTo(0), "Длительность первого реле должна быть 0");

        // Проверяем второе реле (FFFFFFFF - неактивное)
        var relay1 = result.RelaysData[1];
        Assert.That(relay1.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус второго реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay1.Delay, Is.EqualTo(0), "Задержка второго реле должна быть 0");
        Assert.That(relay1.Duration, Is.EqualTo(0), "Длительность второго реле должна быть 0");

        // Проверяем третье реле (FFFFFFFF - неактивное)
        var relay2 = result.RelaysData[2];
        Assert.That(relay2.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус третьего реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay2.Delay, Is.EqualTo(0), "Задержка третьего реле должна быть 0");
        Assert.That(relay2.Duration, Is.EqualTo(0), "Длительность третьего реле должна быть 0");

        // Проверяем список устройств
        Assert.That(result.DevicesId, Is.Not.Null, "Список устройств не должен быть null");
        Assert.That(result.DevicesId, Has.Length.EqualTo(2), "Должно быть 2 устройства в списке");
        Assert.That(result.DevicesId[0], Is.EqualTo("15"), "Первое устройство должно быть '15'");
        Assert.That(result.DevicesId[1], Is.EqualTo("14"), "Второе устройство должно быть '14'");

        // Проверяем временной слот
        Assert.That(result.TimeSlot, Is.EqualTo(90), "Временной слот должен быть 90 (0x09 * 10)");
    }

    /// <summary>
    /// Тест парсинга групповой команды записи с данными реле и списком устройств.
    /// Команда: :FF100000000D18000001000000FFFFFFFFFFFFFFFF1514000000000000001E8C<CR><LF>
    /// </summary>
    [Test]
    public void Parse_GroupCommandWithDeviceListAndRelayData_ShouldReturnValidCommand()
    {
        // Arrange
        string input = ":FF100000000D18000001000000FFFFFFFFFFFFFFFF1514000000000000001E8C\r\n";

        // Act
        var result = Command.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null, "Команда должна быть успешно распарсена");
        Assert.That(result!.Id, Is.EqualTo("FF"), "ID устройства должен быть FF");
        Assert.That(result.Func, Is.EqualTo(FuncType.Write), "Функциональный код должен быть Write");
        Assert.That(result.StartAddress, Is.EqualTo("0000"), "Стартовый адрес должен быть '0000'");
        Assert.That(result.RegisterQuantity, Is.EqualTo(13), "Количество регистров должно быть 13");
        Assert.That(result.IsGroup, Is.True, "Команда должна быть групповой (FF)");
        Assert.That(result.DataBytesQuantity, Is.EqualTo(24), "Количество байт данных должно быть 24 (0x18)");
        Assert.That(result.CounterValue, Is.EqualTo(0), "Значение счетчика должно быть 0 (0x0000)");

        // Проверяем данные реле
        Assert.That(result.RelaysData, Is.Not.Null, "Данные реле не должны быть null");
        Assert.That(result.RelaysData, Has.Length.EqualTo(3), "Должно быть 3 элемента данных реле");

        // Проверяем первое реле (01000000)
        var relay0 = result.RelaysData[0];
        Assert.That(relay0.RelayStatus, Is.EqualTo(RelayStatus.ON), "Статус первого реле должен быть ON (1)");
        Assert.That(relay0.Delay, Is.EqualTo(0), "Задержка первого реле должна быть 0");
        Assert.That(relay0.Duration, Is.EqualTo(0), "Длительность первого реле должна быть 0");

        // Проверяем второе реле (FFFFFFFF - неактивное)
        var relay1 = result.RelaysData[1];
        Assert.That(relay1.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус второго реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay1.Delay, Is.EqualTo(0), "Задержка второго реле должна быть 0");
        Assert.That(relay1.Duration, Is.EqualTo(0), "Длительность второго реле должна быть 0");

        // Проверяем третье реле (FFFFFFFF - неактивное)
        var relay2 = result.RelaysData[2];
        Assert.That(relay2.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус третьего реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay2.Delay, Is.EqualTo(0), "Задержка третьего реле должна быть 0");
        Assert.That(relay2.Duration, Is.EqualTo(0), "Длительность третьего реле должна быть 0");

        // Проверяем список устройств
        Assert.That(result.DevicesId, Is.Not.Null, "Список устройств не должен быть null");
        Assert.That(result.DevicesId, Has.Length.EqualTo(2), "Должно быть 2 устройства в списке");
        Assert.That(result.DevicesId[0], Is.EqualTo("15"), "Первое устройство должно быть '15'");
        Assert.That(result.DevicesId[1], Is.EqualTo("14"), "Второе устройство должно быть '14'");

        // Проверяем временной слот
        Assert.That(result.TimeSlot, Is.EqualTo(300), "Временной слот должен быть 300 (0x1E * 10)");
    }

    /// <summary>
    /// Тест парсинга команды записи с данными реле и счетчиком.
    /// Команда: :AB10000000070E0008FFFFFFFF01010000FFFFFFFF2E<CR><LF>
    /// </summary>
    [Test]
    public void Parse_WriteCommandWithRelayDataAndCounter_ShouldReturnValidCommand()
    {
        // Arrange
        string input = ":AB10000000070E0008FFFFFFFF01010000FFFFFFFF2E\r\n";

        // Act
        var result = Command.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null, "Команда должна быть успешно распарсена");
        Assert.That(result!.Id, Is.EqualTo("AB"), "ID устройства должен быть AB");
        Assert.That(result.Func, Is.EqualTo(FuncType.Write), "Функциональный код должен быть Write");
        Assert.That(result.StartAddress, Is.EqualTo("0000"), "Стартовый адрес должен быть '0000'");
        Assert.That(result.RegisterQuantity, Is.EqualTo(7), "Количество регистров должно быть 7");
        Assert.That(result.IsGroup, Is.False, "Команда не должна быть групповой");
        Assert.That(result.DataBytesQuantity, Is.EqualTo(14), "Количество байт данных должно быть 14 (0x0E)");
        Assert.That(result.CounterValue, Is.EqualTo(8), "Значение счетчика должно быть 8");

        // Проверяем данные реле
        Assert.That(result.RelaysData, Is.Not.Null, "Данные реле не должны быть null");
        Assert.That(result.RelaysData, Has.Length.EqualTo(3), "Должно быть 3 элемента данных реле");

        // Проверяем первое реле (FFFFFFFF - неактивное)
        var relay0 = result.RelaysData[0];
        Assert.That(relay0.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус первого реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay0.Delay, Is.EqualTo(0), "Задержка первого реле должна быть 0");
        Assert.That(relay0.Duration, Is.EqualTo(0), "Длительность первого реле должна быть 0");

        // Проверяем второе реле (01010000)
        var relay1 = result.RelaysData[1];
        Assert.That(relay1.RelayStatus, Is.EqualTo(RelayStatus.ON), "Статус второго реле должен быть ON (1)");
        Assert.That(relay1.Delay, Is.EqualTo(1), "Задержка второго реле должна быть 1");
        Assert.That(relay1.Duration, Is.EqualTo(0), "Длительность второго реле должна быть 0");

        // Проверяем третье реле (FFFFFFFF - неактивное)
        var relay2 = result.RelaysData[2];
        Assert.That(relay2.RelayStatus, Is.EqualTo(RelayStatus.OFF), "Статус третьего реле должен быть OFF (по умолчанию для FFFFFF)");
        Assert.That(relay2.Delay, Is.EqualTo(0), "Задержка третьего реле должна быть 0");
        Assert.That(relay2.Duration, Is.EqualTo(0), "Длительность третьего реле должна быть 0");
    }
}
