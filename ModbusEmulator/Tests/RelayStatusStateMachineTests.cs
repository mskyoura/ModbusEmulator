using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModbusEmulator.Tests
{
    [TestFixture]
    public class RelayStatusStateMachineTests
    {
        private RelayStatusStateMachine _stateMachine;

        [SetUp]
        public void Setup()
        {
            _stateMachine = new RelayStatusStateMachine();
        }

        [Test]
        public void Get_InvalidRelayStatusesLength_ThrowsArgumentException()
        {
            // Arrange
            var invalidStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.ON };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _stateMachine.Get(invalidStatuses));
            Assert.That(ex.Message, Is.EqualTo("relayStatuses must be array of 3 elements"));
        }

        [Test]
        public void Get_TryToControlThirdRelay_ThrowsArgumentException()
        {
            // Arrange
            var statuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.ON };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _stateMachine.Get(statuses));
            Assert.That(ex.Message, Is.EqualTo("Cannot control third relay"));
        }

        [Test]
        public void Get_TryToTurnOnSecondRelayWithoutFirst_ThrowsArgumentException()
        {
            // Arrange
            var statuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.ON, RelayStatus.OFF };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _stateMachine.Get(statuses));
            Assert.That(ex.Message, Is.EqualTo("Cannot turn on second relay without first"));
        }

        [Test]
        public void Get_TurnOnFirstRelayWithDelay_ShouldDelayActivation()
        {
            // Arrange
            var initialStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };
            var requestStatuses = new RelayStatus[] { RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF };
            var relayData = new RelayData[]
            {
                new() { RelayStatus = RelayStatus.ON, Delay = 0.5},
                new(),
                new()
            };

            // Act - Initial call should return OFF state
            var result1 = _stateMachine.Get(requestStatuses, relayData);
            Assert.That(result1, Is.EqualTo(initialStatuses));

            // Wait for delay
            Task.Delay(600).Wait();

            // Act - After delay should return ON state
            var result2 = _stateMachine.Get();
            var expectedAfterDelay = new RelayStatus[] { RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF };
            Assert.That(result2, Is.EqualTo(expectedAfterDelay));
        }

        [Test]
        public void Get_TurnOnSecondRelayWithDelayAndDuration_ShouldWorkCorrectly()
        {
            // Arrange
             var initialStatuses = new RelayStatus[] { RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF };
            _stateMachine = new(initialStatuses);
            var requestStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.ON, RelayStatus.OFF };
            var relayData = new RelayData[]
            {
                new(),
                new() { RelayStatus = RelayStatus.ON, Delay = 0.5, Duration = 0.1},
                new()
            };

            // Act - Initial call should return initial state
            var result1 = _stateMachine.Get(requestStatuses, relayData);
            Assert.That(result1, Is.EqualTo(initialStatuses));

            // Wait for delay (500ms)
            Task.Delay(500).Wait();

            // Act - After delay should return both ON
            var result2 = _stateMachine.Get();
            var expectedAfterDelay = new RelayStatus[] { RelayStatus.ON, RelayStatus.ON, RelayStatus.OFF };
            Assert.That(result2, Is.EqualTo(expectedAfterDelay));

            // Wait for duration (100ms)
            Task.Delay(200).Wait();

            // Act - After duration should return second relay OFF
            var result3 = _stateMachine.Get();
            var expectedAfterDuration = new RelayStatus[] { RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF };
            Assert.That(result3, Is.EqualTo(expectedAfterDuration));
        }

        [Test]
        public void Get_TurnOffFirstRelayWhenOn_ShouldTurnOffImmediately()
        {
            // Arrange
            var initialStatuses = new RelayStatus[] { RelayStatus.ON, RelayStatus.OFF, RelayStatus.OFF};
            _stateMachine = new(initialStatuses);
            var requestStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };
            var relayData = new RelayData[]
            {
                new(),
                new(),
                new()
            };

            // Set initial state
            _stateMachine.Get(requestStatuses, relayData);

            // Act
            var result = _stateMachine.Get();
            var expected = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Get_TurnOffFirstRelayWhenOff_ShouldRemainOff()
        {
            // Arrange
            var initialStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };
            var requestStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };
            var relayData = new RelayData[]
            {
                new(),
                new(),
                new()
            };

            // Act
            var result = _stateMachine.Get(requestStatuses, relayData);

            // Assert
            Assert.That(result, Is.EqualTo(initialStatuses));
        }

        [Test]
        public void Get_TurnOffFirstRelayWhenBothOn_ShouldTurnOffAll()
        {
            // Arrange
            var initialStatuses = new RelayStatus[] { RelayStatus.ON, RelayStatus.ON, RelayStatus.OFF };
            _stateMachine = new(initialStatuses);
            var requestStatuses = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };

            // Set initial state
            _stateMachine.Get(requestStatuses);

            // Act
            var result = _stateMachine.Get();
            var expected = new RelayStatus[] { RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF };

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        
    }
}
