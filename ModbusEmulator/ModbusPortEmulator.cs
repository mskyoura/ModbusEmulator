using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusEmulator
{
    public class ModbusPortEmulator : BackgroundService
    {
        private SerialPort? _serialPort;
        private readonly string[] _deviceAddresses;
        private readonly ILogger<ModbusPortEmulator> _logger;
        private readonly EmulatorOptions _options;
        private readonly Dictionary<int, RelayData> _relayStates = new Dictionary<int, RelayData>();
        private ResponseBuilder _responseBuilder;
        private readonly StringBuilder _buffer = new StringBuilder();
        private DateTime _lastResponseTime = DateTime.MinValue;


        public ModbusPortEmulator(ILogger<ModbusPortEmulator> logger, IOptions<EmulatorOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _deviceAddresses = _options.DevicesAddress ?? throw new ArgumentException("DeviceAddresses must be configured");

            // Валидация настроек
            if (string.IsNullOrEmpty(_options.PortName))
                throw new ArgumentException("PortName must be configured");

            if (_deviceAddresses.Length == 0)
                throw new ArgumentException("At least one device address must be configured");

            // Инициализация SerialPort
            _serialPort = new SerialPort(_options.PortName)
            {
                BaudRate = 9600,
                ReadTimeout = _options.PortTimeout,
                WriteTimeout = _options.PortTimeout,
                NewLine = "\r\n"
            };

            // Инициализация состояний реле
            for (int i = 0; i < 3; i++)
            {
                _relayStates[i] = new RelayData
                {
                    RelayStatus = RelayStatus.OFF,
                    Delay = null,
                    Duration = null
                };
            }

            _responseBuilder = new(_deviceAddresses);

            _logger.LogInformation("Emulator initialized at {Timestamp} for devices: {devices}, min response interval: {interval}ms",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), string.Join(", ", _deviceAddresses), _options.MinResponseInterval);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _serialPort?.Open();
                _logger.LogInformation("Emulator started at {Timestamp} on {portName} for devices: {devices}", 
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), _serialPort?.PortName, string.Join(", ", _deviceAddresses));

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_serialPort?.IsOpen == true)
                    {
                        try
                        {
                            var content = _serialPort.ReadLine();
                            _logger.LogDebug("Received raw data from serial port at {Timestamp}: '{Content}'", 
                                DateTime.Now.ToString("HH:mm:ss.fff"), content);
                            var command = Command.Parse(content);
                            if (command != null)
                            {
                                _logger.LogDebug("Command parsed successfully at {Timestamp}: ID={Id}, Func={Func}, StartAddr={StartAddr}, RegCount={RegCount}", 
                                    DateTime.Now.ToString("HH:mm:ss.fff"), command.Id, command.Func, command.StartAddress, command.RegisterQuantity);
                                var responses = _responseBuilder.BuildResponse(command);
                                _logger.LogDebug("Generated {ResponseCount} responses at {Timestamp}", 
                                    responses.Length, DateTime.Now.ToString("HH:mm:ss.fff"));
                                foreach( var response in responses)
                                {
                                    try
                                    {
                                        await Task.Delay(response.PreSendDelay);
                                        
                                        // Проверяем минимальный интервал между отправками
                                        var timeSinceLastResponse = DateTime.Now - _lastResponseTime;
                                        if (timeSinceLastResponse.TotalMilliseconds < _options.MinResponseInterval)
                                        {
                                            var additionalDelay = _options.MinResponseInterval - (int)timeSinceLastResponse.TotalMilliseconds;
                                            _logger.LogDebug("Adding additional delay of {Delay}ms to maintain minimum interval at {Timestamp}", 
                                                additionalDelay, DateTime.Now.ToString("HH:mm:ss.fff"));
                                            await Task.Delay(additionalDelay);
                                        }
                                        
                                        var responseString = response.ToString();
                                        _serialPort?.Write(responseString);
                                        //_serialPort?.DiscardOutBuffer();
                                        _lastResponseTime = DateTime.Now;
                                        _logger.LogDebug("Serial port sent response at {Timestamp}: {Response}",
                                            DateTime.Now.ToString("HH:mm:ss.fff"), responseString);
                                    }
                                    catch (TimeoutException)
                                    {
                                        // Timeout is expected when no data is available, just continue the loop
                                        _logger.LogDebug("Serial port timeout - write timeout");
                                    }
                                    catch (InvalidOperationException ex)
                                    {
                                        // Handle serial port not open or other serial port issues
                                        _logger.LogWarning(ex, "Serial port operation failed, continuing...");
                                    }
                                    catch (Exception ex)
                                    {
                                        // Log other unexpected errors but continue the loop
                                        _logger.LogError(ex, "Unexpected error writing to serial port, continuing...");
                                    }

                                }
                            }
                            else
                            {
                                _logger.LogWarning("Failed to parse command at {Timestamp}: '{Content}'", 
                                    DateTime.Now.ToString("HH:mm:ss.fff"), content);
                            }
                            //if (!String.IsNullOrEmpty(content))
                            //{
                            //    foreach (var str in content.Split("\r\n"))
                            //    {
                            //        if (!string.IsNullOrEmpty(str))
                            //        {
                            //            _logger.LogDebug("Received command at {Timestamp}: {Command}", 
                            //                DateTime.Now.ToString("HH:mm:ss.fff"), str);
                            //        }
                                    
                            //    }
                            //}  
                        }
                        catch (TimeoutException)
                        {
                            // Timeout is expected when no data is available, just continue the loop
                            _logger.LogDebug("Serial port timeout - no data available");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Handle serial port not open or other serial port issues
                            _logger.LogWarning(ex, "Serial port operation failed, continuing...");
                        }
                        catch (Exception ex)
                        {
                            // Log other unexpected errors but continue the loop
                            _logger.LogError(ex, "Unexpected error reading from serial port, continuing...");
                        }
                    }
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Emulator stopped by cancellation at {Timestamp}", 
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in emulator execution");
            }
            finally
            {
                _serialPort?.Close();
                _logger.LogInformation("Emulator stopped at {Timestamp} on {portName}", 
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), _serialPort?.PortName);
            }
        }


    }
}