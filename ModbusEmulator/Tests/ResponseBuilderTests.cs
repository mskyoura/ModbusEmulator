using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ModbusEmulator.Tests;

[TestFixture]
public class ResponseBuilderTests
{
    [Test]
    public void BuildResponse_GroupCommandWithDeviceList_ShouldReturnCorrectResponsesWithDelays()
    {
        // Arrange
        string input = ":FF100000000D18000001000000FFFFFFFFFFFFFFFF1514000000000000001E8C\r\n";
        var command = Command.Parse(input);
        var responseBuilder = new ResponseBuilder([]); // Для групповых команд используются DevicesId из команды

        // Act
        var responses = responseBuilder.BuildResponse(command!);

        // Assert
        Assert.That(responses, Is.Not.Null);
        Assert.That(responses, Has.Length.EqualTo(2));

        // Проверяем первый ответ (устройство 14)
        var firstResponse = responses[0];
        Assert.That(firstResponse.Id, Is.EqualTo("14"));
        Assert.That(firstResponse.Func, Is.EqualTo(FuncType.Write));
        Assert.That(firstResponse.PreSendDelay, Is.EqualTo(0)); // Первое устройство без задержки

        // Проверяем второй ответ (устройство 15)
        var secondResponse = responses[1];
        Assert.That(secondResponse.Id, Is.EqualTo("15"));
        Assert.That(secondResponse.Func, Is.EqualTo(FuncType.Write));
        Assert.That(secondResponse.PreSendDelay, Is.EqualTo(300)); // 30 * 10 * 1 = 300 (TimeSlot = 30)

        // Проверяем последовательность устройств (сортировка по алфавиту)
        Assert.That(responses[0].Id, Is.EqualTo("14"));
        Assert.That(responses[1].Id, Is.EqualTo("15"));

        // Проверяем точные строковые представления
        var firstResponseString = firstResponse.ToString();
        var secondResponseString = secondResponse.ToString();
        Assert.That(firstResponseString, Is.EqualTo(":14100000000CD0\r\n"));
        Assert.That(secondResponseString, Is.EqualTo(":15100000000CCF\r\n"));
    }

    [Test]
    public void BuildResponse_WriteCommandOldFormat_ShouldReturnCorrectWriteResponse()
    {
        // Arrange
        string input = ":AB10000000070E0008FFFFFFFF01010000FFFFFFFF2E\r\n";
        RelayStatus[] _defaultStatuses = [RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF];
        var command = Command.Parse(input);
        var responseBuilder = new ResponseBuilder(["AB"], _defaultStatuses);

        // Act
        var responses = responseBuilder.BuildResponse(command!);
        // Assert
        Assert.That(responses, Is.Not.Null);
        Assert.That(responses, Has.Length.EqualTo(1));

        var response = responses[0];
        Assert.That(response.Id, Is.EqualTo("AB"));
        Assert.That(response.Func, Is.EqualTo(FuncType.Write));
        Assert.That(response.PreSendDelay, Is.EqualTo(0));

        // Проверяем строковое представление
        var responseString = response.ToString();
        Assert.That(responseString, Is.EqualTo(":AB100000000C39\r\n"));
    }

    [Test]
    public void BuildResponse_GroupCommandWithMultipleDevices_ShouldReturnCorrectResponsesWithDelays()
    {
        // Arrange
        string input = ":FF100000000D180000FFFF000001030000FFFF00000103020000000000000ABC\r\n";
        var command = Command.Parse(input);
        RelayStatus[] _defaultStatuses = [RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF];
        var responseBuilder = new ResponseBuilder(["01","02","03"], _defaultStatuses); // Для групповых команд используются DevicesId из команды

        // Act
        var responses = responseBuilder.BuildResponse(command!);

        // Assert
        Assert.That(responses, Is.Not.Null);
        Assert.That(responses, Has.Length.EqualTo(3));

        // Проверяем последовательность устройств в указанном порядке
        Assert.That(responses[0].Id, Is.EqualTo("01"));
        Assert.That(responses[1].Id, Is.EqualTo("02"));
        Assert.That(responses[2].Id, Is.EqualTo("03"));

        // Проверяем PreSendDelay для каждого устройства
        Assert.That(responses[0].PreSendDelay, Is.EqualTo(0), "Первое устройство должно иметь задержку 0мс");
        Assert.That(responses[1].PreSendDelay, Is.EqualTo(100), "Второе устройство должно иметь задержку 100мс");
        Assert.That(responses[2].PreSendDelay, Is.EqualTo(200), "Третье устройство должно иметь задержку 200мс");

        // Проверяем полные строковые представления ответов
        var firstResponseString = responses[0].ToString();
        var secondResponseString = responses[1].ToString();
        var thirdResponseString = responses[2].ToString();

        Assert.That(firstResponseString, Is.EqualTo(":01100000000CE3\r\n"), "Первый ответ должен соответствовать ожидаемому формату");
        Assert.That(secondResponseString, Is.EqualTo(":02100000000CE2\r\n"), "Второй ответ должен соответствовать ожидаемому формату");
        Assert.That(thirdResponseString, Is.EqualTo(":03100000000CE1\r\n"), "Третий ответ должен соответствовать ожидаемому формату");
    }

    [Test]
    public void BuildResponse_ReadCommandWithoutInitialStatus_ShouldReturnDefaultResponse()
    {
        // Arrange
        string input = ":AB040010000839\r\n";
        var command = Command.Parse(input, 2);
        RelayStatus[] _defautStates = [RelayStatus.ON, RelayStatus.OFF, RelayStatus.ON];
        var responseBuilder = new ResponseBuilder(["AB"], _defautStates); 

        // Act
        var responses = responseBuilder.BuildResponse(command!);

        // Assert
        Assert.That(responses, Is.Not.Null);
        Assert.That(responses, Has.Length.EqualTo(1));

        var response = responses[0];
        Assert.That(response.Id, Is.EqualTo("AB"));
        Assert.That(response.Func, Is.EqualTo(FuncType.Read));
        Assert.That(response.PreSendDelay, Is.EqualTo(0));

        // Проверяем строковое представление
        var responseString = response.ToString();
        Assert.That(responseString, Is.EqualTo(":AB0410000900B100050002000000000000000080\r\n"), "Ответ должен соответствовать ожидаемому формату");
    }

    [Test]
    public void BuildResponse_ReadCommandWithSpecificRelayStatesAndCounter_ShouldReturnCorrectResponse()
    {
        // Arrange
        string input = ":AB040010000839\r\n";
        var command = Command.Parse(input, 5);
        RelayStatus[] _defaultStates = [RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF];
        var responseBuilder = new ResponseBuilder(["AB"], _defaultStates);

        // Act
        var responses = responseBuilder.BuildResponse(command!);

        // Assert
        Assert.That(responses, Is.Not.Null);
        Assert.That(responses, Has.Length.EqualTo(1));

        var response = responses[0];
        Assert.That(response.Id, Is.EqualTo("AB"));
        Assert.That(response.Func, Is.EqualTo(FuncType.Read));
        Assert.That(response.PreSendDelay, Is.EqualTo(0));

        // Проверяем строковое представление
        var responseString = response.ToString();
        Assert.That(responseString, Is.EqualTo(":AB0410000900B100010005000000000000000081\r\n"), "Ответ должен соответствовать ожидаемому формату");
    }

}
