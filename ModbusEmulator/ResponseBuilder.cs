using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ModbusEmulator
{
    public class ResponseBuilder(string[] devicesId, RelayStatus[]? defaultRelaysStatus = null)
    {
        
        private readonly string[] _devicesId = devicesId.Order().ToArray();
        private readonly RelayStatus[]? _defaultRelaysStatus = defaultRelaysStatus;
        private readonly RelayStatusStateMachine _relayStatusStateMachine = new(defaultRelaysStatus);

        public Response[] BuildResponse(Command command)
        {
            var validDevicesId = command.DevicesId.Intersect(_devicesId);
            validDevicesId = validDevicesId.Any() && command.IsGroup ? validDevicesId : [command.Id];
            return command.Func switch { 
                FuncType.Read => [BuildReadResponse(command)],
                FuncType.Write => command.IsGroup && command.DevicesId.Any() ? 
                    command.DevicesId.Order().Select((id, cnt) => BuildWriteResponse(command, cnt, id)).ToArray() :
                    validDevicesId.Select((id, _) => BuildWriteResponse(command, 0, id)).ToArray(),
                    _ => []
            };
        }

        private Response BuildReadResponse(Command command)
        {
            var statuses = command.RelaysData.Select(t => t.RelayStatus).ToArray();
            return new Response
            {
                Id = command.Id,
                Func = FuncType.Read,
                CmdNumber = command.CounterValue ?? 0,
                RelaysStatus = _relayStatusStateMachine.Get()
            };
        }

        private Response BuildWriteResponse(Command command, int cnt, string deviceId)
        {
            var statuses = command.RelaysData.Select(t => t.RelayStatus).ToArray();
            _relayStatusStateMachine.Get(statuses, command.RelaysData);
            return new Response
            {
                Id = deviceId,
                Func = FuncType.Write,
                PreSendDelay = (cnt != 0 && command.TimeSlot != null) ? command.TimeSlot.Value * cnt : 0,
                CmdNumber = command.CounterValue ?? 0
            };
        }

    }
}