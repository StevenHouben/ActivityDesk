using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Controls;

namespace ActivityDesk
{
    /// <summary>
    /// Interaction logic for ResourceViewer.xaml
    /// </summary>
    public partial class ResourceViewer : ScatterViewItem
    {
        public Image Resource { get; set; }
        public ResourceViewer(Image img,string name)
        {
            InitializeComponent();
            this.Resource = img;
            this.Name = name;
        }

        Border _border;
        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("body") != null)
            {
                _border = GetTemplateChild("body") as Border;
                if (_border != null) _border.Background = new ImageBrush(Resource.Source);
            }
            base.OnApplyTemplate();
        }
    }
}
