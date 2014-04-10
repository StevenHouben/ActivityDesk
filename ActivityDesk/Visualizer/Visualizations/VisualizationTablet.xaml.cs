using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ActivityDesk.Infrastructure;
using ActivityDesk.Viewers;
using Blake.NUI.WPF.Gestures;
using NooSphere.Model.Device;

namespace ActivityDesk.Visualizer.Visualizations
{
    public partial class VisualizationTablet : BaseVisualization, INotifyPropertyChanged,IResourceContainer
    {

      
      
        public VisualizationTablet()
	    {
            InitializeComponent();
	    }

      
        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
        {
            OnLocked();
        }

        private void OnDoubleTapGesture(object sender, GestureEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var frameworkElement = Parent as FrameworkElement;
            if (frameworkElement == null) return;
            var point = fe.TranslatePoint(new Point(0, 0), frameworkElement.Parent as FrameworkElement);

            var res = fe.DataContext as LoadedResource;

            if (res == null) return;

            LoadedResources.Remove(res);

             OnResourceReleased(res, point);

            if (Resource == res)
                Resource = LoadedResources.Count != 0 ? LoadedResources.First() : LoadedResource.EmptyResource;
        }


        private void Grid_OnTouchDown(object sender, TouchEventArgs e)
        {
            if (IsDoubleTap(e))
                OnDoubleTouchDown(sender);
        }
        private readonly Stopwatch _doubleTapStopwatch = new Stopwatch();
        private Point _lastTapLocation;

        public event EventHandler DoubleTouchDown;

        protected virtual void OnDoubleTouchDown(object sender)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var frameworkElement = Parent as FrameworkElement;
            if (frameworkElement == null) return;
            var point = fe.TranslatePoint(new Point(0, 0), frameworkElement.Parent as FrameworkElement);

            var res = fe.DataContext as LoadedResource;

            if (res == null) return;

            LoadedResources.Remove(res);


            OnResourceReleased(res,point);

            if (Resource == res)
                Resource = LoadedResources.Count != 0 ? LoadedResources.First() : LoadedResource.EmptyResource;
        }

        private bool IsDoubleTap(TouchEventArgs e)
        {
            TimeSpan elapsed = _doubleTapStopwatch.Elapsed;
            _doubleTapStopwatch.Restart();
            bool tapsAreCloseInTime = (elapsed != TimeSpan.Zero && elapsed < TimeSpan.FromSeconds(0.7));

            return tapsAreCloseInTime;
        }
    }
}