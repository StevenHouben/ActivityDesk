using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Palettes;
using NooSphere.Infrastructure;
using NooSphere.Infrastructure.ActivityBase;
using NooSphere.Infrastructure.Discovery;
using NooSphere.Infrastructure.Helpers;
using NooSphere.Infrastructure.Web;
using NooSphere.Model;
using NooSphere.Model.Device;

namespace ActivityTablet
{
    public partial class Tablet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private bool _master;
        public bool Master
        {
            get { return _master; }
            set
            {
                _master = value;
                OnPropertyChanged("Master");
            }
        }

        private int _maxItemsCount;
        private int _preloadedCount;
        private readonly Device _device;
        private ActivityClient _client;
        private DisplayMode _displayMode = DisplayMode.ResourceViewer;
        public ObservableCollection<LoadedResource> LoadedResources { get; set; }

        public Dictionary<string, LoadedResource> ResourceCache = new Dictionary<string, LoadedResource>();

        private Activity _selectedActivity;

        public ObservableCollection<Proxy> Activities { get; set; }

        public Tablet()
        {
            InitializeComponent();

            Activities = new ObservableCollection<Proxy>();

            SurfaceColors.SetDefaultApplicationPalette(new LightSurfacePalette());

            LoadedResources = new ObservableCollection<LoadedResource>();

            DataContext = this;

            BuildUI();

            _device = new Device()
            {
                DeviceType = DeviceType.Tablet,
                TagValue = "170"
            };

            btnMode.IsEnabled = false;

            StartClient(new WebConfiguration("10.6.6.121", 8000));
        }
        private LoadedResource FromResource(FileResource res)
        {
            var loadedResource = new LoadedResource();

            res.FileType = "IMG";

            var image = new Image();


            using (var stream = _client.GetFileResource(res))
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
        private LoadedResource FromResourceAndBitmapSource(FileResource res, BitmapSource source)
        {
            var loadedResource = new LoadedResource();

            res.FileType = "IMG";

            var image = new Image {Source = source};
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
        private void activityClient_ActivityRemoved(object sender, ActivityRemovedEventArgs e)
        {
            RemoveActivityUI(e.Id);
        }
        private void activityClient_ActivityAdded(object sender, ActivityEventArgs e)
        {
            AddActivityUI(e.Activity as Activity);
        }
        void _client_ActivityChanged(object sender, ActivityEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                foreach (var prox in Activities.Where(prox => prox.Activity.Id == e.Activity.Id))
                {
                    if(e.Activity.Logo != null)
                            prox.Url = _client.GetFileResourceUri(e.Activity.Logo);

                     prox.Activity = (Activity)e.Activity;


                     //if (_selectedActivity == null)
                     //   PopulateResources(prox.Activity.Id);
                     //else if (_selectedActivity.Id == prox.Activity.Id && _displayMode == DisplayMode.Controller)
                     //    PopulateResources(_selectedActivity.Id);
                }

            }));
        }
        private void BuildUI()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                resourceViewer.Visibility = Visibility.Hidden;

                Width = 1280;
                Height = 800;
                MaxHeight = 800;
                MaxWidth = 1280;


                if (SystemParameters.PrimaryScreenWidth > 1400)
                    WindowStyle = WindowStyle.ToolWindow;
                else
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                }
                //Show resource mode by default
                resourceViewer.Visibility = Visibility.Visible;

                Output.Text = "";
                Output.Height = 0;
            }));
        }
        private void AddActivityUI(Activity ac)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                var prox = new Proxy()
                {
                    Activity = ac,
                    Url = ac.Logo != null ? _client.GetFileResourceUri(ac.Logo) : new Uri("pack://application:,,,/Images/activity.PNG")
                };
                Activities.Add(prox);
            }));
        }
        private SurfaceButton CopyButton(SurfaceButton btn)
        {
            return new SurfaceButton
            {
                Content = btn.Content,
                Tag = btn.Tag,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }
        private void SrfcBtnClick(object sender, RoutedEventArgs e)
        {
            var sb = sender as SurfaceButton;

            var prox = sb.DataContext as Proxy;
            if (prox == null)
                return;

            _selectedActivity = prox.Activity;

            foreach (var proxies in Activities)
            {
                proxies.Selected = _selectedActivity.Id == proxies.Activity.Id;
            }

            SwitchActivity(prox.Activity.Id);
        }

        private void SwitchActivity(string id)
        {
            if (_displayMode == DisplayMode.Controller)
                PopulateResources(id);
            else
            {
                _client.SendMessage(MessageType.ActivityChanged, id);
                ClearResources();
            }
        }

        private void PopulateResources(string id)
        {
            if (!_client.Activities.ContainsKey(id)) return;

            var activity = _client.Activities[id];

            ClearResources();
            foreach (var res in activity.FileResources)
            {
                LoadedResource loadedResource;
                if (ResourceCache.ContainsKey(res.Id))
                {
                    loadedResource = ResourceCache[res.Id];
                }
                else
                {
                    loadedResource = FromResource(res);
                    ResourceCache.Add(res.Id, loadedResource);
                }

                foreach (var lr in LoadedResources)
                {
                    lr.Selected = false;
                }
                loadedResource.Selected = true;

                LoadedResources.Add(loadedResource);
                lastShownResource = loadedResource;
                ShowResource(loadedResource.Content);

            }
        }

        private LoadedResource lastShownResource = new LoadedResource();

        private void RemoveActivityUI(string id)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                Proxy proxToRemove = null;
                foreach (var prox in Activities.Where(prox => prox.Activity.Id == id))
                    proxToRemove = prox;

                if (proxToRemove == null) return;

                Activities.Remove(proxToRemove);

                ClearResources();

            }));
        }

        private void StartClient(WebConfiguration config)
        {
            if (_client != null)
                return;
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() => Activities.Clear()));


                _client = new ActivityClient(config.Address, config.Port,
                    _device);
                _client.ActivityAdded += activityClient_ActivityAdded;
                _client.ActivityChanged += _client_ActivityChanged;
                _client.ActivityRemoved += activityClient_ActivityRemoved;
                _client.FileResourceAdded += _client_ResourceAdded;
                _client.MessageReceived += _client_MessageReceived;
                _client.DeviceRemoved += _client_DeviceRemoved;



                foreach (var act in _client.Activities.Values)
                {

                    Dispatcher.Invoke(() =>
                    {
                        btnMode.IsEnabled = true;
                        AddActivityUI(act as Activity);
                        LoadResources(act);

                    });
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }

        void _client_ResourceAdded(object sender, FileResourceEventArgs e)
        {
             Task.Factory.StartNew(() =>
                {
                    _maxItemsCount ++;
                    LoadBitmap(e.Resource,_client.GetFileResource(e.Resource));
                });
        }

        private void LoadResources(IActivity act)
        {
            foreach (var res in act.FileResources)
            {
                Task.Factory.StartNew(() =>
                {
                    _maxItemsCount ++;
                    LoadBitmap(res,_client.GetFileResource(res));
                });
            }
            if ( _maxItemsCount==0)
            {
                Output.Text = "";
                Output.Height = 0;
            }
            else
            {
                Output.Height = 25;
            }
        }
        private void LoadBitmap(FileResource res, Stream s)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = s;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            s.Close();
            s.Dispose();
            Dispatcher.Invoke(DispatcherPriority.Send, new Action<FileResource, BitmapImage>(AddLoadedResourceFromCachedBitmap), res, bitmap);
        }
        private void AddLoadedResourceFromCachedBitmap(FileResource resource, BitmapImage img)
        {
            if (!ResourceCache.ContainsKey(resource.Id))
                ResourceCache.Add(resource.Id, FromResourceAndBitmapSource(resource, img));

            Output.Text = "Cached " + resource.Id +" -- "+ _preloadedCount++ +"/"+_maxItemsCount;

            if (_preloadedCount == _maxItemsCount)
            {
                Output.Text = "";
                Output.Height = 0;
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
                    var resource = e.Message.Content as FileResource;
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

                            lastShownResource = loadedResource;
                            ShowResource(loadedResource.Content);

                            LoadedResources.Add(loadedResource);
                        });
                    }
                    break;
                    case MessageType.ResourceRemove:
                    var resourceToRemove = e.Message.Content as FileResource;
                    if (resourceToRemove != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadedResource rToR = null;
                            foreach (var res in LoadedResources.Where(res => res.Resource.Id == resourceToRemove.Id))
                            {
                                rToR = res;
                            }
                            if (rToR != null)
                            {
                                LoadedResources.Remove(rToR);
                                if (lastShownResource.Resource.Id == rToR.Resource.Id)
                                {
                                    ContentHolder.Background = Brushes.White;
                                    Canvas.Strokes.Clear();
                                    ContentHolder.Children.Clear();
                                }

                            }
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(ClearResources);
                    }
                    break;
                    case MessageType.ActivityChanged:
                        _selectedActivity = e.Message.Content as Activity;
                    break;

            }
        }

        private void ShowResource(Image img)
        {
             Canvas.Strokes.Clear();
             ContentHolder.Children.Clear();
             ContentHolder.Height = img.Source.Height;

            if (img.Source.Height > 1500)
            {
                var height = (int)img.Source.Height;
                var width = (int)img.Source.Width;
                var imageHeight = height/10;
                var runs = height/imageHeight;

                for (var i = 0; i < runs; i++)
                {
                    var croppedImage = new Image {Width = width, Height = imageHeight};
                    var rect = new Int32Rect(0, i*imageHeight, width, imageHeight);

                    var cb = new CroppedBitmap(
                        (BitmapSource) img.Source, rect
                        );

                    croppedImage.Source = cb;
                    ContentHolder.Children.Add(croppedImage);
                }
            }
            else
            {
                ContentHolder.Children.Add(img);
            }
            resourceViewScroller.ScrollToHome();

        }
        private void ITouchDown(object sender, TouchEventArgs e)
        {
            IMouseDown(sender,e);
        }
        private void IMouseDown(object sender, EventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var res = fe.DataContext as LoadedResource;
            if (res == null) return;

            lastShownResource = res;
            ShowResource(res.Content);

            foreach (var lr in LoadedResources)
                lr.Selected = lr.Resource.Id == res.Resource.Id;
        }
        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            if(_client == null)
                Environment.Exit(0);
           _client.RemoveDevice(_device.Id);
           Environment.Exit(0);
        }
        private void btnMode_Click(object sender, RoutedEventArgs e)
        {
            if (time == ConvertToTimestamp(DateTime.Now))
                return;
            else
                time = ConvertToTimestamp(DateTime.Now);

            _displayMode = _displayMode == DisplayMode.ResourceViewer ? DisplayMode.Controller : DisplayMode.ResourceViewer;
            switch (_displayMode)
            {
                case DisplayMode.ResourceViewer:
                    ClearResources();
                    _client.SendMessage(MessageType.Control, "slave");
                    Master = false;
                    break;
                case DisplayMode.Controller:
                    Master = true;
                    PopulateResources(_client.Activities.First().Value.Id);
                    _client.SendMessage(MessageType.Control, "disconnected");
                    break;
            }
        }

        private static int time;
        private int ConvertToTimestamp(DateTime value)
        {
            //create Timespan by subtracting the value provided from
            //the Unix Epoch
            TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

            //return the total seconds (which is a UNIX timestamp)
            return (int)span.TotalSeconds;
        }

        private void ClearResources()
        {
            LoadedResources.Clear();
            ContentHolder.Background = Brushes.White;
            Canvas.Strokes.Clear();
            ContentHolder.Children.Clear();
        }

    }

    public enum DisplayMode
    {
        ResourceViewer,
        Controller,
        input
    }
}