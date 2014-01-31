using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Hosting;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Discovery;
using NooSphere.Infrastructure.Helpers;
using NooSphere.Infrastructure.Web;
using NooSphere.Model;

namespace ActivityDesk.Infrastructure
{
    internal class DeskManager
    {
        private ActivitySystem _activitySystem;
        private ActivityService _activityService;

        public Collection<IActivity> Activities { get; set; }

        private DocumentContainer _documentContainer;
        public void Start(DocumentContainer documentContainer)
        {
            Activities = new Collection<IActivity>();

            var webconfiguration = WebConfiguration.DefaultWebConfiguration;

            const string ravenDatabaseName = "desksystem";

            var databaseConfiguration = new DatabaseConfiguration(webconfiguration.Address, webconfiguration.Port, ravenDatabaseName);


            _activitySystem = new ActivitySystem(databaseConfiguration);
            _activitySystem.ActivityAdded += _activitySystem_ActivityAdded;
            _activitySystem.ResourceAdded += _activitySystem_ResourceAdded;    


            _activityService = new ActivityService(_activitySystem, Net.GetIp(IpType.All), 8000);

            _activityService.Start();

            _activityService.StartBroadcast(DiscoveryType.Zeroconf, "deskmanager");

            _documentContainer = documentContainer;
            _documentContainer.IntersectionDetected += _documentContainer_IntersectionDetected;

            InitializeContainer();
        }

        void _documentContainer_IntersectionDetected(object sender, Intersection e)
        {
            _activityService.SendMessage(MessageType.Resource, e.Resource);
        }

        void _activitySystem_ResourceAdded(object sender, ResourceEventArgs e)
        {
            _documentContainer.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() => AddResourceToContainer(e.Resource)));
        }

        private void AddResourceToContainer(Resource e)
        {
            _documentContainer.AddResource(FromResource(e));
        }

        private LoadedResource FromResource(Resource res)
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
            }
            loadedResource.Resource = res;
            loadedResource.Content = image;
            loadedResource.Thumbnail = image.Source;
            return loadedResource;
        }

        private void InitializeContainer()
        {
            foreach (var resource in from act in _activitySystem.Activities.Values from resource in act.Resources where resource != null select resource)
                _documentContainer.AddResource(FromResource(resource));
        }

        void _activitySystem_ActivityAdded(object sender, NooSphere.Infrastructure.ActivityEventArgs e)
        {
            Activities.Add(e.Activity);
        }
        public string Title { get; set; }
        public string Content { get; set; }

    }
}
