using System;
using System.Windows.Controls;
using System.Windows.Media;
using Blake.NUI.WPF.Gestures;
using Microsoft.Surface.Presentation.Controls;
using ActivityDesk.Infrastructure;

namespace ActivityDesk.Viewers
{
    /// <summary>
    /// Interaction logic for ResourceViewer.xaml
    /// </summary>
    public partial class ResourceViewer : ScatterViewItem, IResourceContainer
    {

        public event EventHandler<LoadedResource> Copied = delegate { };

        public Image Image { get; set; }

        public LoadedResource Resource { get; set; }

        public bool Iconized { get; set; }
        public ResourceViewer(LoadedResource res)
        {
            Image = res.Content;
            Name = res.Resource.Name;
            Resource = res;

            InitializeComponent();

            Width = 1024;
            Height = 768;

            Events.RegisterGestureEventSupport(this);
        }

        private Border _border;

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("body") != null)
            {
                _border = GetTemplateChild("body") as Border;
                if (_border != null) _border.Background = new ImageBrush(Image.Source);
            }
            base.OnApplyTemplate();
        }

        private void Grid_OnDoubleTapGesture(object sender, GestureEventArgs e)
        {
            if (time == ConvertToTimestamp(DateTime.Now))
                return;
            else
                time = ConvertToTimestamp(DateTime.Now);
               
            if(Iconized)
                Copied(this, Resource);
            else
            {
                Template = (ControlTemplate)FindResource("Docked");
                Width = 100;
                Height = 100;
                Iconized = true;
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
    }
}
