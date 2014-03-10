using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using NooSphere.Model;

namespace ActivityTablet
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

        private bool _selected;

        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged("Selected");
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

        private FileResource _resource;

        public FileResource Resource
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
