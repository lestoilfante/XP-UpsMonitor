using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using UpsMonitor.Common;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Data;

namespace UpsMonitor.Admin
{
    /// <summary>
    /// This class manages the UPS in the plugin.
    /// It has a few ContextMenu actions
    /// It simulates ContextMenu actions directly, e.g. no real communication is going on.
    /// ContextMenu actions are created as Children as TriggerEvents, so they can be triggered by anyone
    /// Configuration is stored together with video server configuration.
    /// </summary>
    public class UpsMonitorItemManager : ItemManager
    {
        private UpsMonitorItemUserControl _userControlUps;
        private readonly Guid _kind;

        // Definitions for Event Server and Alarm handling
        internal EventGroup eventGroup = new EventGroup() { ID = Common.UpsMonitor.Event.GroupId, Name = Common.UpsMonitor.PluginName };

        #region Constructors

        public UpsMonitorItemManager(Guid kind)
        {
            _kind = kind;
        }

        public override void Init()
        {

        }

        public override void Close()
        {

        }

        #endregion

        #region UserControl Methods

        /// <summary>
        /// Generate the UserControl for configuring a type of item that this ItemManager manages.
        /// </summary>
        /// <returns></returns>
        public override UserControl GenerateDetailUserControl()
        {
            _userControlUps = new UpsMonitorItemUserControl();
            _userControlUps.ConfigurationChangedByUser += new EventHandler(ConfigurationChangedByUserHandler);
            return _userControlUps;
        }

        /// <summary>
        /// A user control to display when the administrator clicks on the treeNode.
        /// This can be a help page or a status over of the configuration
        /// </summary>
        public override ItemNodeUserControl GenerateOverviewUserControl()
        {
            var reminder = String.Empty;
            if (!SiteLicenseHandler.IsValidLicense)
            {
                reminder = Environment.NewLine + SiteLicenseHandler.KindLycenseMe();
            }
            return
            new VideoOS.Platform.UI.HelpUserControl(
                UpsMonitorDefinition._upsItemImage,
                "UPS Systems",
                "Here you can add all the UPS that you want to keep under control." + Environment.NewLine +
                "Event Server must be able to reach them in order to poll for SNMP status." + Environment.NewLine +
                "UPS status can then be used for display on the Smart Client map, and for event handling in the Event Server." + Environment.NewLine +
                reminder
                );
        }

        /// <summary>
        /// Clear all user entries on the UserControl.
        /// </summary>
        public override void ClearUserControl()
        {
            CurrentItem = null;
            _userControlUps?.ClearContent();
        }

        /// <summary>
        /// Fill the UserControl with the content of the Item or the data it represent.
        /// </summary>
        /// <param name="item">The Item to work with</param>
        public override void FillUserControl(Item item)
        {
            CurrentItem = item;
            _userControlUps?.FillContent(item);
        }

        /// <summary>
        /// The UserControl is not used any more. Release resources used by the UserControl.
        /// </summary>
        public override void ReleaseUserControl()
        {
            if (_userControlUps != null)
            {
                _userControlUps.ConfigurationChangedByUser -= new EventHandler(ConfigurationChangedByUserHandler);
                _userControlUps = null;
            }
        }
        #endregion

        #region Working with currentItem

        /// <summary>
        /// Get the name of the current Item.
        /// </summary>
        /// <returns></returns>
        public override string GetItemName()
        {
            if (_userControlUps != null)
            {
                return _userControlUps.DisplayName;
            }
            return "";
        }

        /// <summary>
        /// Update the name for current Item. The user edited the Name via F2 in the TreeView
        /// </summary>
        /// <param name="name"></param>
        public override void SetItemName(string name)
        {
            if (_userControlUps != null)
            {
                _userControlUps.DisplayName = name;
            }
        }

        /// <summary>
        /// Validate the user entry, and return true for OK
        /// If any entry error exists, the field in error should get focus, and an error
        //  message should be displayed to the user or a ValidateAndSaveMIPException should
        //  be thrown.
        /// </summary>
        /// <returns></returns>
        public override bool ValidateAndSaveUserControl()
        {
            if (CurrentItem != null)
            {
                //Get user entered fields
                _userControlUps.UpdateItem(CurrentItem);

                //In this template we save configuration on the VMS system
                Configuration.Instance.SaveItemConfiguration(Common.UpsMonitor.PluginId, CurrentItem);
                UpsMonitorHelper.SavePollInterval(_userControlUps.PollInterval);

                //Send message to refresh the view
                EnvironmentManager.Instance.SendMessage(
                    new VideoOS.Platform.Messaging.Message(VideoOS.Platform.Messaging.MessageId.System.ApplicationRefreshTreeViewCommand) { Data = Common.UpsMonitor.CtrlKindId });
            }
            return true;
        }

        /// <summary>
        /// Create a new Item
        /// </summary>
        /// <param name="parentItem">The parent for the new Item</param>
        /// <param name="suggestedFQID">A suggested FQID for the new Item</param>
        public override Item CreateItem(Item parentItem, FQID suggestedFQID)
        {
            CurrentItem = new Item(suggestedFQID, "Enter a name");
            _userControlUps?.FillContent(CurrentItem);
            Configuration.Instance.SaveItemConfiguration(Common.UpsMonitor.PluginId, CurrentItem);
            //Send message to refresh the view
            EnvironmentManager.Instance.SendMessage(
                new VideoOS.Platform.Messaging.Message(VideoOS.Platform.Messaging.MessageId.System.ApplicationRefreshTreeViewCommand) { Data = Common.UpsMonitor.CtrlKindId });
            return CurrentItem;
        }

        /// <summary>
        /// Delete an Item
        /// </summary>
        /// <param name="item">The Item to delete</param>
        public override void DeleteItem(Item item)
        {
            if (item != null)
            {
                Configuration.Instance.DeleteItemConfiguration(Common.UpsMonitor.PluginId, item);
                //Send message to refresh the view
                EnvironmentManager.Instance.SendMessage(
                    new VideoOS.Platform.Messaging.Message(VideoOS.Platform.Messaging.MessageId.System.ApplicationRefreshTreeViewCommand) { Data = Common.UpsMonitor.CtrlKindId });
            }

        }
        #endregion

        #region Configuration Access Methods

        /// <summary>
        /// Returns a list of all Items of this Kind
        /// </summary>
        /// <returns>A list of items.  Allowed to return null if no Items found.</returns>
        public override List<Item> GetItems()
        {
            //All items in this sample are stored with the Video, therefore no ServerIds or parent ids is used.
            List<Item> items = Configuration.Instance.GetItemConfigurations(Common.UpsMonitor.PluginId, null, _kind);
            List<Item> myItems = new List<Item>();
            foreach (Item item in items)
            {
                myItems.Add(new UpsItem(item));
            }
            return myItems;
        }

        /// <summary>
        /// Returns a list of all Items from a specific server.
        /// </summary>
        /// <param name="parentItem">The parent Item</param>
        /// <returns>A list of items.  Allowed to return null if no Items found.</returns>
        public override List<Item> GetItems(Item parentItem)
        {
            List<Item> items = Configuration.Instance.GetItemConfigurations(Common.UpsMonitor.PluginId, parentItem, _kind);
            List<Item> myItems = new List<Item>();
            foreach (Item item in items)
            {
                myItems.Add(new UpsItem(item));
            }
            return myItems;
        }

        /// <summary>
        /// Returns the Item defined by the FQID. Will return null if not found.
        /// </summary>
        /// <param name="fqid">Fully Qualified ID of an Item</param>
        /// <returns>An Item</returns>
        public override Item GetItem(FQID fqid)
        {
            Item item = Configuration.Instance.GetItemConfiguration(Common.UpsMonitor.PluginId, _kind, fqid.ObjectId);
            if (item == null)
                return null;
            return new UpsItem(item);
        }

        #endregion

        #region Event Server support

        /// <summary>
        /// Return an Event Group, to assist in configuring the Alarms 
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public override Collection<EventGroup> GetKnownEventGroups(CultureInfo culture)
        {
            return new Collection<EventGroup>() { eventGroup };
        }

        public override Collection<EventType> GetKnownEventTypes(CultureInfo culture)
        {
            return new Collection<EventType>
                       {
                        new EventType() {
                            GroupID = Common.UpsMonitor.Event.GroupId,
                            ID = Common.UpsMonitor.Event.OnBatteryEnd.MessageId,
                            StateGroupID = Common.UpsMonitor.Event.OnBatteryEnd.StateGroupId,
                            State = "OK",
                            DefaultSourceKind = Common.UpsMonitor.CtrlKindId,
                            Message = Common.UpsMonitor.Event.OnBatteryEnd.Message,
                            SourceKinds = new List<Guid>(){ Common.UpsMonitor.CtrlKindId}},
                        new EventType() {
                            GroupID = Common.UpsMonitor.Event.GroupId,
                            ID = Common.UpsMonitor.Event.OnBatteryStart.MessageId,
                            StateGroupID = Common.UpsMonitor.Event.OnBatteryStart.StateGroupId,
                            State = "Battery",
                            DefaultSourceKind = Common.UpsMonitor.CtrlKindId,
                            Message = Common.UpsMonitor.Event.OnBatteryStart.Message,
                            SourceKinds = new List<Guid>(){ Common.UpsMonitor.CtrlKindId}},
                        new EventType() {
                            GroupID = Common.UpsMonitor.Event.GroupId,
                            ID = Common.UpsMonitor.Event.OnWarningEnd.MessageId,
                            StateGroupID = Common.UpsMonitor.Event.OnWarningEnd.StateGroupId,
                            State = "OK",
                            DefaultSourceKind = Common.UpsMonitor.CtrlKindId,
                            Message = Common.UpsMonitor.Event.OnWarningEnd.Message,
                            SourceKinds = new List<Guid>(){ Common.UpsMonitor.CtrlKindId}},
                        new EventType() {
                            GroupID = Common.UpsMonitor.Event.GroupId,
                            ID = Common.UpsMonitor.Event.OnWarningStart.MessageId,
                            StateGroupID = Common.UpsMonitor.Event.OnWarningStart.StateGroupId,
                            State = "Warning",
                            DefaultSourceKind = Common.UpsMonitor.CtrlKindId,
                            Message = Common.UpsMonitor.Event.OnWarningStart.Message,
                            SourceKinds = new List<Guid>(){ Common.UpsMonitor.CtrlKindId}}

                       };
        }

        /// <summary>
        /// Return the event state groups defined and used by this plugin
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public override Collection<StateGroup> GetKnownStateGroups(CultureInfo culture)
        {
            return new Collection<StateGroup>
            {
                new StateGroup() {
                    ID = Common.UpsMonitor.Event.StateGroupIdOutputSource,
                    Name = "UPS Output State",
                    States = new[] { "OK", "Battery" }
                },
                new StateGroup() {
                    ID = Common.UpsMonitor.Event.StateGroupIdWarning,
                    Name = "UPS Warning State",
                    States = new[] { "OK", "Warning" }
                }
            };
        }

        public override OperationalState GetOperationalState(Item item)
        {
            if (EnvironmentManager.Instance.EnvironmentType != EnvironmentType.Service)
            {
                return OperationalState.Ok;
            }
            // Make sense only on Event Server
            return UpsMonitorHelper.GetUpsOperationalState(item);
        }

        /// <summary>
        /// Build and return some details.  In this sample we ignore the language.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public override string GetItemStatusDetails(Item item, string language)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false
                };
                using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings))
                {
                    xmlWriter.WriteStartElement("details");
                    xmlWriter.WriteAttributeString("language", "en-US");

                    xmlWriter.WriteStartElement("detail");
                    xmlWriter.WriteAttributeString("detailname", "UPS State");
                    xmlWriter.WriteElementString("detail_string", UpsMonitorHelper.GetUpsOutputSource(item));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("detail");
                    xmlWriter.WriteAttributeString("detailname", "Backup Time");
                    xmlWriter.WriteElementString("detail_int64", "" + UpsMonitorHelper.GetUpsRuntimeRemaining(item));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("detail");
                    xmlWriter.WriteAttributeString("detailname", "Poll Error");
                    xmlWriter.WriteElementString("detail_string", UpsMonitorHelper.GetUpsSNMPerror(item));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("detail");
                    xmlWriter.WriteAttributeString("detailname", "Operational State");
                    xmlWriter.WriteElementString("detail_string", UpsMonitorHelper.GetUpsOperationalState(item).ToString());
                    xmlWriter.WriteEndElement();

                    if (!SiteLicenseHandler.IsValidLicense)
                    {
                        xmlWriter.WriteStartElement("detail");
                        xmlWriter.WriteAttributeString("detailname", "Info");
                        xmlWriter.WriteElementString("detail_string", SiteLicenseHandler.KindLycenseMe());
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                    xmlWriter.Flush();
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return new StreamReader(memoryStream, Encoding.UTF8).ReadToEnd();
                }
            }
        }

        #endregion

        #region Translation

        #endregion
    }

}

