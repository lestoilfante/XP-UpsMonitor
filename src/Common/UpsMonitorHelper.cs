using System;
using System.Collections.Generic;
using System.Xml;
using UpsMonitor.Admin;
using VideoOS.Platform;
using VideoOS.Platform.Admin;

namespace UpsMonitor.Common
{
    public static class UpsMonitorHelper
    {
        public static OperationalState UpsOperationalState(UpsOutputStatusMIB_UPS upsOutputStatus)
        {
            switch (upsOutputStatus)
            {
                case UpsOutputStatusMIB_UPS.Normal:
                    return OperationalState.OkActive;

                case UpsOutputStatusMIB_UPS.Bypass:
                case UpsOutputStatusMIB_UPS.Booster:
                case UpsOutputStatusMIB_UPS.Reducer:
                    return OperationalState.Warning;

                case UpsOutputStatusMIB_UPS.Battery:
                case UpsOutputStatusMIB_UPS.None:
                    return OperationalState.Error;

                // Unknown state
                case UpsOutputStatusMIB_UPS.Other:
                default:
                    return OperationalState.Warning;
            }
        }
        public static OperationalState UpsOperationalState(UpsOutputStatusMIB_POWERNET upsOutputStatus)
        {
            switch (upsOutputStatus)
            {
                // Normal operational states
                case UpsOutputStatusMIB_POWERNET.OnLine:
                case UpsOutputStatusMIB_POWERNET.EcoMode:
                case UpsOutputStatusMIB_POWERNET.HotStandby:
                case UpsOutputStatusMIB_POWERNET.eConversion:
                case UpsOutputStatusMIB_POWERNET.OnBatteryTest:
                case UpsOutputStatusMIB_POWERNET.ActiveLoad:
                case UpsOutputStatusMIB_POWERNET.ChargerSpotMode:
                case UpsOutputStatusMIB_POWERNET.InverterSpotMode:
                case UpsOutputStatusMIB_POWERNET.SpotMode:
                case UpsOutputStatusMIB_POWERNET.PowerSaving:
                case UpsOutputStatusMIB_POWERNET.BatteryDischargeSpotMode:
                    return OperationalState.OkActive;

                // Warning states
                case UpsOutputStatusMIB_POWERNET.OnSmartBoost:
                case UpsOutputStatusMIB_POWERNET.OnSmartTrim:
                case UpsOutputStatusMIB_POWERNET.SwitchedBypass:
                case UpsOutputStatusMIB_POWERNET.StaticBypassStandby:
                case UpsOutputStatusMIB_POWERNET.InverterStandby:
                case UpsOutputStatusMIB_POWERNET.TimedSleeping:
                case UpsOutputStatusMIB_POWERNET.Rebooting:
                case UpsOutputStatusMIB_POWERNET.HardwareFailureBypass:
                case UpsOutputStatusMIB_POWERNET.EmergencyStaticBypass:
                case UpsOutputStatusMIB_POWERNET.SoftwareBypass:
                case UpsOutputStatusMIB_POWERNET.SleepingUntilPowerReturn:
                    return OperationalState.Warning;

                // Error states
                case UpsOutputStatusMIB_POWERNET.OnBattery:
                case UpsOutputStatusMIB_POWERNET.Off:
                    return OperationalState.Error;

                // Unknown state
                case UpsOutputStatusMIB_POWERNET.Unknown:
                default:
                    return OperationalState.Warning;
            }
        }

        private static OperationalState UpsOperationalState(UpsOutputStatusMIB_XUPS upsOutputStatus)
        {
            switch (upsOutputStatus)
            {
                case UpsOutputStatusMIB_XUPS.Normal:
                case UpsOutputStatusMIB_XUPS.HighEfficiencyMode:
                    return OperationalState.OkActive;

                case UpsOutputStatusMIB_XUPS.Bypass:
                case UpsOutputStatusMIB_XUPS.Booster:
                case UpsOutputStatusMIB_XUPS.Reducer:
                case UpsOutputStatusMIB_XUPS.ParallelCapacity:
                case UpsOutputStatusMIB_XUPS.ParallelRedundant:
                    return OperationalState.Warning;

                case UpsOutputStatusMIB_XUPS.Battery:
                case UpsOutputStatusMIB_XUPS.None:
                    return OperationalState.Error;

                // Unknown state
                case UpsOutputStatusMIB_XUPS.Other:
                default:
                    return OperationalState.Warning;
            }
        }

        public static OperationalState GetUpsOperationalState(UpsStatus upsStatus, MibFamily mibFamily)
        {
            switch (mibFamily)
            {
                case MibFamily.POWERNET:
                    return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_POWERNET)upsStatus.OutputSource);
                case MibFamily.UPS:
                    return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_UPS)upsStatus.OutputSource);
                case MibFamily.XUPS:
                    return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_XUPS)upsStatus.OutputSource);
                default:
                    return OperationalState.Error;
            }
        }

        public static OperationalState GetUpsOperationalState(UpsItem upsItem)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(upsItem.FQID.ObjectId, out UpsStatus upsStatus))
            {
                switch (upsItem.MibFamily)
                {
                    case MibFamily.POWERNET:
                        return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_POWERNET)upsStatus.OutputSource);
                    case MibFamily.UPS:
                        return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_UPS)upsStatus.OutputSource);
                    case MibFamily.XUPS:
                        return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_XUPS)upsStatus.OutputSource);
                    default:
                        return OperationalState.Error;
                }
            }
            return OperationalState.Error;
        }
        public static OperationalState GetUpsOperationalState(Item vmsItem)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsItem.FQID.ObjectId, out UpsStatus upsStatus))
            {
                var mibFamily = (vmsItem.Properties.ContainsKey(ConfigPropertyName.MibFamily)) ? (MibFamily)Enum.Parse(typeof(MibFamily), vmsItem.Properties[ConfigPropertyName.MibFamily]) : MibFamily.POWERNET;
                switch (mibFamily)
                {
                    case MibFamily.POWERNET:
                        return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_POWERNET)upsStatus.OutputSource);
                    case MibFamily.UPS:
                        return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_UPS)upsStatus.OutputSource);
                    case MibFamily.XUPS:
                        return UpsMonitorHelper.UpsOperationalState((UpsOutputStatusMIB_XUPS)upsStatus.OutputSource);
                    default:
                        return OperationalState.Warning;
                }
            }
            return OperationalState.Warning;
        }

        public static string GetUpsOutputSource(UpsStatus upsStatus, MibFamily mibFamily)
        {
            switch (mibFamily)
            {
                case MibFamily.POWERNET:
                    return ((UpsOutputStatusMIB_POWERNET)upsStatus.OutputSource).ToString();
                case MibFamily.UPS:
                    return ((UpsOutputStatusMIB_UPS)upsStatus.OutputSource).ToString();
                case MibFamily.XUPS:
                    return ((UpsOutputStatusMIB_XUPS)upsStatus.OutputSource).ToString();
                default:
                    return UpsOutputStatusMIB_POWERNET.Unknown.ToString();
            }
        }

        public static string GetUpsOutputSource(Item vmsItem, MibFamily mibFamily)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsItem.FQID.ObjectId, out UpsStatus upsStatus))
            {
                switch (mibFamily)
                {
                    case MibFamily.POWERNET:
                        return ((UpsOutputStatusMIB_POWERNET)upsStatus.OutputSource).ToString();
                    case MibFamily.UPS:
                        return ((UpsOutputStatusMIB_UPS)upsStatus.OutputSource).ToString();
                    case MibFamily.XUPS:
                        return ((UpsOutputStatusMIB_XUPS)upsStatus.OutputSource).ToString();
                    default:
                        return UpsOutputStatusMIB_POWERNET.Unknown.ToString();
                }
            }
            return UpsOutputStatusMIB_POWERNET.Unknown.ToString();
        }
        public static string GetUpsOutputSource(Item vmsItem)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsItem.FQID.ObjectId, out UpsStatus upsStatus))
            {
                var mibFamily = (vmsItem.Properties.ContainsKey(ConfigPropertyName.MibFamily)) ? (MibFamily)Enum.Parse(typeof(MibFamily), vmsItem.Properties[ConfigPropertyName.MibFamily]) : MibFamily.POWERNET;
                switch (mibFamily)
                {
                    case MibFamily.POWERNET:
                        return ((UpsOutputStatusMIB_POWERNET)upsStatus.OutputSource).ToString();
                    case MibFamily.UPS:
                        return ((UpsOutputStatusMIB_UPS)upsStatus.OutputSource).ToString();
                    case MibFamily.XUPS:
                        return ((UpsOutputStatusMIB_XUPS)upsStatus.OutputSource).ToString();
                    default:
                        return UpsOutputStatusMIB_POWERNET.Unknown.ToString();
                }
            }
            return UpsOutputStatusMIB_POWERNET.Unknown.ToString();
        }

        public static int GetUpsRuntimeRemaining(Guid vmsObjectId)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsObjectId, out UpsStatus upsStatus))
            {
                return upsStatus.RuntimeRemaining;
            }
            return 0;
        }
        public static int GetUpsRuntimeRemaining(Item vmsItem)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsItem.FQID.ObjectId, out UpsStatus upsStatus))
            {
                return upsStatus.RuntimeRemaining;
            }
            return 0;
        }

        public static string GetUpsSNMPerror(Guid vmsObjectId)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsObjectId, out UpsStatus upsStatus))
            {
                return upsStatus.PollError;
            }
            return String.Empty;
        }
        public static string GetUpsSNMPerror(Item vmsItem)
        {
            if (UpsMonitorDefinition.LastPollResults.TryGetValue(vmsItem.FQID.ObjectId, out UpsStatus upsStatus))
            {
                return upsStatus.PollError;
            }
            return String.Empty;
        }

        public static List<KeyValuePair<MibFamily, string>> MibFamilyList = new List<KeyValuePair<MibFamily, string>>
        {
            new KeyValuePair<MibFamily, string>(MibFamily.POWERNET, "PowerNet-MIB (APC)"),
            new KeyValuePair<MibFamily, string>(MibFamily.UPS, "UPS-MIB (Standard RFC 1628)"),
            new KeyValuePair<MibFamily, string>(MibFamily.XUPS, "XUPS-MIB (Eaton/Powerware)")
        };

        internal static int LoadPollInterval()
        {
            XmlNode result = VideoOS.Platform.Configuration.Instance.GetOptionsConfiguration(UpsMonitor.PropertyPollIntervalId, false);
            if (result != null)
            {
                return (Int32.TryParse(result.InnerText, out int val) && val > 0) ? val : UpsMonitor.DefaultPollInterval;
            }
            return UpsMonitor.DefaultPollInterval;
        }
        internal static void SavePollInterval(int seconds)
        {
            VideoOS.Platform.Configuration.Instance.SaveOptionsConfiguration(UpsMonitor.PropertyPollIntervalId, false, ToXml("PollInterval", seconds.ToString()));
        }
        internal static XmlElement ToXml(string key, string value)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("root");
            doc.AppendChild(root);
            XmlElement child = doc.CreateElement(key);
            child.InnerText = value;
            root.AppendChild(child);
            return root;
        }
    }
}
