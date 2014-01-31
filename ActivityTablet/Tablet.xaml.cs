using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Surface.Presentation.Controls;
using NooSphere.Infrastructure;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Discovery;
using NooSphere.Infrastructure.Files;
using NooSphere.Infrastructure.Helpers;
using NooSphere.Infrastructure.Web;
using NooSphere.Model;
using NooSphere.Model.Device;
using NooSphere.Model.Users;

namespace ActivityTablet
{
    public partial class Tablet
    {
        #region Private Members
        private User _user;
        private Device _device;
        private readonly Dictionary<string, Proxy> _proxies = new Dictionary<string, Proxy>();
        private Activity _currentActivity;
        private ActivityClient _client;

        #endregion

        #region Constructor
        public Tablet()
        {
            InitializeComponent();

            BuildUI();

            _user = new User()
            {
                Name = "TabletUser"
            };

            _device = new Device()
            {
                DeviceType = DeviceType.Tablet
            };

            RunDiscovery();
        }
        #endregion

        #region Private Methods

        private void RunDiscovery()
        {
           var disco = new DiscoveryManager();

            disco.DiscoveryAddressAdded += (sender, e) =>
            {
                var foundWebConfiguration = new WebConfiguration(e.ServiceInfo.Address);
                StartClient(foundWebConfiguration);
            };
            disco.Find(DiscoveryType.Zeroconf);

        }
    
        void activityClient_ActivityRemoved(object sender, ActivityRemovedEventArgs e)
        {
            RemoveActivityUI(e.Id);
        }

        void activityClient_ActivityAdded(object sender, ActivityEventArgs e)
        {
            AddActivityUI(e.Activity as Activity);
        }

        private void BuildUI()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                resourceViewer.Visibility = Visibility.Hidden;
                //inputView.Visibility = Visibility.Hidden;
                //menu.Visibility = Visibility.Hidden;
                WindowStyle = WindowStyle.ToolWindow;
                Width = 1280;
                Height = 800;
                MaxHeight = 800;
                MaxWidth = 1280;

                //Show menu
                menu.Visibility = Visibility.Visible;
                
                //Show resource mode by default
                resourceViewer.Visibility = Visibility.Visible;
            }));
        }
        private void AddActivityUI(Activity ac)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                var srfcBtn = new SurfaceButton {Width = activityScroller.Width, Tag = ac.Id};
                var p = new Proxy {Activity = ac, Ui = srfcBtn};
                srfcBtn.Content = ac.Name;
                srfcBtn.Click += SrfcBtnClick;
                activityStack.Children.Add(srfcBtn);

                var mtrBtn = CopyButton(srfcBtn);
                mtrBtn.Width = mtrBtn.Height = 200;
                mtrBtn.Click += SrfcBtnClick;
                activityMatrix.Children.Add(mtrBtn);
                _proxies.Add(p.Activity.Id, p);
            }));
        }
        private SurfaceButton CopyButton(SurfaceButton btn)
        {
            return new SurfaceButton
                       {
                           Content = btn.Content, 
                           Tag=btn.Tag, 
                           VerticalContentAlignment = VerticalAlignment.Center,
                           HorizontalContentAlignment = HorizontalAlignment.Center
                       };
        }
        private void SrfcBtnClick(object sender, RoutedEventArgs e)
        {
            
        }
        private void RemoveActivityUI(string id)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                for (int i = 0; i < activityStack.Children.Count;i++ )
                    if((string)((SurfaceButton)activityStack.Children[i]).Tag ==id)
                    {
                        activityStack.Children.RemoveAt(i);
                        activityMatrix.Children.RemoveAt(i);
                    }
                _proxies.Remove(id);
            }));
        }
       
        private void StartClient(WebConfiguration config)
        {
            if (_client != null)
                return;
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    activityStack.Children.Clear();
                    activityMatrix.Children.Clear();
                }));

                _client = new ActivityClient(config.Address, config.Port,
                    _device);
                _client.ActivityAdded += activityClient_ActivityAdded;
                _client.ActivityRemoved += activityClient_ActivityRemoved;
                _client.MessageReceived += _client_MessageReceived;

                foreach (var act in _client.Activities.Values)
                {
                    AddActivityUI(act as Activity);
                    PopulateResource(act as Activity);
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }

        void _client_MessageReceived(object sender, MessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case MessageType.Resource:
                    var resource = e.Message.Content as Resource;
                    if (resource != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var image = new Image();
                            using (var stream = _client.GetResource(resource))
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = stream;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                bitmap.Freeze();
                                image.Source = bitmap;
                            }
                            ContentHolder.Strokes.Clear();

                            ContentHolder.Height = image.Height;
                            ContentHolder.Background = new ImageBrush(image.Source);
                        });
                    }
                    break;

            }
        }

        private void ShowResource(object sender)
        {
            try
            {
                ContentHolder.Strokes.Clear();

                var src = ((Image)sender).Source;
                ContentHolder.Height = src.Height;
                ContentHolder.Background = new ImageBrush(src);
                ContentHolder.Tag = ((Image) sender).Tag;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void PopulateResource(Activity activity)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                ContentHolder.Strokes.Clear();
                resourceDock.Children.Clear();

                ContentHolder.Background = null;

                foreach ( var resource in activity.Resources)
                {
                    AddResource(resource);

                }
            }));
        }
        private void AddResource(Resource resource)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                var i = new Image { Tag = resource.GetType() };
                using (var stream = _client.GetResource(resource))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    i.Source = bitmap;
                }
                i.Width = i.Height = 100;
                i.Stretch = Stretch.Uniform;
                i.MouseDown += IMouseDown;
                i.TouchDown += ITouchDown;

                resourceDock.Children.Add(i);
            }));
        }

     
      
        #endregion

        #region Events Handlers
        private void ITouchDown(object sender, TouchEventArgs e)
        {
            ShowResource(sender);
        }
        private void ClientActivityAdded(object obj, ActivityEventArgs e)
        {
            //AddActivityUI(e.Activity);
            //_currentActivity = e.Activity;
        }
        private void ClientActivitySwitched(object sender, ActivityEventArgs e)
        {
            //_currentActivity = e.Activity;
            //PopulateResource(e.Activity);
        }
        private void ClientFileAdded(object sender, FileEventArgs e)
        {
            //if (e.Resource.ActivityId != _currentActivity.Id)
            //    return;
            //AddResource(e.Resource, e.LocalPath);
        }
        private void ClientActivityRemoved(object sender, ActivityRemovedEventArgs e)
        {
            RemoveActivityUI(e.Id);
        }

        private void BtnAddClick(object sender, RoutedEventArgs e)
        {
            
        }
        private void IMouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowResource(sender);
        }
        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        #endregion

        #region Helpers
        public Activity GetInitializedActivity()
        {
            var ac = new Activity
            {
                Name = "nameless",
                Description = "This is the description of the test activity - " + DateTime.Now
            };
            ac.Uri = "http://tempori.org/" + ac.Id;
            ac.Participants.Add(new User() { Email = " 	snielsen@itu.dk" });
            ac.Meta.Data = "added meta data";
            ac.Owner = _user;
            return ac;
        }
        #endregion

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {

            //PopulateResource(_currentActivity);
        }

        private void SaveToFile(Uri path, InkCanvas surface)
        {
            //get the dimensions of the ink control
            var margin = (int)surface.Margin.Left;
            var width = (int)surface.ActualWidth - margin;
            var height = (int)surface.ActualHeight - margin;
            //render ink to bitmap
            var rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
            rtb.Render(surface);
            //save the ink to a memory stream
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (var ms = new FileStream(path.LocalPath, FileMode.Create))
            {
                encoder.Save(ms);
            }
        }
        public void ExportToPng(Uri path, InkCanvas surface)
        {
            if (path == null) return;

            // Save current canvas transform
            Transform transform = surface.LayoutTransform;
            // reset current transform (in case it is scaled or rotated)
            surface.LayoutTransform = null;

            // Get the size of canvas
            Size size = new Size(surface.Width, surface.Height);
            // Measure and arrange the surface
            // VERY IMPORTANT
            surface.Measure(size);
            surface.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap =
              new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Pbgra32);
            renderBitmap.Render(surface);

            // Create a file stream for saving image
            using (FileStream outStream = new FileStream(path.LocalPath, FileMode.Create))
            {
                surface.Strokes.Save(outStream);
                // Use png encoder for our data
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                // push the rendered bitmap to it
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                // save the data to the stream
                encoder.Save(outStream);
            }

            // Restore previously saved layout
            surface.LayoutTransform = transform;
        }

        private void btnMode_Click(object sender, RoutedEventArgs e)
        {
            if (_displayMode == DisplayMode.ResourceViewer)
                _displayMode = DisplayMode.Controller;
            else
                _displayMode = DisplayMode.ResourceViewer;
            switch (_displayMode)
            {
                case DisplayMode.ResourceViewer:
                    resourceViewer.Visibility = Visibility.Visible;
                    inputView.Visibility = Visibility.Hidden;
                    controllerView.Visibility =Visibility.Hidden;
                    break;
                case DisplayMode.Controller:
                    resourceViewer.Visibility = Visibility.Hidden;
                    inputView.Visibility = Visibility.Hidden;
                    controllerView.Visibility = Visibility.Visible;
                    break;
            }
        }

        private DisplayMode _displayMode;
    }

    public enum DisplayMode
    {
        ResourceViewer,
        Controller,
        input
    }
}