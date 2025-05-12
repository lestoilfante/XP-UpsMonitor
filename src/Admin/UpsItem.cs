using System;
using UpsMonitor.Common;
using VideoOS.Platform;

namespace UpsMonitor.Admin
{
    /// <summary>
    /// UpsItem is a class that extends the Item class and adds properties specific to the UPS Monitor plugin.
    /// </summary>
    public class UpsItem : Item
    {
        private string _host;
        private string _community;
        private MibFamily _mibFamily = MibFamily.UPS;
        private UpsStatus _lastStatus = new UpsStatus();

        public string Host
        {
            get => _host;
            set
            {
                base.Properties[ConfigPropertyName.IPAddress] = value.ToString();
                _host = value;
            }
        }
        public string Community
        {
            get => _community;
            set
            {
                base.Properties[ConfigPropertyName.SNMPCommunity] = value.ToString();
                _community = value;
            }
        }
        public MibFamily MibFamily
        {
            get => _mibFamily;
            set
            {
                base.Properties[ConfigPropertyName.MibFamily] = ((int)value).ToString();
                _mibFamily = value;
            }
        }
        public string OidStatus
        {
            get
            {
                switch (MibFamily)
                {
                    case MibFamily.POWERNET:
                        return ".1.3.6.1.4.1.318.1.1.1.4.1.1.0";
                    case MibFamily.UPS:
                        return ".1.3.6.1.2.1.33.1.4.1.0";
                    case MibFamily.XUPS:
                        return ".1.3.6.1.2.1.33.1.4.1.0";
                    default:
                        return ".1.3.6.1.4.1.318.1.1.1.4.1.1.0";
                }
            }
        }
        public string OidRuntimeRemaining { get; } = "1.3.6.1.2.1.33.1.2.3.0";

        public UpsStatus LastStatus
        {
            get => _lastStatus;
            set
            {
                _lastStatus = value;
            }
        }
        public UpsItem(Item item)
            : base(item.FQID, item.Name)
        {
            foreach (string key in item.Properties.Keys)
            {
                switch (key)
                {
                    case ConfigPropertyName.IPAddress:
                        Host = (!String.IsNullOrEmpty(item.Properties[key])) ? item.Properties[key] : "";
                        break;
                    case ConfigPropertyName.SNMPCommunity:
                        Community = (!String.IsNullOrEmpty(item.Properties[key])) ? item.Properties[key] : "public";
                        break;
                    case ConfigPropertyName.MibFamily:
                        MibFamily = (MibFamily.TryParse(item.Properties[key], out MibFamily mibFamily)) ? mibFamily : MibFamily.UPS;
                        break;
                    case ConfigPropertyName.PropertyNameUpsLastOutputSource:
                        LastStatus.OutputSource = (Int32.TryParse(item.Properties[key], out int val1)) ? val1 : (int)UpsOutputStatusMIB_POWERNET.Unknown;
                        base.Properties[ConfigPropertyName.PropertyNameUpsLastOutputSource] = LastStatus.OutputSource.ToString();
                        break;
                    case ConfigPropertyName.PropertyNameUpsLastRuntimeRemaining:
                        LastStatus.RuntimeRemaining = (Int32.TryParse(item.Properties[key], out int val2)) ? val2 : 0;
                        base.Properties[ConfigPropertyName.PropertyNameUpsLastRuntimeRemaining] = LastStatus.RuntimeRemaining.ToString();
                        break;
                    default:
                        base.Properties[key] = item.Properties[key];
                        break;
                }
            }
            LastStatus.OperationalState = UpsMonitorHelper.GetUpsOperationalState(LastStatus, MibFamily);
        }

        public override Guid MapIconKey
        {
            get
            {
                var state = UpsMonitorHelper.GetUpsOperationalState(LastStatus, MibFamily);
                switch (state)
                {
                    case VideoOS.Platform.Admin.OperationalState.OkActive:
                    case VideoOS.Platform.Admin.OperationalState.Warning: // Avoid changing map icon on warning
                        return Common.UpsMonitor.EventIcon.Ok.Key;
                    case VideoOS.Platform.Admin.OperationalState.Error:
                        return Common.UpsMonitor.EventIcon.Error.Key;
                    default:
                        return Common.UpsMonitor.EventIcon.Ok.Key;
                }
            }
        }
    }
}
