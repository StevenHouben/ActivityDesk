using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Controls;

namespace ActivityDesk.Viewers
{
    /// <summary>
    /// Interaction logic for PdfViewer.xaml
    /// </summary>
    public partial class PdfViewer : ScatterViewItem
    {
         public Image Resource { get; set; }
         public ImageSource Thumbnail { get; set; }

         public PdfViewer(Image img, Image thumb)
         {

            Resource = img;
            Thumbnail = thumb.Source;

            DataContext = this;

            InitializeComponent();
        }

        private Panel _panel;
        private SurfaceScrollViewer _scroll;
         public override void OnApplyTemplate()
         {
             if(_panel !=null)
                _panel.Children.Clear();
             if (GetTemplateChild("panel") != null)
             {
                 _panel = GetTemplateChild("panel") as Panel;
                 if (_panel != null)
                 {
                     if (_panel.Children.Count == 0)
                     {
                         _panel.Children.Add(Resource);
                         _panel.Width = Resource.Width;
                         _panel.Height = Resource.Height;
                     }
                 }
             }
             base.OnApplyTemplate();
         }
    }
}
