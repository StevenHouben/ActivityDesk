using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ActivityDesk.Infrastructure;
using ActivityDesk.Viewers;
using Blake.NUI.WPF.Gestures;
using Microsoft.Surface.Presentation.Controls;
using NooSphere.Model;
using NooSphere.Model.Device;
using NooSphere.Model.Resources;

namespace ActivityDesk.Visualizer.Visualizations
{
    public abstract class BaseVisualization : TagVisualization, IResourceContainer
    {
        public event EventHandler<Point> ResourceReleased = delegate { };
        public event EventHandler<Device> Closed = delegate { };
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
       
        public event LockedEventHandler Locked = delegate { }; 
        public event EventHandler<FileResource> ReleaseResource = delegate { };
        private LoadedResource _resource = LoadedResource.EmptyResource;

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
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


        protected void OnResourceReleased(LoadedResource res, Point p)
        {
            if (ResourceReleased != null)
                ResourceReleased(res, p);
        }
        public Connection Connector { get; set; }

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
                OnPropertyChanged("Pinned");
            }

        }



        public ObservableCollection<LoadedResource> LoadedResources { get; set; }

        public Device Device { get; set; }

        public void AddResource(LoadedResource res)
        {
            Resource = res;
            LoadedResources.Add(res);
        }

        public LoadedResource LoadedResource
        {
            get;
            set;
        }

        public bool Iconized
        {
            get;
            set;
        }

        public event EventHandler<LoadedResource> Copied;

        public string ResourceType
        {
            get;
            set;
        }

        protected BaseVisualization()
        {
            AllowDrop = true;
            Resource = new LoadedResource();
            LoadedResources = new ObservableCollection<LoadedResource>();
            LoadedResources.CollectionChanged += LoadedResources_CollectionChanged;
            DataContext = this;
            Events.RegisterGestureEventSupport(this);
        }
        void LoadedResources_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UiOverflow = LoadedResources.Count > 8;
        }

        protected virtual void OnLocked()
        {
            if (Locked != null)
                Locked(this, new LockedEventArgs(VisualizedTag.Value.ToString()));
        }

        protected virtual void OnReleaseResource(FileResource resource)
        {
            if (ReleaseResource != null)
                ReleaseResource(this, resource);
        }
    }
    public delegate void LockedEventHandler(Object sender, LockedEventArgs e);
    public class LockedEventArgs
    {
        public string VisualizedTag { get; set; }
        public LockedEventArgs(string tag)
        {
            VisualizedTag = tag;
        }
    }
}
