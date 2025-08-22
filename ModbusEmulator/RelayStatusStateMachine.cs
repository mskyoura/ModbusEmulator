using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusEmulator
{
    public class RelayStatusStateMachine(RelayStatus[]? previousStatuses = null)
    {
        private RelayStatus[] _previousStatuses = (previousStatuses != null && previousStatuses.Length == 3) ?
            previousStatuses : [RelayStatus.OFF, RelayStatus.OFF, RelayStatus.OFF];
        private readonly Dictionary<int, CancellationTokenSource> _activeTimers = new();

        public RelayStatus[] Get(RelayStatus[]? relayStatuses = null, RelayData[]? relayData = null)
        {
            if (relayStatuses == null)
            {
                return _previousStatuses;
            }
            if (relayStatuses?.Length != 3)
                throw new ArgumentException("relayStatuses must be array of 3 elements");

            var result = new RelayStatus[3];
            Array.Copy(_previousStatuses, result, 3);

            var firstRelayOnIndex = Array.LastIndexOf(relayStatuses, RelayStatus.ON);
            var firstRelayOnIndexLastTime = Array.LastIndexOf(_previousStatuses, RelayStatus.ON);

            switch (firstRelayOnIndex)
            {
                case 2:
                    throw new ArgumentException("Cannot control third relay");
                case 1 when firstRelayOnIndexLastTime == -1:
                    throw new ArgumentException("Cannot turn on second relay without first");
                case >= 0 when firstRelayOnIndex > firstRelayOnIndexLastTime:
                    if (relayData != null && relayData.Length > firstRelayOnIndex && relayData[firstRelayOnIndex].Delay.HasValue && relayData[firstRelayOnIndex].Delay.Value > 0)
                    {
                        ScheduleRelayOn(firstRelayOnIndex, relayData[firstRelayOnIndex]);
                    }
                    else
                    {
                        result[firstRelayOnIndex] = RelayStatus.ON;
                        if (relayData != null && relayData.Length > firstRelayOnIndex)
                        {
                            ScheduleRelayOff(firstRelayOnIndex, relayData[firstRelayOnIndex]);
                        }
                    }
                    break;
                case var index when index < firstRelayOnIndexLastTime:
                    for (int i = index >=0 ? index : 0; i <= firstRelayOnIndexLastTime; i++)
                    {
                        result[i] = RelayStatus.OFF;
                    }
                    CancelRelayTimer(index);
                    break;
            }
            Array.Copy(result, _previousStatuses, result.Length);
            return result;
        }

        private void ScheduleRelayOff(int relayIndex, RelayData relayData)
        {
            CancelRelayTimer(relayIndex);

            if (!relayData.Duration.HasValue || relayData.Duration.Value <= 0) return;

            var cts = new CancellationTokenSource();
            _activeTimers[relayIndex] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay((int)(relayData.Duration.Value * 1000), cts.Token);
                    
                    if (!cts.Token.IsCancellationRequested)
                    {
                        _previousStatuses[relayIndex] = RelayStatus.OFF;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    _activeTimers.Remove(relayIndex);
                }
            }, cts.Token);
        }

        private void ScheduleRelayOn(int relayIndex, RelayData relayData)
        {
            CancelRelayTimer(relayIndex);

            var cts = new CancellationTokenSource();
            _activeTimers[relayIndex] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay((int)(relayData.Delay.Value * 1000), cts.Token);
                    
                    if (!cts.Token.IsCancellationRequested)
                    {
                        _previousStatuses[relayIndex] = RelayStatus.ON;
                        
                        if (relayData.Duration.HasValue && relayData.Duration.Value > 0)
                        {
                            ScheduleRelayOff(relayIndex, relayData);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    _activeTimers.Remove(relayIndex);
                }
            }, cts.Token);
        }

        private void CancelRelayTimer(int relayIndex)
        {
            if (_activeTimers.TryGetValue(relayIndex, out var cts))
            {
                cts.Cancel();
                _activeTimers.Remove(relayIndex);
            }
        }
    }
}
