using System;
using System.Collections.ObjectModel;
using System.IO;
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


        public void Start()
        {
            Activities = new Collection<IActivity>();

            _activitySystem = new ActivitySystem("desksystem");
            _activitySystem.ActivityRemoved += _activitySystem_ActivityRemoved;
            _activitySystem.ActivityAdded += _activitySystem_ActivityAdded;
            _activitySystem.Run(WebConfiguration.DefaultWebConfiguration);

            _activitySystem.StartBroadcast(DiscoveryType.Zeroconf, "deskmanager");

            _activityService = new ActivityService(_activitySystem, Net.GetIp(IpType.All), 8000);

            _activityService.Start();

            _activitySystem.AddActivity(new Activity());
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
