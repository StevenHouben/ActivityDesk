
using System;
using System.Windows.Controls;
using ActivityDesk.Infrastructure;
using ActivityDesk.Visualizer.Visualizations;
using NooSphere.Model.Device;

namespace ActivityDesk.Viewers
{
    public class DeviceContainer
    {
        public delegate void ResourceReleasedHandler(Device sender, ResourceReleasedEventArgs e);

        public event ResourceReleasedHandler ResourceReleased; 

        private DeviceThumbnail _deviceThumbnail;

        public bool Intersecting { get; set; }

        public DeviceThumbnail DeviceThumbnail
        {
            get { return _deviceThumbnail; }
            set
            {
                _deviceThumbnail = value;
                _deviceThumbnail.ResourceReleased += resourceReleased;
            }
        }

     
        public Device Device { get; set; }


        private VisualizationTablet _deviceVisualization;

        public VisualizationTablet DeviceVisualization
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


        private bool _connected;
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
            Pinned = false;
            VisualStyle=DeviceVisual.Visualisation;
            Device = new Device();
        }

        internal void Clear()
        {
            if (DeviceThumbnail != null)
            {
                DeviceThumbnail.LoadedResources.Clear();
                DeviceThumbnail.Resource = null;
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
    }

    public enum DeviceVisual
    {
        Thumbnail,
        Visualisation
    }

}
