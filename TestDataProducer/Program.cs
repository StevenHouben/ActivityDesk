﻿using System.IO;
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

            _activitySystem.AddActivity(
                new Activity()
                {
                    
                }
                );

            while (Working) ;

        }

        static void activitySystem_ActivityAdded(object sender, ActivityEventArgs e)
        {
            var act = e.Activity as Activity;

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Desert.jpg")), "IMG", Path.GetFileName(@"C:\Users\Public\Pictures\Sample Pictures\Desert.jpg"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Hydrangeas.jpg")), "IMG",
                Path.GetFileName(@"C:\Users\Public\Pictures\Sample Pictures\Hydrangeas.jpg"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Jellyfish.jpg")), "IMG",
                Path.GetFileName(@"C:\Users\Public\Pictures\Sample Pictures\Jellyfish.jpg"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Koala.jpg")), "IMG",
                Path.GetFileName(@"C:\Users\Public\Pictures\Sample Pictures\Koala.jpg"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Lighthouse.jpg")), "IMG",
                Path.GetFileName(@"C:\Users\Public\Pictures\Sample Pictures\Lighthouse.jpg"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\Users\Public\Pictures\Sample Pictures\Penguins.jpg")), "IMG",
                Path.GetFileName(@"C:\Users\Public\Pictures\Sample Pictures\Lighthouse.jpg"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\papers\1.png")), "PDF",
                Path.GetFileName(@"C:\papers\1.png"));
            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\papers\2.png")),
                "PDF",
                Path.GetFileName(@"C:\papers\2.png"));

            _activitySystem.AddFileResourceToActivity(act, new MemoryStream(File.ReadAllBytes(@"C:\papers\3.png")), "PDF",
                Path.GetFileName(@"C:\papers\1.png"));

            if (count++ < 5)
                _activitySystem.AddActivity(new Activity());
            else
            {
                Working = false;
            }

        }
    }
}
