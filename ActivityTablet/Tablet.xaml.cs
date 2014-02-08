using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
using NooSphere.Infrastructure.Helpers;
using NooSphere.Infrastructure.Web;
using NooSphere.Model;
using NooSphere.Model.Device;

namespace ActivityTablet
{
    public partial class Tablet
    {
        #region Private Members
        private Device _device;
        private readonly Dictionary<SurfaceButton, Proxy> _proxies = new Dictionary<SurfaceButton, Proxy>();
        private ActivityClient _client;

        public ObservableCollection<LoadedResource> LoadedResources { get; set; }

        public Dictionary<string, LoadedResource> ResourceCache = new Dictionary<string, LoadedResource>();

        #endregion

        #region Constructor
        public Tablet()
        {
            InitializeComponent();

            LoadedResources = new ObservableCollection<LoadedResource>(); 

            DataContext = this;

            BuildUI();


            _device = new Device()
            {
                DeviceType = DeviceType.Tablet,
                TagValue = "170"
            };

            RunDiscovery();
        }
        #endregion

        #region Private Methods

        private LoadedResource FromResource(Resource res)
        {
            var loadedResource = new LoadedResource();

            res.FileType = "IMG";

            var image = new Image();

         
            using (var stream = _client.GetResource(res))
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

        private void RunDiscovery()
        {
           var disco = new DiscoveryManager();

            disco.DiscoveryAddressAdded += (sender, e) =>
            {
                if (e.ServiceInfo.Code != "9865") return;
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
                srfcBtn.Tag = ac.Id;
                srfcBtn.Width = 180;
                activityStack.Children.Add(srfcBtn);

                var mtrBtn = CopyButton(srfcBtn);
                mtrBtn.Width = mtrBtn.Height = 200;
                mtrBtn.Click += SrfcBtnClick;
                mtrBtn.Tag = ac.Id;
                //activityMatrix.Children.Add(mtrBtn);
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
            var sb = sender as SurfaceButton;
            if (sb.Tag == null)
                return;

            _client.SendMessage(MessageType.ActivityChanged,sb.Tag as string) ;
        }
        private void RemoveActivityUI(string id)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                for (int i = 0; i < activityStack.Children.Count;i++ )
                    if((string)((SurfaceButton)activityStack.Children[i]).Tag ==id)
                    {
                        activityStack.Children.RemoveAt(i);
                        //activityMatrix.Children.RemoveAt(i);
                    }
            }));
        }
       
        void StartClient(WebConfiguration config)
        {
            if (_client != null)
                return;
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    activityStack.Children.Clear();
                    //activityMatrix.Children.Clear();
                }));

                _client = new ActivityClient(config.Address, config.Port,
                    _device);
                _client.ActivityAdded += activityClient_ActivityAdded;
                _client.ActivityRemoved += activityClient_ActivityRemoved;
                _client.MessageReceived += _client_MessageReceived;
                _client.DeviceRemoved += _client_DeviceRemoved;

                foreach (var act in _client.Activities.Values)
                {
                    AddActivityUI(act as Activity);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }

        void _client_DeviceRemoved(object sender, DeviceRemovedEventArgs e)
        {
            if(e.Id == _device.Id)
                Environment.Exit(0);
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
                            LoadedResource loadedResource;
                            if (ResourceCache.ContainsKey(resource.Id))
                            {
                                loadedResource = ResourceCache[resource.Id];
                            }
                            else
                            {
                                loadedResource = FromResource(resource);
                                ResourceCache.Add(resource.Id,loadedResource);
                            }

                            ShowResource(loadedResource.Content);

                            LoadedResources.Add(loadedResource);
                        });
                    }
                    break;
                    case MessageType.ResourceRemove:
                    var resourceToRemove = e.Message.Content as Resource;
                    if (resourceToRemove != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadedResource rToR = null;
                            foreach (var res in LoadedResources)
                            {
                                if (res.Resource.Id == resourceToRemove.Id)
                                    rToR = res;
                            }
                            if (rToR != null)
                            {
                                LoadedResources.Remove(rToR);
                            }
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadedResources.Clear();
                            ContentHolder.Background = Brushes.White;
                        });
                    }
                    break;

            }
        }

        private void ShowResource(Image img)
        {
            try
            {
                ContentHolder.Strokes.Clear();

                var src = img.Source;
                ContentHolder.Height = src.Height;
                ContentHolder.Background = new ImageBrush(src);
                ContentHolder.Tag = img.Tag;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
     
        #endregion

        #region Events Handlers
        private void ITouchDown(object sender, TouchEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var res = fe.DataContext as LoadedResource;
            if (res == null) return;
 
            ShowResource(res.Content);
        }
        private void IMouseDown(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var res = fe.DataContext as LoadedResource;
            if (res == null) return;

            ShowResource(res.Content);
        }
        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            _client.RemoveDevice(_device.Id);
        }
        #endregion

        private void btnMode_Click(object sender, RoutedEventArgs e)
        {
            _displayMode = _displayMode == DisplayMode.ResourceViewer ? DisplayMode.Controller : DisplayMode.ResourceViewer;
            switch (_displayMode)
            {
                case DisplayMode.ResourceViewer:
                    resourceViewer.Visibility = Visibility.Visible;

                    break;
                case DisplayMode.Controller:
                    resourceViewer.Visibility = Visibility.Hidden;
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