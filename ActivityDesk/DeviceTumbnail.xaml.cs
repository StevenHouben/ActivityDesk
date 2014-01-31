using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActivityDesk.Infrastructure;
using Blake.NUI.WPF.Gestures;
using NooSphere.Model.Device;
using ActivityDesk.Viewers;

namespace ActivityDesk
{
    public partial class DeviceTumbnail : IResourceContainer,INotifyPropertyChanged
	{

        public event PropertyChangedEventHandler PropertyChanged = delegate { }; 
        public event EventHandler<LoadedResource> ResourceReleased = delegate { }; 

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private LoadedResource _resource;

        public LoadedResource Resource
        {
            get { return _resource; }
            set
            {
                if (_resource != null)
                {
                    if (ResourceReleased != null)
                        ResourceReleased(this, _resource);
                }
                _resource = value;
                OnPropertyChanged("Resource");
            }

        }

        public bool Iconized { get; set; }

	    public event EventHandler<Device> Closed = delegate { };

	    public Device Device { get; private set; }

	    public DeviceTumbnail(Device device)
	    {
	        Device = device;
	        Name = device.Name;
            InitializeComponent();

            DataContext = this;
            Events.RegisterGestureEventSupport(this);

	        CanScale = false;
	        CanRotate = false;
	    }

	    private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
	    {
	        if(Closed != null)
                Closed(Device, Device);
	    }

	    private void OnDoubleTapGesture(object sender, GestureEventArgs e)
	    {
	        if (Resource == null) return;
	        if (ResourceReleased != null)
	            ResourceReleased(this, Resource);
	        Resource = null;
	    }
	}
}