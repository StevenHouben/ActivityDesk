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



        private ObservableCollection<LoadedResource> ViewerConfiguration = new ObservableCollection<LoadedResource>();
        public Tablet()
        {
            InitializeComponent();

            SurfaceColors.SetDefaultApplicationPalette(new LightSurfacePalette());

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
        private LoadedResource FromResourceAndBitmapSource(Resource res,BitmapSource source)
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

            }));
        }
        private void AddActivityUI(Activity ac)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                var srfcBtn = new SurfaceButton {Width = activityScroller.Width, Tag = ac.Id};
                var p = new Proxy {Activity = ac, Ui = srfcBtn};
                srfcBtn.Content = ac.Name;
                srfcBtn.Background = Brushes.Transparent;
                srfcBtn.Foreground = Brushes.Black;
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
                Tag = btn.Tag,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }
        private void SrfcBtnClick(object sender, RoutedEventArgs e)
        {
            var sb = sender as SurfaceButton;
            if (sb.Tag == null)
                return;

            SwitchActivity(sb.Tag as string);
        }

        private void SwitchActivity(string id)
        {
            if(_displayMode == DisplayMode.Controller)
                PopulateResources(id);
            else
                _client.SendMessage(MessageType.ActivityChanged, id);
        }

        private void PopulateResources(string id)
        {
            if (!_client.Activities.ContainsKey(id)) return;

            var activity = _client.Activities[id];

            ClearResources();
            foreach (var res in activity.Resources)
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

                ShowResource(loadedResource.Content);

                LoadedResources.Add(loadedResource);
            }
        }

        private void RemoveActivityUI(string id)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                for (int i = 0; i < activityStack.Children.Count; i++)
                    if ((string) ((SurfaceButton) activityStack.Children[i]).Tag == id)
                    {
                        activityStack.Children.RemoveAt(i);
                        //activityMatrix.Children.RemoveAt(i);
                    }
            }));
        }

        private void StartClient(WebConfiguration config)
        {
            if (_client != null)
                return;
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() => activityStack.Children.Clear()));

                _client = new ActivityClient(config.Address, config.Port,
                    _device);
                _client.ActivityAdded += activityClient_ActivityAdded;
                _client.ActivityRemoved += activityClient_ActivityRemoved;
                _client.MessageReceived += _client_MessageReceived;
                _client.DeviceRemoved += _client_DeviceRemoved;


                foreach (var act in _client.Activities.Values)
                {
                    AddActivityUI(act as Activity);

                    LoadResources(act);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }
        private void LoadResources(IActivity act)
        {
            foreach (var res in act.Resources)
            {
                Task.Factory.StartNew(() =>
                {
                    _maxItemsCount ++;
                    LoadBitmap(res,_client.GetResource(res));
                });
            }
        }
        private void LoadBitmap(Resource res,Stream s)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = s;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            s.Close();
            s.Dispose();
            Dispatcher.Invoke(DispatcherPriority.Send, new Action<Resource,BitmapImage>(AddLoadedResourceFromCachedBitmap),res,bitmap);
        }
        private void AddLoadedResourceFromCachedBitmap(Resource resource,BitmapImage img)
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
                        Dispatcher.Invoke(ClearResources);
                    }
                    break;
                    case MessageType.ActivityChanged:

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

                var height = 9240; // (int)bitmapsource.Height;
                var width = 502; //(int)bitmapsource.Width;
                var imageHeight = height/10;
                var runs = height/imageHeight;

                for (var i = 0; i < runs; i++)
                {
                    var croppedImage = new Image {Width = width, Height = imageHeight, Stretch = Stretch.Fill};
                    var rect = new Int32Rect(0, i*imageHeight, width, imageHeight);

                    var cb = new CroppedBitmap(
                        (BitmapSource) img.Source, rect
                        );

                    croppedImage.Source = cb;
                    croppedImage.Width = 800;
                    croppedImage.Height = 1056;
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