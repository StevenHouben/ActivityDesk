using System.IO;
using NooSphere.Infrastructure;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Helpers;
using NooSphere.Model;
using System;

namespace Debug.Datagenerator
{
    class Program
    {

        private static ActivitySystem _activitySystem;
        public static bool Working = true;
        public static int count = 0;

        static void Main(string[] args)
        {
            var databaseConfiguration = new DatabaseConfiguration("127.0.0.1", 8080, "desksystem");

            _activitySystem = new ActivitySystem(databaseConfiguration) { };

            _activitySystem.ActivityAdded += activitySystem_ActivityAdded;

            foreach (var a in _activitySystem.Activities.Keys)
                _activitySystem.RemoveActivity(a);

            foreach (var u in _activitySystem.Users.Keys)
                _activitySystem.RemoveUser(u);

            _activitySystem.DeleteAllAttachments();

            Console.WriteLine("All activities and resources deleted");
            Console.WriteLine("Press any key to create dummy data");
            Console.ReadKey();

            _activitySystem.AddActivity(new Activity());

            while (Working) ;

        }

        static void activitySystem_ActivityAdded(object sender, ActivityEventArgs e)
        {
            var act = e.Activity as Activity;

            _activitySystem.AddResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Desert.jpg")), "IMG");
            _activitySystem.AddResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Hydrangeas.jpg")), "IMG");
            _activitySystem.AddResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Jellyfish.jpg")), "IMG");
            _activitySystem.AddResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Koala.jpg")), "IMG");
            _activitySystem.AddResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Lighthouse.jpg")), "IMG");
            _activitySystem.AddResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Penguins.jpg")), "IMG");

            if (count++ < 5)
                _activitySystem.AddActivity(new Activity());
            else
            {
                Working = false;
            }

        }
    }
}
