using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NooSphere.Model;

namespace ActivityDesk.Infrastructure
{
    public class LoadedResource : INotifyPropertyChanged
    {

        public LoadedResource()
        {
            _thumbnail = new Image().Source;
            _image = new Image();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private ImageSource _thumbnail;

        public ImageSource Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                OnPropertyChanged("Thumbnail");
            }

        }

        private Image _image;

        public Image Content
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Content");
            }

        }

        private Resource _resource;

        public Resource Resource
        {
            get { return _resource; }
            set
            {
                _resource = value;
                OnPropertyChanged("Resource");
            }

        }
        public static LoadedResource EmptyResource = new LoadedResource();
    }
}
