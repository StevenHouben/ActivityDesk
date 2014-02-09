using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ActivityDesk.Infrastructure;
using Blake.NUI.WPF.Gestures;
using ActivityDesk.Viewers;

namespace ActivityDesk
{

    public partial class DeviceThumbnail : INotifyPropertyChanged, IResourceContainer
    {

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        internal event EventHandler<Point> ResourceReleased = delegate { };

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

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
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

        private bool _interruptable;
        public bool Interruptable
        {
            get { return _interruptable; }
            set
            {
                _interruptable = value;
                OnPropertyChanged("Interruptable");
            }

        }

        private bool _pinned;
        public bool Pinned
        {
            get { return _pinned; }
            set
            {
                _pinned = value;
                OnPropertyChanged("Connected");
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

        public void AddResource(LoadedResource res)
        {
            Resource = res;
            LoadedResources.Add(res);
        }

        public bool Iconized { get; set; }

        public event EventHandler<string> Closed = delegate { };

        public string VisualizedTag { get; set; }

        public DeviceThumbnail(string visualizedTagValue)
        {
            VisualizedTag = visualizedTagValue;

            Resource = new LoadedResource();
            LoadedResources = new ObservableCollection<LoadedResource>();
            LoadedResources.CollectionChanged += LoadedResources_CollectionChanged;

            InitializeComponent();

            DataContext = this;
            Events.RegisterGestureEventSupport(this);

	        CanScale = false;
	        CanRotate = false;

            Width = 300;
            Height = 150;

            Template = (ControlTemplate)FindResource("Floating");

            Interruptable = false;
        }

        void LoadedResources_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UiOverflow = LoadedResources.Count > 3;
        }

        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
	    {
	        if(Closed != null)
                Closed(this, VisualizedTag);
	    }

	    private void OnDoubleTapGesture(object sender, GestureEventArgs e)
	    {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var point = fe.TranslatePoint(new Point(0, 0), Parent as FrameworkElement);

            var res = fe.DataContext as LoadedResource;

            if (res == null) return;

            LoadedResources.Remove(res);

            if (ResourceReleased != null)
                ResourceReleased(res, point);

            if (Resource == res)
                Resource = LoadedResources.Count != 0 ? LoadedResources.First() : LoadedResource.EmptyResource;

	    }


        public event EventHandler<LoadedResource> Copied;

        public string ResourceType
        {
            get; set;
        }
    }
}