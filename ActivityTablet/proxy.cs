using System;
using System.ComponentModel;
using NooSphere.Model;

namespace ActivityTablet
{
    public class Proxy: INotifyPropertyChanged
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

        private Uri _url;
        public Uri Url
        {
            get { return _url; }
            set
            {
                _url = value;
                OnPropertyChanged("Url");
            }
        }

        public Activity Activity { get; set; }

    }
}
