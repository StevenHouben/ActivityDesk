using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Discovery;
using NooSphere.Infrastructure.Helpers;
using NooSphere.Infrastructure.Web;
using NooSphere.Model;
using NooSphere.Model.Configuration;
using NooSphere.Model.Device;
using Image = System.Windows.Controls.Image;

namespace ActivityDesk.Infrastructure
{
    internal class DeskManager
    {
        private ActivitySystem _activitySystem;
        private ActivityService _activityService;
        private DocumentContainer _documentContainer;
        private Activity _selectedActivity;

        private readonly List<string> _queuedDeviceDetections = new List<string>();


        /// <summary>
        /// Start the desk manager
        /// </summary>
        public void Start(DocumentContainer documentContainer)
        {

            var webconfiguration = WebConfiguration.DefaultWebConfiguration;

            const string ravenDatabaseName = "desksystem";

            var databaseConfiguration = new DatabaseConfiguration(webconfiguration.Address, webconfiguration.Port,
                ravenDatabaseName);

            _activitySystem = new ActivitySystem(databaseConfiguration);

            _activitySystem.ActivityAdded += _activitySystem_ActivityAdded;
            _activitySystem.ActivityChanged += _activitySystem_ActivityChanged;
            _activitySystem.ActivityRemoved += _activitySystem_ActivityRemoved;
            _activitySystem.ResourceAdded += _activitySystem_ResourceAdded;
            _activitySystem.DeviceAdded += _activitySystem_DeviceAdded;
            _activitySystem.DeviceRemoved += _activitySystem_DeviceRemoved;
            _activitySystem.MessageReceived += _activitySystem_MessageReceived;

            _activityService = new ActivityService(_activitySystem, Net.GetIp(IpType.All), 8000);

            _activityService.Start();

            _activityService.StartBroadcast(DiscoveryType.Zeroconf, "deskmanager", "pitlab", "9865");

            _documentContainer = documentContainer;

            _documentContainer.ResourceHandled += _documentContainer_IntersectionDetected;
            _documentContainer.ResourceHandleReleased += _documentContainer_ResourceHandleReleased;
            _documentContainer.DeviceValueAdded += _documentContainer_DeviceValueAdded;
            _documentContainer.DeviceValueRemoved += _documentContainer_DeviceValueRemoved;

            foreach (var res in _activitySystem.Activities.Values.SelectMany(act => act.Resources))
            {
                _documentContainer.ResourceCache.Add(res.Id, FromResource(res));
            }
           

            if (_activitySystem.Activities.Count>0)
                _selectedActivity = _activitySystem.Activities.Values.First() as Activity;

            if (_selectedActivity != null)
                InitializeContainer(_selectedActivity);

        }

        void _activitySystem_ActivityRemoved(object sender, NooSphere.Infrastructure.ActivityRemovedEventArgs e)
        {
            if (_selectedActivity == null) return;
            if (_selectedActivity.Id != e.Id) return;
            Application.Current.Dispatcher.Invoke(() => _documentContainer.Clear());
            _selectedActivity = null;
        }

        void _activitySystem_ActivityChanged(object sender, NooSphere.Infrastructure.ActivityEventArgs e)
        {

        }

        private void _activitySystem_MessageReceived(object sender, MessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case MessageType.ActivityChanged:
                    if (e.Message.Content != null && _activitySystem.Activities.ContainsKey(e.Message.Content as string))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_selectedActivity != null)
                            {
                                _selectedActivity.Configuration =
                                    _documentContainer.GetDeskConfiguration() as ISituatedConfiguration;

                                _activitySystem.UpdateActivity(_selectedActivity);
                            }


                            _selectedActivity = _activitySystem.Activities[e.Message.Content as string] as Activity;

                            if (_selectedActivity != null)
                                InitializeContainer(_selectedActivity);
                        });
                    }

                    break;
                    case MessageType.Control:
                    if (e.Message.Content != null && (string) e.Message.Content == "slave")
                    {
                        if (e.Message.From == null) return;
                        if (_activitySystem.Devices.ContainsKey(e.Message.From))
                        {
                            var dev = _activitySystem.Devices[e.Message.From] as Device;
 
                            DeviceContainer container = null;
                            foreach (var con in _documentContainer.DeviceContainers.Values.Where(con => con.Device.Id == e.Message.From))
                                container = con;
                            if (container == null)
                                return;
                            foreach (var lr in container.LoadedResources)
                            {
                                _activityService.SendMessage(dev, MessageType.Resource, lr.Resource);
                            }
                          
                        }
                    }
                    break;
            }
        }

        private void _documentContainer_ResourceHandleReleased(object sender, ResourceHandle e)
        {
            _activityService.SendMessage(e.Device, MessageType.ResourceRemove, e.Resource);
        }

        private void _documentContainer_DeviceValueRemoved(object sender, string e)
        {
            //no implications for NooSphere -> just remove visualisation
        }

        private void _documentContainer_DeviceValueAdded(object sender, string e)
        {
            //If device value matches a device connected to NooSphere
            if (_activitySystem.Devices.Values.Any(dev => dev.TagValue == e))
            {
                var device = _activitySystem.Devices.Values.Where(dev => dev.TagValue == e);
                _documentContainer.ValidateDevice(e, device.First() as Device);
                return;
            }

            //Does not match any device connected to NooSphere, so queue uit
            _queuedDeviceDetections.Add(e);
        }
        void _activitySystem_DeviceRemoved(object sender, DeviceRemovedEventArgs e)
        {

            //hard kill of device visualisation since NooSphere device is closes
            _documentContainer.RemoveDevice(e.Id);
        }

        private void _activitySystem_DeviceAdded(object sender, DeviceEventArgs e)
        {
            var value = "";

            //If no tag is found, we don't need to handle the device now
            if (!_documentContainer.DeviceContainers.ContainsKey(e.Device.TagValue)) 
                return;

            //Check whether the added NooSphere device matches a detected tag
            foreach (var val in _queuedDeviceDetections.Where(val => val == e.Device.TagValue))
            {
                //Validate it
                _documentContainer.ValidateDevice(val, e.Device as Device);
                value = val;
            }

            //Device is not found, but maybe it is trying to reconnect to the visualisation
            if (value == "")
            {
                foreach (var val in _documentContainer.DeviceContainers.Keys.Where(val => val == e.Device.TagValue))
                {
                    _documentContainer.ReconnectDevice(val, e.Device as Device);
                    value = val;
                }
            }

            //if no device was found, we add it to
            if (value != "" && _queuedDeviceDetections.Contains(value))
                _queuedDeviceDetections.Remove(value);
        }

        private void _documentContainer_IntersectionDetected(object sender, ResourceHandle e)
        {
            _activityService.SendMessage(e.Device, MessageType.Resource, e.Resource);
        }

        private void _activitySystem_ResourceAdded(object sender, ResourceEventArgs e)
        {
            _documentContainer.Dispatcher.Invoke(DispatcherPriority.Background,
                new System.Action(() =>
                {
                    var loadedResource = FromResource(e.Resource);
                    _documentContainer.ResourceCache.Add(e.Resource.Id, loadedResource);

                    if (e.Resource.ActivityId == _selectedActivity.Id)
                        _documentContainer.AddResource(loadedResource, true);
                }));
        }
        internal LoadedResource FromResource(Resource res)
        {
            var loadedResource = new LoadedResource();

            var image = new Image();
            using (var stream = _activitySystem.GetStreamFromResource(res.Id))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                image.Source = bitmap;
                image.Width = bitmap.Width;
                image.Height = bitmap.Height;
            }
            loadedResource.Resource = res;
            loadedResource.Content = image;
            loadedResource.Thumbnail = image.Source;
            return loadedResource;
        }
        private void InitializeContainer(Activity act)
        {
            Application.Current.Dispatcher.Invoke(() => _documentContainer.Build(act));
        }

        void _activitySystem_ActivityAdded(object sender, NooSphere.Infrastructure.ActivityEventArgs e)
        {
            if (_selectedActivity != null)
            {
                 Application.Current.Dispatcher.Invoke(() =>
                        {
                                            _selectedActivity.Configuration =
                    _documentContainer.GetDeskConfiguration() as ISituatedConfiguration;

                         _activitySystem.UpdateActivity(_selectedActivity);
                        });

            }

            _selectedActivity = (Activity)e.Activity;

            if (_selectedActivity != null)
                InitializeContainer(_selectedActivity);
        }
        public string Title { get; set; }
        public string Content { get; set; }

    }
}
