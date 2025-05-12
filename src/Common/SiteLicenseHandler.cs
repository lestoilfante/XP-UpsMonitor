using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using VideoOS.Platform;
using VideoOS.Platform.License;
using VideoOS.Platform.Messaging;

namespace UpsMonitor.Common
{
    internal class SiteLicenseHandler
    {
        private static LicenseInformation _myLicense;
        private static LicenseInformation _activatedLicense;

        private static object _msgLicenseReceived;
        private static bool _initialized = false;

        private static int _productCode = 0;
        private static readonly string _licenseFilePath = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "license.lic");
        private static readonly Random _random = new Random();

        private const string _publicKey = "RUNTMSAAAACX3Gif3YO/KllVjym2H9kXOFijfq5Sqc3DgUa2QwF3OCzlXbTJ9pIarUo/Km7ES4gxUyAlQlPqk0G8jJrPfnCn";

        public static bool IsFreeLicense
        {
            get
            {
                if (_productCode == 0)
                {
                    _productCode = EnvironmentManager.Instance.SystemLicense.ProductCode;
                }
                return (_productCode == 440);
            }
        }
        public static bool IsValidLicense
        {
            get
            {
                if (!_initialized)
                {
                    Init();
                }
                if (IsFreeLicense) return true;
                return (!_myLicense.TrialMode);
            }
        }

        internal static void Init()
        {
            _msgLicenseReceived = EnvironmentManager.Instance.RegisterReceiver(EvtNewLicense,
                                                                               new MessageIdFilter(
                                                                                MessageId.System.LicenseChangedIndication));

            // Get kind of license, if available
            // If this plugin does not have a kind of license, trial license will be returned.
            _activatedLicense = new SiteLicenseHandler().GetLicense();

            // Get the stored license on VMS
            _myLicense = EnvironmentManager.Instance.LicenseManager.ReservedLicenseManager.GetLicenseInformation(
                Common.UpsMonitor.PluginId,
                Common.UpsMonitor.LicenseString);

            // If real license is present, use that one as a starting point
            if (_activatedLicense != null && _activatedLicense.TrialMode == false)
            {
                _myLicense = _activatedLicense;
                // Store updated license on VMS
                if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.Administration)
                {
                    EnvironmentManager.Instance.LicenseManager.ReservedLicenseManager.SaveLicenseInformation(_myLicense);
                }
            }
            // Verify if there is a signed license on VMS
            else if (_myLicense != null && String.IsNullOrEmpty(_myLicense.CustomData) == false)
            {
                if (VerifySignature(_myLicense))
                {
                    _myLicense.TrialMode = false;
                }
            }
            else
            {
                _myLicense = _activatedLicense;
            }
            _initialized = true;
        }

        internal static void Close()
        {
            EnvironmentManager.Instance.UnRegisterReceiver(_msgLicenseReceived);
        }

        /// <summary>
        /// Return license information for our plugin(s)
        /// </summary>
        internal static Collection<LicenseInformation> GetPluginLicense()
        {
            if (!_initialized)
                Init();
            _myLicense.Counter = 1;
            return new Collection<LicenseInformation>() { _myLicense };
        }

        /// <summary>
        /// A new license response has been received via activation.
        /// </summary>
        private static object EvtNewLicense(VideoOS.Platform.Messaging.Message message, FQID s, FQID r)
        {
            // Do nothing since license is not currently managed as Milestone Partner
            return null;
        }

        /// <summary>
        /// Validates the license file against the current SLC
        /// </summary>
        public LicenseInformation GetLicense()
        {
            LicenseInfo _license = null;
            LicenseInformation license;

            try
            {
                // Check if license file exists
                if (!File.Exists(_licenseFilePath))
                {
                    throw new Exception("License file not found");
                }

                // Deserialize the license file
                using (FileStream fs = new FileStream(_licenseFilePath, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LicenseInfo));
                    _license = (LicenseInfo)serializer.Deserialize(fs);
                }

                // Check if license is expired
                if (DateTime.Now > _license.ExpirationDate)
                {
                    throw new Exception("License has expired");
                }

                string vmsSLC = EnvironmentManager.Instance.SystemLicense.SLC;

                // Check if SLC matches
                if (_license.SLC != vmsSLC)
                {
                    throw new Exception("License is not valid for this site");
                }

                // Verify the digital signature
                if (!VerifySignature(_license))
                {
                    throw new Exception("License is invalid");
                }

                license = new LicenseInformation()
                {
                    PluginId = _license.ProductId,
                    Counter = 1,
                    CustomData = _license.Signature,
                    LicenseType = _license.LicenseType,
                    Name = _license.ProductName,
                    Expire = _license.ExpirationDate,
                    ItemIdentifications = new Collection<LicenseItem>()
                };
            }
            catch (Exception ex)
            {
                license = new LicenseInformation()
                {
                    PluginId = Common.UpsMonitor.PluginId,
                    Counter = 1,
                    CustomData = ex.Message.Substring(0, Math.Min(255, ex.Message.Length)),
                    LicenseType = IsFreeLicense ? Common.UpsMonitor.LicenseFreeString : Common.UpsMonitor.LicenseString,
                    Name = Common.UpsMonitor.PluginName,
                    Expire = DateTime.UtcNow.AddYears(99),
                    TrialMode = true,
                    ItemIdentifications = new Collection<LicenseItem>()
                };
            }
            return license;
        }

        /// <summary>
        /// Verifies the digital signature of the license
        /// </summary>
        private static bool VerifySignature(LicenseInfo license)
        {
            try
            {
                // Create data to verify (everything except the signature itself)
                string dataToVerify = $"{license.ProductName}|{license.ProductId}|{license.LicenseType}|" +
                                    $"{license.SLC}|{license.ExpirationDate.Ticks}";

                // Convert signature from Base64 to bytes
                byte[] signatureBytes = Convert.FromBase64String(license.Signature);

                // Import the public key
                using (CngKey cngKey = CngKey.Import(Convert.FromBase64String(_publicKey), CngKeyBlobFormat.GenericPublicBlob))
                using (ECDsaCng ecdsa = new ECDsaCng(cngKey))
                {
                    // Verify the signature
                    return ecdsa.VerifyData(Encoding.UTF8.GetBytes(dataToVerify), signatureBytes, HashAlgorithmName.SHA256);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies the digital signature of the license as stored on VMS DB
        /// </summary>
        private static bool VerifySignature(LicenseInformation license)
        {
            try
            {
                // Create data to verify (everything except the signature itself)
                string dataToVerify = $"{UpsMonitor.PluginName}|{UpsMonitor.PluginId}|{license.LicenseType}|" +
                                    $"{EnvironmentManager.Instance.SystemLicense.SLC}|{license.Expire.Ticks}";

                // Convert signature from Base64 to bytes
                byte[] signatureBytes = Convert.FromBase64String(license.CustomData);

                // Import the public key
                using (CngKey cngKey = CngKey.Import(Convert.FromBase64String(_publicKey), CngKeyBlobFormat.GenericPublicBlob))
                using (ECDsaCng ecdsa = new ECDsaCng(cngKey))
                {
                    // Verify the signature
                    return ecdsa.VerifyData(Encoding.UTF8.GetBytes(dataToVerify), signatureBytes, HashAlgorithmName.SHA256);
                }
            }
            catch
            {
                return false;
            }
        }

        internal protected static string KindLycenseMe()
        {
            string[] strings = {
                "Open-source plugin running on perpetual trial mode. Join our supporters list with just one contribution.",
                "Open-source plugin running on perpetual trial mode. Still in unofficial mode. One small donation = one official license. Just saying.",
                "Open-source plugin running on perpetual trial mode. Procurement-friendly 'contribution receipt' available upon donation.",
                "Open-source plugin running on perpetual trial mode. Think about supporting, but hey, no pressure... it's your trial version.",
                "Open-source plugin running on perpetual trial mode. 100% functional. 0% enforced. 1000% emotionally manipulative for contribution.",
                "Open-source plugin running on perpetual trial mode. Powered by caffeine and unrealistic expectations of community support.",
                "Open-source plugin running on perpetual trial mode. Feel free to ask your IT department about supporting the tools you rely on.",
                "Open-source plugin running on perpetual trial mode. Like it? please consider supporting the project.",
                "Open-source plugin running on perpetual trial mode. Finance department called – they'd like to legitimize this asset.",
                "Open-source plugin running on perpetual trial mode. Works forever, but hopes you'll be kind.",
                "Open-source plugin running on perpetual trial mode. Community-powered software: support keeps our little engine running.",
                "Open-source plugin running on perpetual trial mode. Free trial of guilt expires never. Chance to donate expires... also never.",
                "Open-source plugin running on perpetual trial mode. Make it official! Any donation grants a genuine supporter license.",
                "Open-source plugin running on perpetual trial mode. You’re still using me, right? Can I count on you?",
                "Open-source plugin running on perpetual trial mode. Tech team installs me everywhere but forgets to say thanks.",
                "Open-source plugin running on perpetual trial mode. This notification is powered by hope and optimism. Your support validates both.",
                "Open-source plugin running on perpetual trial mode. No strings attached, except the silent expectation that you’ll do the right thing.",
                "Open-source plugin running on perpetual trial mode. We won’t lock features. Just your sense of ethical responsibility.",
                "Open-source plugin running on perpetual trial mode. You are not legally obligated to donate. But you may experience sudden desire to say thanks on GitHub.",
                "Open-source plugin running on perpetual trial mode. Adding this project to your 'properly supported software' portfolio today?",
                "Open-source plugin running on perpetual trial mode. You are not legally obligated to donate. But you may experience occasional guilt.",
                "Open-source plugin running on perpetual trial mode. Convert into an official business expense with any donation amount.",
                "Open-source plugin running on perpetual trial mode. Free to use. Freer to support.",
                "Open-source plugin running on perpetual trial mode. Curious why I'm not on 'supported software' budget?.",
                "Open-source plugin running on perpetual trial mode. No ads, no tracking, no mandatory fees. Just optional guilt trips. Donate?",
                "Open-source plugin running on perpetual trial mode. Almost licensed... just one small contribution away.",
                "Open-source plugin running on perpetual trial mode. Your system admin knows how to upgrade me to 'appreciated' status.",
                "Open-source plugin running on perpetual trial mode. Schrödinger's Cat Edition (simultaneously licensed and unlicensed until you donate).",
                "Open-source plugin running on perpetual trial mode. Works perfectly without a license. Your conscience, however...",
                "Open-source plugin running on perpetual trial mode. Supporting open source projects costs less than one corporate lunch meeting.",
                "Open-source plugin running on perpetual trial mode. Trust-based licensing model. We believe in you.",
                "Open-source plugin running on perpetual trial mode. Contribution turns this nagging reminder into eternal gratitude.",
                "Open-source plugin running on perpetual trial mode. Consider transition from 'forever evaluator' to 'acknowledged supporter'.",
                "Open-source plugin running on perpetual trial mode. Use it for free. Pay if it helped.",
                "Open-source plugin running on perpetual trial mode. Ask your installer why they're not supporting me.",
                "Open-source plugin running on perpetual trial mode. Made with love and open source magic. Your support keeps the magic flowing.",
                "Open-source plugin running on perpetual trial mode. Software usage policy typically requires proper licensing. Just a friendly corporate nudge.",
                "Open-source plugin running on perpetual trial mode. A KindLycense is not mandatory, just morally compelling.",
                "Open-source plugin running on perpetual trial mode. Convert your perpetual trial into documented goodwill with any donation.",
                "Open-source plugin running on perpetual trial mode. Your department budget has room for one small 'software appreciation' line item.",
                "Open-source plugin running on perpetual trial mode. Your conscience might be calling.",
                "Open-source plugin running on perpetual trial mode. Enterprise user detected. Enterprise supporter status pending.",
                "Open-source plugin running on perpetual trial mode. Ask your MSP why they're not supporting me.",
                "Open-source plugin running on perpetual trial mode. Free as in freedom, sustained by kindness.",
                "Open-source plugin running on perpetual trial mode. Corporate responsibility initiative: Transform your unofficial usage into official partnership status.",
                "Open-source plugin running on perpetual trial mode. One donation converts this from a budget oversight to a community investment."
            };
            int index = _random.Next(strings.Length);
            return strings[index];
        }
    }
}
