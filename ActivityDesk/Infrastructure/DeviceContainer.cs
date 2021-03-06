﻿using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Windows;
using ActivityDesk.Viewers;
using ActivityDesk.Visualizer.Visualizations;
using NooSphere.Model.Device;

namespace ActivityDesk.Infrastructure
{
    public class DeviceContainer
    {
        public delegate void ResourceReleasedHandler(Device sender, ResourceReleasedEventArgs e);

        public event ResourceReleasedHandler ResourceReleased; 
        private DeviceThumbnail _deviceThumbnail;
        private BaseVisualization _deviceVisualization;
        private bool _connected;


        private bool visible;
        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                UpdateVisbility(visible);
            }
        }

        private void UpdateVisbility(bool vis)
        {
            if(_deviceThumbnail !=null)
                _deviceThumbnail.Visibility = vis ? Visibility.Visible : Visibility.Hidden;
            if(_deviceVisualization != null)
                _deviceVisualization.Visibility = vis ? Visibility.Visible : Visibility.Hidden;
        }


        public IResourceContainer ActiveDevice
        {
            get
            {
                if (VisualStyle == DeviceVisual.Thumbnail)
                    return _deviceThumbnail;
                return _deviceVisualization;
            }
        }

        public ObservableCollection<LoadedResource> LoadedResources
        {
            get
            {
                if (VisualStyle == DeviceVisual.Thumbnail)
                    return _deviceThumbnail.LoadedResources;
                return _deviceVisualization.LoadedResources;
            }
        }



        public DeviceThumbnail DeviceThumbnail
        {
            get { return _deviceThumbnail; }
            set
            {
                _deviceThumbnail = value;
                _deviceThumbnail.ResourceReleased += resourceReleased;
            }
        }

        public bool Intersecting { get; set; }

        public Device Device { get; set; }

        public BaseVisualization DeviceVisualization
        {
            get { return _deviceVisualization; }
            set
            {
                _deviceVisualization = value;
                _deviceVisualization.ResourceReleased += resourceReleased;
            }
        }

        public DeviceVisual VisualStyle { get; set; }

        public string TagValue { get; set; }

        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                if (DeviceThumbnail != null)
                    DeviceThumbnail.Connected = _connected;
                if (DeviceVisualization != null)
                    DeviceVisualization.Connected = _connected;
            }

        }

        private bool _pinned;
        public bool Pinned
        {
            get { return _pinned; }
            set
            {
                _pinned = value;
                if (DeviceThumbnail != null)
                    DeviceThumbnail.Pinned = _pinned;
                if (DeviceVisualization != null)
                    DeviceVisualization.Pinned = _pinned;
            }

        }

        public DeviceContainer()
        {
            Visible = true;
            Pinned = false;
            VisualStyle=DeviceVisual.Visualisation;
            Device = new Device();
        }

        internal void Clear()
        {
            if (DeviceThumbnail != null)
            {
                DeviceThumbnail.LoadedResources.Clear();
                DeviceThumbnail.LoadedResource = null;
            }


            if (DeviceVisualization != null)
            {
                DeviceVisualization.LoadedResources.Clear();
                DeviceVisualization.Resource = null;
            }

        }

        internal void Invalidate()
        {
            Connected = Connected;
            Pinned = Pinned;
            Visible = Visible;
        }

        void resourceReleased(object sender, System.Windows.Point e)
        {
            if (ResourceReleased != null)
                ResourceReleased(Device, new ResourceReleasedEventArgs()
                {
                    Device = Device,
                    LoadedResource = sender as LoadedResource,
                    Position = e
                });
        }

        internal void AddResource(LoadedResource loadedResource)
        {
            if (VisualStyle == DeviceVisual.Thumbnail)
                 _deviceThumbnail.AddResource(loadedResource);
            else _deviceVisualization.AddResource(loadedResource); ;
        }
    }

    public enum DeviceVisual
    {
        Thumbnail,
        Visualisation
    }

}
