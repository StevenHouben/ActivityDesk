using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ActivityDesk.Infrastructure;
using ActivityDesk.Viewers;
using Blake.NUI.WPF.Gestures;
using NooSphere.Model.Device;

namespace ActivityDesk.Visualizer.Visualizations
{
    public partial class VisualizationTablet : BaseVisualization, INotifyPropertyChanged,IResourceContainer
    {
        public event EventHandler<Device> Closed = delegate { };
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event EventHandler<Point> ResourceReleased = delegate { };


        private bool _uiOverflow;
        public bool UiOverflow
        {
            get { return _uiOverflow; }
            set
            {
                _uiOverflow = value;
                OnPropertyChanged("UiOverflow");
            }

        }

        public Connection Connector { get; set;
        }

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        private LoadedResource _resource = LoadedResource.EmptyResource;

        public ObservableCollection<LoadedResource> LoadedResources { get; set; }

        public LoadedResource Resource
        {
            get { return _resource; }
            set
            {
                _resource = value;
                OnPropertyChanged("Resource");
            }

        }

        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                OnPropertyChanged("Connected");
            }

        }

        private bool _pinned;
        public bool Pinned
        {
            get { return _pinned; }
            set
            {
                _pinned = value;
                OnPropertyChanged("Pinned");
            }

        }

        public void AddResource(LoadedResource res)
        {
            Resource = res;
            LoadedResources.Add(res);
        }


         public Device Device { get; private set; }

         public VisualizationTablet()
	    {
            Resource = new LoadedResource();
            LoadedResources = new ObservableCollection<LoadedResource>();
            LoadedResources.CollectionChanged += LoadedResources_CollectionChanged;

            InitializeComponent();

            DataContext = this;
            Events.RegisterGestureEventSupport(this);

	    }

         void LoadedResources_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
         {
             UiOverflow = LoadedResources.Count > 8;
         }
        
        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
        {
            OnLocked();
        }

        private void OnDoubleTapGesture(object sender, GestureEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var frameworkElement = Parent as FrameworkElement;
            if (frameworkElement == null) return;
            var point = fe.TranslatePoint(new Point(0, 0), frameworkElement.Parent as FrameworkElement);

            var res = fe.DataContext as LoadedResource;

            if (res == null) return;

            LoadedResources.Remove(res);

            if (ResourceReleased != null)
                ResourceReleased(res, point);

            if (Resource == res)
                Resource = LoadedResources.Count != 0 ? LoadedResources.First() : LoadedResource.EmptyResource;
        }

        public LoadedResource LoadedResource
        {
            get; set;
        }

        public bool Iconized
        {
            get; set;
        }

        public event EventHandler<LoadedResource> Copied;

        public string ResourceType
        {
            get; set;
        }

        private void Grid_OnTouchDown(object sender, TouchEventArgs e)
        {
            if (IsDoubleTap(e))
                OnDoubleTouchDown(sender);
        }
        private readonly Stopwatch _doubleTapStopwatch = new Stopwatch();
        private Point _lastTapLocation;

        public event EventHandler DoubleTouchDown;

        protected virtual void OnDoubleTouchDown(object sender)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var frameworkElement = Parent as FrameworkElement;
            if (frameworkElement == null) return;
            var point = fe.TranslatePoint(new Point(0, 0), frameworkElement.Parent as FrameworkElement);

            var res = fe.DataContext as LoadedResource;

            if (res == null) return;

            LoadedResources.Remove(res);

            if (ResourceReleased != null)
                ResourceReleased(res, point);

            if (Resource == res)
                Resource = LoadedResources.Count != 0 ? LoadedResources.First() : LoadedResource.EmptyResource;
        }

        private bool IsDoubleTap(TouchEventArgs e)
        {
            TimeSpan elapsed = _doubleTapStopwatch.Elapsed;
            _doubleTapStopwatch.Restart();
            bool tapsAreCloseInTime = (elapsed != TimeSpan.Zero && elapsed < TimeSpan.FromSeconds(0.7));

            return tapsAreCloseInTime;
        }
    }
}