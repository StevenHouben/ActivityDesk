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
        private static ActivitySystem _activitySystem;
        private ActivityService _activityService;

        public Collection<IActivity> Activities { get; set; }

        private readonly List<string> _queuedDeviceDetections = new List<string>();

        private DocumentContainer _documentContainer;

        private Dictionary<string, Device> _devices = new Dictionary<string, Device>();

        private Activity _selectedActivity;

        public void Start(DocumentContainer documentContainer)
        {
            Activities = new Collection<IActivity>();

            var webconfiguration = WebConfiguration.DefaultWebConfiguration;

            const string ravenDatabaseName = "desksystem";

            var databaseConfiguration = new DatabaseConfiguration(webconfiguration.Address, webconfiguration.Port,
                ravenDatabaseName);


            _activitySystem = new ActivitySystem(databaseConfiguration);
            _activitySystem.ActivityAdded += _activitySystem_ActivityAdded;
            _activitySystem.ResourceAdded += _activitySystem_ResourceAdded;

            _activitySystem.DeviceAdded += _activitySystem_DeviceAdded;

            _activitySystem.MessageReceived += _activitySystem_MessageReceived;


            _activityService = new ActivityService(_activitySystem, Net.GetIp(IpType.All), 8000);

            _activityService.Start();

            _activityService.StartBroadcast(DiscoveryType.Zeroconf, "deskmanager", "pitlab", "9865");

            _documentContainer = documentContainer;

            _documentContainer.ResourceHandled += _documentContainer_IntersectionDetected;
            _documentContainer.ResourceHandleReleased += _documentContainer_ResourceHandleReleased;
            _documentContainer.DeviceValueAdded += _documentContainer_DeviceValueAdded;
            _documentContainer.DeviceValueRemoved += _documentContainer_DeviceValueRemoved;


            _selectedActivity = _activitySystem.Activities.Values.First() as Activity;

            if (_selectedActivity != null)
                InitializeContainer(_selectedActivity);

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
            }
        }

        private void _documentContainer_ResourceHandleReleased(object sender, ResourceHandle e)
        {
            _activityService.SendMessage(e.Device, MessageType.ResourceRemove, e.Resource);
        }

        private void _documentContainer_DeviceValueRemoved(object sender, string e)
        {
            if (_activitySystem.Devices.Values.Any(dev => dev.TagValue == e))
            {
                //remove here
            }
        }

        private void _documentContainer_DeviceValueAdded(object sender, string e)
        {
            if (_activitySystem.Devices.Values.Any(dev => dev.TagValue == e))
            {
                var device = _activitySystem.Devices.Values.Where(dev => dev.TagValue == e);
                _documentContainer.ValidateDevice(e, device.First() as Device);
                return;
            }
            _queuedDeviceDetections.Add(e);
        }

        private void _activitySystem_DeviceAdded(object sender, DeviceEventArgs e)
        {
            var value = "";
            foreach (var val in _queuedDeviceDetections.Where(val => val == e.Device.TagValue))
            {
                _documentContainer.ValidateDevice(val, e.Device as Device);
                value = val;
            }
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
                new System.Action(() => AddResourceToContainer(e.Resource)));
        }

        private void AddResourceToContainer(Resource e)
        {
            _documentContainer.AddResource(FromResource(e));
        }

        internal LoadedResource FromResource(Resource res)
        {
            var loadedResource = new LoadedResource();

            res.FileType = "IMG";

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
            var configuration = act.Configuration;

            var loadedResources = act.Resources.ToDictionary(res => res.Id, FromResource);

            _documentContainer.Build(loadedResources, configuration as DeskConfiguration);
            _selectedActivity = act;
 
        }

        void _activitySystem_ActivityAdded(object sender, NooSphere.Infrastructure.ActivityEventArgs e)
        {
            Activities.Add(e.Activity);
        }
        public string Title { get; set; }
        public string Content { get; set; }

    }
}
