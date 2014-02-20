using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ActivityDesk.Helper;
using ActivityDesk.Infrastructure;
using Blake.NUI.WPF.Gestures;
using Microsoft.Surface.Presentation.Controls;
using NooSphere.Model;

namespace ActivityDesk.Viewers
{
    public partial class TouchWindow : ScatterViewItem, IResourceContainer
	{
		#region Attached Property InitialSizeRequest
		public static Size GetInitialSizeRequest(DependencyObject obj)
		{
			return (Size)obj.GetValue(InitialSizeRequestProperty);
		}

		public static void SetInitialSizeRequest(DependencyObject obj, Size value)
		{
			obj.SetValue(InitialSizeRequestProperty, value);
		}

		// Using a DependencyProperty as the backing store for InitialSizeRequest.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InitialSizeRequestProperty =
			DependencyProperty.RegisterAttached("InitialSizeRequest", typeof(Size), typeof(TouchWindow), new UIPropertyMetadata(Size.Empty));

		#endregion

        public LoadedResource LoadedResource { get; set; }

        public Connection Connector { get; set; }

        public bool Iconized { get; set; }

        public event EventHandler<LoadedResource> Copied = delegate { };

        public string ResourceType { get; set; }
	    public FrameworkElement Content { get; set; }
	    public ImageSource Thumbnail { get; set; }
	    public string Title { get; set; }

        public string ContentType { get; set; }

	    public TouchWindow(LoadedResource resource)
	    {
	        LoadedResource = resource;
	        Title = resource.Resource.Name;
            ContentType = resource.Resource.FileType;
            Thumbnail = resource.Thumbnail;
            Content = resource.Content;

            DataContext = this;
	        InitializeComponent();

	        MaxHeight = 600;
	        MaxWidth = 800;

	        CanRotate = false;

            Width = 400;
           Height = 300;
            Events.RegisterGestureEventSupport(this);
	    }


	    private ContentControl _contentHolder;
        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("contentHolder") != null)
            {
                _contentHolder = GetTemplateChild("contentHolder") as ContentControl;
                if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                    _contentHolder.Loaded += (c_contentHolder_Loaded);
                _contentHolder.Content = Content;

            }
            base.OnApplyTemplate();
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
        private void Grid_OnDoubleTapGesture(object sender, GestureEventArgs e)
        {
            if (time == ConvertToTimestamp(DateTime.Now))
                return;
            time = ConvertToTimestamp(DateTime.Now);

            if (Iconized)
                Copied(this, LoadedResource);
            else
            {
                Template = (ControlTemplate)FindResource("Docked");
                Width = 100;
                Height = 100;
                Iconized = true;
            }

        }

		private static readonly Size DefaultPopupSize = new Size(300, 200);
		/// <summary>
		/// Gets the size that the parent container should have to fully accomodate the PopupWindow and its child content
		/// based on the child's InitialSizeRequest.
		/// </summary>
		/// <returns>The size, which should be set to the parent container</returns>
		private Size CalculateScatterViewItemSize()
		{
            var presenter = GuiHelpers.GetChildObject<ContentPresenter>(_contentHolder);
			if (presenter == null)
				return DefaultPopupSize;
			// It seems it's safe to assume the ContentPresenter will always only have one child and that child is the visual representation
			// of the content of c_contentHolder.
			var child = VisualTreeHelper.GetChild(presenter, 0);
			if (child == null)
				return DefaultPopupSize;
			var requestedSize = TouchWindow.GetInitialSizeRequest(child);
			if (!requestedSize.IsEmpty
				&& requestedSize.Width != 0
				&& requestedSize.Height != 0)
			{
                var borderHeight = this.ActualHeight - _contentHolder.ActualHeight;
                var borderWidth = this.ActualWidth - _contentHolder.ActualWidth;
				return new Size(requestedSize.Width + borderWidth, requestedSize.Height + borderHeight);
			}
			else
				return DefaultPopupSize;
		}

		void c_contentHolder_Loaded(object sender, RoutedEventArgs e)
		{
			var newSize = CalculateScatterViewItemSize();
		}
		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			var sv = GuiHelpers.GetParentObject<ScatterView>(this);
			if (sv != null)
				sv.Items.Remove(this);
		}

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Template = (ControlTemplate)FindResource("Docked");
            Iconized = true;   
        }
	}
}
