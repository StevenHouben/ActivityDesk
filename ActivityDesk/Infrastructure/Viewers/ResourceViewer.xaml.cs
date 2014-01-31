using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Blake.NUI.WPF.Gestures;
using Microsoft.Surface.Presentation.Controls;
using NooSphere.Model;
using ActivityDesk.Infrastructure;

namespace ActivityDesk.Viewers
{
    /// <summary>
    /// Interaction logic for ResourceViewer.xaml
    /// </summary>
    public partial class ResourceViewer : ScatterViewItem, IResourceContainer
    {
        public Image Image { get; set; }

        public LoadedResource Resource { get; set; }

        public bool Iconized { get; set; }
        public ResourceViewer(LoadedResource res)
        {
            Image = res.Content;
            Name = res.Resource.Name;
            Resource = res;

            InitializeComponent();

            Events.RegisterGestureEventSupport(this);
        }
        private void OnDoubleTapGesture(object sender, GestureEventArgs e)
        {
            Template = (ControlTemplate)FindResource("Docked");
            Width = 100;
            Height = 100;
            Iconized = true;   
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
    }
}
