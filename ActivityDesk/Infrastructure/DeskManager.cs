using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Discovery;
using NooSphere.Infrastructure.Helpers;
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
            _activitySystem.ActivityRemoved += _activitySystem_ActivityRemoved;
            _activitySystem.ActivityAdded += _activitySystem_ActivityAdded;
            _activitySystem.ResourceAdded += _activitySystem_ResourceAdded;    


            _activityService = new ActivityService(_activitySystem, Net.GetIp(IpType.All), 8000);

            _activityService.Start();

            _activityService.StartBroadcast(DiscoveryType.Zeroconf, "deskmanager");

            _documentContainer = documentContainer;

            InitializeContainer();
        }

        void _activitySystem_ResourceAdded(object sender, ResourceEventArgs e)
        {
            _documentContainer.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {

            var image = new Image();
            using (var stream = _activitySystem.GetStreamFromResource(e.Resource.Id))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                image.Source = bitmap;
            }
            _documentContainer.AddResource(image, "");
            }));
        }

        private void InitializeContainer()
        {
            foreach(var act in _activitySystem.Activities.Values)
                foreach (var resource in act.Resources)
                {
                    if (resource != null)
                    {
                        var image = new Image();
                        using (var stream = _activitySystem.GetStreamFromResource(resource.Id))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            image.Source = bitmap;
                        }
                        _documentContainer.AddResource(image, "");
                    }
                }
        }

        private readonly Activity _activity = new Activity() {Name = "Images"};
        public void AddAttachment()
        {
            //var resource = _activitySystem.AddResourceToActivity(_activity, new MemoryStream(File.ReadAllBytes(@"C:\morten.jpg")), "",
            //    Path.GetExtension(@"C:\morten.jpg"));
            //_activity.Resources.Add(resource);
        }
            
        void _activitySystem_ActivityRemoved(object sender, NooSphere.Infrastructure.ActivityRemovedEventArgs e)
        {
        
        }

        void _activitySystem_ActivityAdded(object sender, NooSphere.Infrastructure.ActivityEventArgs e)
        {
            Activities.Add(e.Activity);
        }

    }

    public class Note:LegacyResource
    {
        public string Title { get; set; }
        public string Content { get; set; }

    }
}
