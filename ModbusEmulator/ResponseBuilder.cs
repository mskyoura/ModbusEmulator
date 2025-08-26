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
            return command.Func switch 
            { 
                FuncType.Read => [BuildReadResponse(command)],
                FuncType.Write => BuildWriteResponses(command),
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

        private Response[] BuildWriteResponses(Command command)
        {
            // For group write commands (with specified DeviceIds), send a confirmation Response for each device
            if (command.IsGroup && command.DevicesId.Any())
            {
                var validDeviceIds = command.DevicesId.Intersect(_devicesId).Order().ToArray();
                return validDeviceIds.Select((id, cnt) => BuildWriteResponse(command, cnt, id)).ToArray();
            }
            
            // For individual write commands, send a confirmation Response for that device
            return [BuildWriteResponse(command, 0, command.Id)];
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