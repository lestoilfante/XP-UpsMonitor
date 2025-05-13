using System;
using System.Collections.Generic;
using VideoOS.Platform.Admin;

namespace UpsMonitor.Common
{
    public static class UpsMonitor
    {
        public const string PluginName = "UPS Monitor";
        public const string ManufacturerName = "https://github.com/lestoilfante";
        public static Guid PluginId = new Guid("2246c909-3967-4bfa-a7c9-29131bed1498");
        public static Guid BackgroundPluginId = new Guid("7f751baf-71f7-4805-850c-5b517b611b3e");
        public static Guid CtrlKindId = new Guid("7d73286c-f0c0-4528-8e7a-b5a0cc119d17");
        public static Guid PropertyPollIntervalId = new Guid("2c31af59-7f22-4a03-81db-7478fe7f44f5");
        public const string LicenseString = "Base-License";
        public const string LicenseFreeString = "Free-License";
        public const int DefaultPollInterval = 60;
        public const int DefaultPollTimeout = 5;
        public const int MinPollInterval = 30;
        public const int MaxPollInterval = 3600;

        public static class Event // Definitions for Event Server and Alarm handling
        {
            public static Guid GroupId = new Guid("1211a1a2-2fb0-45e9-8a04-df4e1aea1fea");
            public static Guid StateGroupIdWarning = new Guid("5b993b8d-47f4-4f00-a567-35bf6a5f797f");
            public static Guid StateGroupIdOutputSource = new Guid("80e4a221-1b97-44b9-a343-fb7cde850638");

            public static UpsMonitorEventDefinition OnBatteryStart = new UpsMonitorEventDefinition(
                new Guid("a9348b16-677f-4092-a9df-df7d98bcc01b"),
                StateGroupIdOutputSource,
                "Running on battery");
            public static UpsMonitorEventDefinition OnBatteryEnd = new UpsMonitorEventDefinition(
                new Guid("f3f4ab43-2ec0-4c3e-9ab5-4bc902dd8107"),
                StateGroupIdOutputSource,
                "Running normally");
            public static UpsMonitorEventDefinition OnWarningStart = new UpsMonitorEventDefinition(
                new Guid("8d78c108-79a2-43c9-b4fe-bfb7f04e3651"),
                StateGroupIdWarning,
                "Anomaly detected");
            public static UpsMonitorEventDefinition OnWarningEnd = new UpsMonitorEventDefinition(
                new Guid("a356fab2-5800-4c8d-ab1e-bfe7b100eaff"),
                StateGroupIdWarning,
                "Anomaly resolved");
        }
        public static class EventIcon
        {
            public static KeyValuePair<Guid, System.Drawing.Icon> Ok = new KeyValuePair<Guid, System.Drawing.Icon>(new Guid("0343de71-9ce2-41c7-80f3-cc24600eb328"), Properties.Resources.UpsIcon);
            public static KeyValuePair<Guid, System.Drawing.Icon> Error = new KeyValuePair<Guid, System.Drawing.Icon>(new Guid("fe20ee41-fa8d-489e-86b0-ed3f3a5aa274"), Properties.Resources.UpsIconError);
        }
    }

    public class UpsMonitorEventDefinition
    {
        public readonly Guid MessageId;
        public readonly string Message;
        public readonly Guid StateGroupId;

        public UpsMonitorEventDefinition(Guid messageId, Guid groupId, string message)
        {
            MessageId = messageId;
            StateGroupId = groupId;
            Message = message;
        }
    }

    public static class ConfigPropertyName
    {
        public const string IPAddress = "IPAddress";
        public const string SNMPCommunity = "SNMPCommunity";
        public const string MibFamily = "MibFamily";
        public const string PropertyNameUpsLastOutputSource = "_LastOutputSource";
        public const string PropertyNameUpsLastRuntimeRemaining = "_LastRuntimeRemaining";
    }

    public class UpsStatus
    {
        public int OutputSource { get; set; } = 1; //Other or Unknown depending on MIB used
        public int RuntimeRemaining { get; set; } = 0;
        public string PollError { get; set; } = "-";
        public OperationalState OperationalState { get; set; } = OperationalState.Ok;
        public override string ToString()
        {
            return $"OutputSource: {OutputSource}, RuntimeRemaining: {RuntimeRemaining}";
        }
    }

    public enum MibFamily
    {
        POWERNET, UPS, XUPS
    }

    public enum UpsOutputStatusMIB_POWERNET //1.3.6.1.4.1.318.1.1.1.4.1.1.0
    {
        Unknown = 1,
        OnLine = 2,
        OnBattery = 3,
        OnSmartBoost = 4,
        TimedSleeping = 5,
        SoftwareBypass = 6,
        Off = 7,
        Rebooting = 8,
        SwitchedBypass = 9,
        HardwareFailureBypass = 10,
        SleepingUntilPowerReturn = 11,
        OnSmartTrim = 12,
        EcoMode = 13,
        HotStandby = 14,
        OnBatteryTest = 15,
        EmergencyStaticBypass = 16,
        StaticBypassStandby = 17,
        PowerSaving = 18,
        SpotMode = 19,
        eConversion = 20,
        ChargerSpotMode = 21,
        InverterSpotMode = 22,
        ActiveLoad = 23,
        BatteryDischargeSpotMode = 24,
        InverterStandby = 25
    }

    public enum UpsOutputStatusMIB_UPS //1.3.6.1.2.1.33.1.4.1.0
    {
        Other = 1,
        None = 2,
        Normal = 3,
        Bypass = 4,
        Battery = 5,
        Booster = 6,
        Reducer = 7,
    }
    public enum UpsOutputStatusMIB_XUPS //1.3.6.1.4.1.534.1.4.5.0
    {
        Other = 1,
        None = 2,
        Normal = 3,
        Bypass = 4,
        Battery = 5,
        Booster = 6,
        Reducer = 7,
        ParallelCapacity = 8,
        ParallelRedundant = 9,
        HighEfficiencyMode = 10
    }

    [Serializable]
    public class LicenseInfo
    {
        public string ProductName { get; } = UpsMonitor.PluginName;
        public Guid ProductId { get; } = UpsMonitor.PluginId;
        public string LicenseType { get; } = UpsMonitor.LicenseString;
        public string SLC { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Signature { get; set; }
    }
}
