using System;
using System.Windows.Controls;
using System.Windows.Media;
using ActivityDesk.Infrastructure;
using Blake.NUI.WPF.Gestures;
using Microsoft.Surface.Presentation.Controls;

namespace ActivityDesk.Viewers
{
    /// <summary>
    /// Interaction logic for PdfViewer.xaml
    /// </summary>
    public partial class PdfViewer : ScatterViewItem,IResourceContainer
    {
         public LoadedResource Resource { get; set; }
         public ImageSource Thumbnail { get; set; }

         public event EventHandler<LoadedResource> Copied = delegate { };

        public bool Iconized { get; set; }

        public string ResourceType { get; set; }

         public PdfViewer(LoadedResource loadedResource)
         {

            Resource = loadedResource;
            Thumbnail = loadedResource.Thumbnail;

            DataContext = this;

            InitializeComponent();

            Width = 1024;
            Height = 768;
            Events.RegisterGestureEventSupport(this);


        }

        private Panel _panel;
        private SurfaceScrollViewer _scroll;
        private Border _border;
         public override void OnApplyTemplate()
         {
             if (_panel != null)
                 _panel.Children.Clear();
             if (GetTemplateChild("panel") != null)
             {
                 _panel = GetTemplateChild("panel") as Panel;
                 if (_panel != null)
                 {
                     if (_panel.Children.Count == 0)
                     {
                         _panel.Children.Add(Resource.Content);
                         _panel.Width = Resource.Content.Width;
                         _panel.Height = Resource.Content.Height;
                     }
                 }
             }
             if (GetTemplateChild("body") != null)
             {
                 _border = GetTemplateChild("body") as Border;
                 if (_border != null) _border.Background = new ImageBrush(Thumbnail);
             }
             base.OnApplyTemplate();
         }
         private void Grid_OnDoubleTapGesture(object sender, GestureEventArgs e)
         {
             if (time == ConvertToTimestamp(DateTime.Now))
                 return;
                 time = ConvertToTimestamp(DateTime.Now);

             if (Iconized)
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
