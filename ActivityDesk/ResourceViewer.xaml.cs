using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
