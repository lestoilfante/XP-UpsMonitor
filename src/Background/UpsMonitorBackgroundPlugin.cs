using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UpsMonitor.Admin;
using UpsMonitor.Common;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Background;
using VideoOS.Platform.Data;
using VideoOS.Platform.Messaging;

namespace UpsMonitor.Background
{
    public class UpsMonitorBackgroundPlugin : BackgroundPlugin
    {
        private Thread _thread;
        private volatile bool _running = false;
        private ConcurrentDictionary<Guid, UpsItem> _devicesConfigs;
        private readonly int _pollTimeoutMs = Common.UpsMonitor.DefaultPollTimeout * 1000;
        private ManualResetEvent _stopEvent;
        private readonly object _devicesConfigsLock = new object();
        private volatile bool _devicesConfigsNeedReload = false;
        private readonly int _maxParallelism = 10;

        private object _msgPluginConfigChanged;

        /// <summary>
        /// Gets the unique id identifying this plugin component
        /// </summary>
        public override Guid Id
        {
            get { return Common.UpsMonitor.BackgroundPluginId; }
        }

        /// <summary>
        /// The name of this background plugin
        /// </summary>
        public override String Name
        {
            get { return Common.UpsMonitor.PluginName; }
        }

        /// <summary>
        /// Called by the Environment when the user has logged in.
        /// </summary>
        public override void Init()
        {
            if (_thread != null && _thread.IsAlive)
            {
                EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, "Plugin is already running", null);
                return;
            }

            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, "Plugin init", null);

            _stopEvent = new ManualResetEvent(false);

            // Events subscription
            _msgPluginConfigChanged = EnvironmentManager.Instance.RegisterReceiver(EvtConfigChanged, new MessageIdAndRelatedKindFilter(VideoOS.Platform.Messaging.MessageId.Server.ConfigurationChangedIndication, Common.UpsMonitor.CtrlKindId));

            _devicesConfigs = new ConcurrentDictionary<Guid, UpsItem>();
            _stopEvent.Reset();
            _running = true;
            _thread = new Thread(() => Run(_pollTimeoutMs))
            {
                IsBackground = true
            };
            _thread.Start();
        }

        private object EvtConfigChanged(Message message, FQID dest, FQID sender)
        {
            _devicesConfigsNeedReload = true;
            return null;
        }

        /// <summary>
        /// Called by the Environment when the user log's out.
        /// You should close all remote sessions and flush cache information, as the
        /// user might logon to another server next time.
        /// </summary>
        public override void Close()
        {
            EnvironmentManager.Instance.UnRegisterReceiver(_msgPluginConfigChanged);
            _msgPluginConfigChanged = null;

            if (!_running || _thread == null)
            {
                EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, "Plugin is already stopped", null);
                return;
            }

            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, "Plugin closing", null);
            _running = false;
            _stopEvent.Set(); // Signal the waiting thread to stop immediately

            // Wait for the thread to exit with a short timeout
            if (!_thread.Join(1000))
            {
                EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, "Poller thread did not exit gracefully, interrupting...", null);
                try
                {
                    _thread.Interrupt();
                    _thread.Join(1000);
                }
                catch (ThreadStateException)
                {
                    // Thread might have exited already
                }
            }
            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, "Saving last known status", null);
            // Save last known status on Milestone DB
            SaveUpsLastValues();
            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, "Save completed", null);
            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, "Plugin stopped", null);
        }

        /// <summary>
        /// Define in what Environments the current background task should be started.
        /// </summary>
        public override List<EnvironmentType> TargetEnvironments
        {
            get { return new List<EnvironmentType>() { EnvironmentType.Service }; } // Default will run in the Event Server
        }


        /// <summary>
        /// The thread doing the work
        /// </summary>
        private void Run(int timeout)
        {
            try
            {
                Thread.Sleep(30000); // Wait a bit before starting the first poll, configChanged event may not have been fired yet
                int pollIntval = UpsMonitorHelper.LoadPollInterval() * 1000;
                EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS poll interval set to {pollIntval / 1000} seconds");
                if (!SiteLicenseHandler.IsValidLicense)
                    EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, SiteLicenseHandler.KindLycenseMe());
                while (_running)
                {
                    try
                    {
                        // Get update list of UPS devices to poll on each iteration
                        var upsUnitsActiveOnDb = GetUpsItemsConfig();
                        // Perform the SNMP poll
                        var results = PollAllDevicesAsync(upsUnitsActiveOnDb, timeout).GetAwaiter().GetResult();

                        // Process results
                        foreach (var currentResult in results)
                        {
                            // Compute the operational state based on the result
                            currentResult.Value.OperationalState = UpsMonitorHelper.GetUpsOperationalState(currentResult.Value, upsUnitsActiveOnDb[currentResult.Key].MibFamily);

                            // Check if we have a previous result to compare with
                            if (UpsMonitorDefinition.LastPollResults.TryGetValue(currentResult.Key, out UpsStatus oldResult))
                            {
                                // Store updated result in memory
                                UpsMonitorDefinition.LastPollResults[currentResult.Key] = currentResult.Value;

                                if (oldResult.OperationalState != currentResult.Value.OperationalState || oldResult.PollError != currentResult.Value.PollError)
                                {
                                    SendEvent(currentResult.Key, currentResult.Value, oldResult);
                                    EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS [{currentResult.Key}] changed from {oldResult.OutputSource} to {currentResult.Value.OutputSource}", null);
                                }
                                else
                                {
                                    //EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS [{currentResult.Key}] without updates {currentResult.Value.OutputSource} to {currentResult.Value.OutputSource}", null);
                                }
                            }
                            // Check against the last known value stored in the DB
                            else
                            {
                                UpsMonitorDefinition.LastPollResults[currentResult.Key] = currentResult.Value;
                                if (upsUnitsActiveOnDb.TryGetValue(currentResult.Key, out UpsItem storedDevice))
                                {
                                    if (oldResult != null && storedDevice.LastStatus.OperationalState != currentResult.Value.OperationalState)
                                    {
                                        SendEvent(currentResult.Key, currentResult.Value, oldResult);
                                        EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS [{currentResult.Key}] value changed since last known value {currentResult.Value.OutputSource}", null);
                                    }
                                    else
                                    {
                                        //EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS [{currentResult.Key}] without updates since last known value {currentResult.Value.OutputSource}", null);
                                    }
                                }
                                else
                                {
                                    EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS [{currentResult.Key}] 1st value {currentResult.Value.OutputSource}", null);
                                }
                            }
                        }
                        // Wait for the next poll interval OR until stop is signaled
                        // This allows immediate exit rather than waiting for the full interval
                        if (_stopEvent.WaitOne(pollIntval))
                        {
                            // Stop was signaled
                            break;
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        // Thread was interrupted, exit the loop
                        EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, "Thread was interrupted, exit the loop", null);
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue polling
                        EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, $"Error during polling: {ex.Message}", null);
                        // Still wait before retrying
                        if (_stopEvent.WaitOne(pollIntval))
                        {
                            // Stop was signaled
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, $"Fatal error in polling thread: {ex.Message}", null);
            }
            finally
            {

            }
        }

        //private async Task<Dictionary<Guid, UpsStatus>> PollAllDevicesAsync(ConcurrentDictionary<Guid, UpsItem> devices, int timeout)
        //{
        //    var results = new Dictionary<Guid, UpsStatus>();

        //    // Use a semaphore to limit the number of parallel operations
        //    using (var semaphore = new SemaphoreSlim(_maxParallelism))
        //    {
        //        var tasks = new List<Task>();

        //        foreach (var device in devices)
        //        {
        //            // Wait until we can enter the semaphore
        //            await semaphore.WaitAsync();

        //            // Start a new task for this device
        //            tasks.Add(Task.Run(async () =>
        //            {
        //                var status = new UpsStatus();
        //                try
        //                {
        //                    status = await PollSnmpUpsAsync(device.Value.Host, device.Value.Community, device.Value.OidStatus, device.Value.OidRuntimeRemaining, timeout);
        //                }
        //                catch (Exception ex)
        //                {
        //                    status.PollError = ex.Message;
        //                }
        //                finally
        //                {
        //                    lock (results)
        //                    {
        //                        results[device.Value.FQID.ObjectId] = status;
        //                    }
        //                    // Release the semaphore when done
        //                    semaphore.Release();
        //                }
        //            }));
        //        }

        //        // Wait for all tasks to complete
        //        await Task.WhenAll(tasks);
        //    }

        //    return results;
        //}
        private async Task<ConcurrentDictionary<Guid, UpsStatus>> PollAllDevicesAsync(ConcurrentDictionary<Guid, UpsItem> devices, int timeout)
        {
            var results = new ConcurrentDictionary<Guid, UpsStatus>();
            using (var semaphore = new SemaphoreSlim(_maxParallelism))
            {
                var tasks = devices.Select(async device =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var status = await PollSnmpUpsAsync(device.Value.Host, device.Value.Community, device.Value.OidStatus, device.Value.OidRuntimeRemaining, timeout);
                        results.TryAdd(device.Key, status);
                    }
                    catch (Exception ex)
                    {
                        results.TryAdd(device.Key, new UpsStatus { PollError = ex.Message });
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                await Task.WhenAll(tasks);
            }

            return results;
        }


        private async Task<UpsStatus> PollSnmpUpsAsync(string host, string community, string oidStatus, string oidRuntimeRemaining, int timeout)
        {
            var status = new UpsStatus();
            try
            {
                // Create IP endpoint for target SNMP agent
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(host), 161);

                // Create a variable binding list for the OID we want to query
                List<Variable> variables = new List<Variable>
                {
                    new Variable(new ObjectIdentifier(oidStatus)),
                    new Variable(new ObjectIdentifier(oidRuntimeRemaining))
                };

                // Send the SNMP get request
                IList<Variable> result = await Task.Run(() => Messenger.Get(
                    VersionCode.V2,
                    endpoint,
                    new OctetString(community),
                    variables,
                    timeout
                ));

                // Process the result
                if (result.Count > 0)
                {
                    status.OutputSource = Convert.ToInt32(result[0].Data.ToString());
                    status.RuntimeRemaining = Convert.ToInt32(result[1].Data.ToString());
                }
                else
                {
                    throw new Exception("No data received");
                }
            }
            catch (Exception ex)
            {
                status.PollError = ex.Message;
            }
            return status;
        }

        private List<UpsItem> LoadUpsDevicesConfig()
        {
            var config = Configuration.Instance.GetItemConfigurations(Common.UpsMonitor.PluginId, null, Common.UpsMonitor.CtrlKindId);
            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"Loading updated configuration. UPS found: {config.Count}", null);
            var upsItems = new List<UpsItem>();
            foreach (Item item in config)
            {
                UpsItem upsItem = new UpsItem(item);
                upsItems.Add(upsItem);
            }
            return upsItems;
        }
        private ConcurrentDictionary<Guid, UpsItem> GetUpsItemsConfig()
        {
            lock (_devicesConfigsLock)
            {
                if (!_devicesConfigsNeedReload)
                {
                    return _devicesConfigs;
                }
            }
            var devicesList = new ConcurrentDictionary<Guid, UpsItem>();
            foreach (UpsItem device in LoadUpsDevicesConfig())
            {
                if (device.Enabled && device.Host != String.Empty)
                {
                    EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"Adding \"{device.Name}\"", null);
                    devicesList[device.FQID.ObjectId] = device;
                }
            }
            EnvironmentManager.Instance.Log(false, Common.UpsMonitor.PluginName, $"UPS enabled: {devicesList.Count}", null);
            lock (_devicesConfigsLock)
            {
                _devicesConfigs = devicesList;
                _devicesConfigsNeedReload = false;
                return _devicesConfigs;
            }
        }

        private UpsItem GetUpsItemConfig(Guid upsId)
        {
            UpsItem upsItem;
            lock (_devicesConfigsLock)
            {
                _devicesConfigs.TryGetValue(upsId, out upsItem);
            }
            return upsItem;
        }

        private void SaveUpsLastValues()
        {
            foreach (var upsLastResult in UpsMonitorDefinition.LastPollResults)
            {
                var item = Configuration.Instance.GetItemConfiguration(Common.UpsMonitor.PluginId, Common.UpsMonitor.CtrlKindId, upsLastResult.Key);
                if (item != null)
                {
                    item.Properties[ConfigPropertyName.PropertyNameUpsLastOutputSource] = upsLastResult.Value.OutputSource.ToString();
                    item.Properties[ConfigPropertyName.PropertyNameUpsLastRuntimeRemaining] = upsLastResult.Value.RuntimeRemaining.ToString();
                    Configuration.Instance.SaveItemConfiguration(Common.UpsMonitor.PluginId, item);
                }
            }
        }

        private void SendEvent(Guid upsId, UpsStatus statusNew, UpsStatus statusOld)
        {
            UpsItem upsItem = GetUpsItemConfig(upsId);
            EventHeader eventHeader = CreateBaseEvent(upsItem);
            EventHeader eventHeaderAdditional = null;

            // Next 2 -> Open and close Error state
            if (statusNew.OperationalState == OperationalState.Error && statusOld.OperationalState == OperationalState.OkActive) // Error is returned only if UPS powersource is OnBattery or None
            {
                eventHeader.Message = Common.UpsMonitor.Event.OnBatteryStart.Message;
                eventHeader.MessageId = Common.UpsMonitor.Event.OnBatteryStart.MessageId;
                eventHeader.CustomTag = $"Estimated minutes remaining: {statusNew.RuntimeRemaining}";
            }
            else if (statusNew.OperationalState == OperationalState.OkActive && statusOld.OperationalState == OperationalState.Error)
            {
                eventHeader.Message = Common.UpsMonitor.Event.OnBatteryEnd.Message;
                eventHeader.MessageId = Common.UpsMonitor.Event.OnBatteryEnd.MessageId;
            }
            // Next 2 -> Open and close Warning state
            else if (statusNew.OperationalState == OperationalState.Warning && statusOld.OperationalState == OperationalState.OkActive) // Warning is returned if UPS poweroutput has some anomaly
            {
                eventHeader.Message = Common.UpsMonitor.Event.OnWarningStart.Message;
                eventHeader.MessageId = Common.UpsMonitor.Event.OnWarningStart.MessageId;
            }
            else if (statusNew.OperationalState == OperationalState.OkActive && statusOld.OperationalState == OperationalState.Warning)
            {
                eventHeader.Message = Common.UpsMonitor.Event.OnWarningEnd.Message;
                eventHeader.MessageId = Common.UpsMonitor.Event.OnWarningEnd.MessageId;
            }
            // Unusual transition from Warning(anomaly or poll issue) to Error(battery), needs to close warning state
            else if (statusNew.OperationalState == OperationalState.Error && statusOld.OperationalState == OperationalState.Warning)
            {
                eventHeader.Message = Common.UpsMonitor.Event.OnBatteryStart.Message;
                eventHeader.MessageId = Common.UpsMonitor.Event.OnBatteryStart.MessageId;
                eventHeader.CustomTag = $"Estimated minutes remaining: {statusNew.RuntimeRemaining}";

                eventHeaderAdditional = CreateBaseEvent(upsItem);
                eventHeaderAdditional.Message = Common.UpsMonitor.Event.OnWarningEnd.Message;
                eventHeaderAdditional.MessageId = Common.UpsMonitor.Event.OnWarningEnd.MessageId;
            }
            // Unusual transition from Error(battery) to Warning(anomaly or poll issue), needs to close error state
            else if (statusNew.OperationalState == OperationalState.Warning && statusOld.OperationalState == OperationalState.Error)
            {
                eventHeader.Message = Common.UpsMonitor.Event.OnWarningStart.Message;
                eventHeader.MessageId = Common.UpsMonitor.Event.OnWarningStart.MessageId;

                eventHeaderAdditional = CreateBaseEvent(upsItem);
                eventHeaderAdditional.Message = Common.UpsMonitor.Event.OnBatteryEnd.Message;
                eventHeaderAdditional.MessageId = Common.UpsMonitor.Event.OnBatteryEnd.MessageId;
            }
            // WTF?!?
            else
            {
                EnvironmentManager.Instance.Log(true, Common.UpsMonitor.PluginName, $"State OLD is {statusOld.OperationalState} NEW is {statusNew.OperationalState}", null);
                return;
            }

            // Send event
            AnalyticsEvent eventData = new AnalyticsEvent
            {
                EventHeader = eventHeader
            };
            EnvironmentManager.Instance.SendMessage(new VideoOS.Platform.Messaging.Message(MessageId.Server.NewEventCommand) { Data = eventData });

            // Send additional event if needed
            if (eventHeaderAdditional != null)
            {
                AnalyticsEvent eventDataAdditional = new AnalyticsEvent
                {
                    EventHeader = eventHeaderAdditional
                };
                EnvironmentManager.Instance.SendMessage(new VideoOS.Platform.Messaging.Message(MessageId.Server.NewEventCommand) { Data = eventDataAdditional });
            }


            EventServerControl.Instance.ItemStatusChanged(upsItem);
        }

        private EventHeader CreateBaseEvent(UpsItem upsItem)
        {
            return new EventHeader()
            {
                ID = Guid.NewGuid(),
                Class = "Operational",
                Type = "Power",
                Timestamp = DateTime.Now,
                Name = upsItem.Name,
                Source = new EventSource { FQID = upsItem.FQID, Name = upsItem.Name },
                //IMPRO Location = item.Properties[Location]
            };
        }
    }
}
