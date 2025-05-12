using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using UpsMonitor.Admin;
using UpsMonitor.Background;
using UpsMonitor.Common;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Background;

namespace UpsMonitor
{
    /// <summary>
    /// The PluginDefinition is the ‘entry’ point to any plugin.  
    /// This is the starting point for any plugin development and the class MUST be available for a plugin to be loaded.  
    /// Several PluginDefinitions are allowed to be available within one DLL.
    /// Here the references to all other plugin known objects and classes are defined.
    /// The class is an abstract class where all implemented methods and properties need to be declared with override.
    /// The class is constructed when the environment is loading the DLL.
    /// </summary>
    public class UpsMonitorDefinition : PluginDefinition
    {
        internal protected static System.Drawing.Image _upsItemImage;
        internal protected static System.Drawing.Image _treeNodeImage;
        internal protected static System.Drawing.Image _topTreeNodeImage;

        internal static ConcurrentDictionary<Guid, UpsStatus> LastPollResults = new ConcurrentDictionary<Guid, UpsStatus>();

        #region Private fields

        //
        // Note that all the plugin are constructed during application start, and the constructors
        // should only contain code that references their own dll, e.g. resource load.

        private UserControl _treeNodeInfoUserControl;
        private List<ItemNode> _itemNodes;
        private readonly List<BackgroundPlugin> _backgroundPlugins = new List<BackgroundPlugin>();

        #endregion

        #region Initialization

        /// <summary>
        /// Load resources 
        /// </summary>
        static UpsMonitorDefinition()
        {
            _upsItemImage = Properties.Resources.UPS;
            _treeNodeImage = Properties.Resources.UPS;
        }


        /// <summary>
        /// Get the icon for the plugin
        /// </summary>
        internal static Image TreeNodeImage
        {
            get { return _treeNodeImage; }
        }

        #endregion

        /// <summary>
        /// This method is called when the environment is up and running.
        /// Registration of Messages via RegisterReceiver can be done at this point.
        /// </summary>
        public override void Init()
        {
            _topTreeNodeImage = Properties.Resources.UPS16x16; //Must be 16x16 coz on Admini -> Rule -> event -> icon doesn't resize

            List<SecurityAction> _securityActionsCtrl = new List<SecurityAction>
                                                       {
                                                           new SecurityAction("GENERIC_WRITE", "Manage"),
                                                           new SecurityAction("GENERIC_READ", "Read"),
                                                       };

            Dictionary<Guid, Icon> upsMapIcon = new Dictionary<Guid, Icon>
            {
                { Common.UpsMonitor.EventIcon.Ok.Key, Common.UpsMonitor.EventIcon.Ok.Value },
                { Common.UpsMonitor.EventIcon.Error.Key, Common.UpsMonitor.EventIcon.Error.Value }
            };     // Have 2 Icons ready for use on the MAP

            _itemNodes = new List<ItemNode>
                             {
                                 new ItemNode(Common.UpsMonitor.CtrlKindId,
                                              Guid.Empty,
                                              "UPS Systems", _upsItemImage,
                                              "UPS Systems", _upsItemImage,
                                              Category.Text, true,
                                              ItemsAllowed.Many,
                                              new UpsMonitorItemManager(Common.UpsMonitor.CtrlKindId),
                                              null,
                                              null,
                                              null,
                                              null,
                                              PlacementHint.Devices
                                     )
                                     {
                                        SecurityActions = _securityActionsCtrl,
                                        MapIconDictionary = upsMapIcon
                                     }
                             };

            _backgroundPlugins.Add(new UpsMonitorBackgroundPlugin());
        }

        /// <summary>
        /// The main application is about to be in an undetermined state, either logging off or exiting.
        /// You can release resources at this point, it should match what you acquired during Init, so additional call to Init() will work.
        /// </summary>
        public override void Close()
        {
            _backgroundPlugins.Clear();
        }

        #region Identification Properties

        /// <summary>
        /// Gets the unique id identifying this plugin component
        /// </summary>
        public override Guid Id
        {
            get
            {
                return Common.UpsMonitor.PluginId;
            }
        }

        /// <summary>
        /// This Guid can be defined on several different IPluginDefinitions with the same value,
        /// and will result in a combination of this top level ProductNode for several plugins.
        /// Set to Guid.Empty if no sharing is enabled.
        /// </summary>
        public override Guid SharedNodeId
        {
            get
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Define name of top level Tree node - e.g. A product name
        /// </summary>
        public override string Name
        {
            get { return Common.UpsMonitor.PluginName; }
        }

        /// <summary>
        /// Top level name
        /// </summary>
        public override string SharedNodeName
        {
            get { return Common.UpsMonitor.PluginName; }
        }

        /// <summary>
        /// Your company name
        /// </summary>
        public override string Manufacturer
        {
            get
            {
                return Common.UpsMonitor.ManufacturerName;
            }
        }

        /// <summary>
        /// Version of this plugin.
        /// </summary>
        public override string VersionString
        {
            get
            {
                return Common.UpsMonitor.VersionString;
            }
        }

        /// <summary>
        /// Icon to be used on top level - e.g. a product or company logo
        /// </summary>
        public override System.Drawing.Image Icon
        {
            get { return _topTreeNodeImage; }
        }

        #endregion


        #region Administration properties

        /// <summary>
        /// A list of server side configuration items in the administrator
        /// </summary>
        public override List<ItemNode> ItemNodes
        {
            get { return _itemNodes; }
        }

        /// <summary>
        /// A user control to display when the administrator clicks on the top TreeNode
        /// </summary>
        public override UserControl GenerateUserControl()
        {
            _treeNodeInfoUserControl = new HelpPage();
            return _treeNodeInfoUserControl;
        }

        /// <summary>
        /// This property can be set to true, to be able to display your own help UserControl on the entire panel.
        /// When this is false - a standard top and left side is added by the system.
        /// </summary>
        public override bool UserControlFillEntirePanel
        {
            get { return false; }
        }

        /// <summary>
        /// Return a list of the LicenseRequest that this plugin needs.
        /// </summary>
        public override Collection<VideoOS.Platform.License.LicenseInformation> PluginLicenseRequest
        {
            get { return SiteLicenseHandler.GetPluginLicense(); }
        }
        #endregion

        #region Client related methods and properties

        /// <summary>
        /// Create and returns the background task.
        /// </summary>
        public override List<VideoOS.Platform.Background.BackgroundPlugin> BackgroundPlugins
        {
            get { return _backgroundPlugins; }
        }

        #endregion

    }
}
