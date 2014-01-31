using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ActivityDesk.Infrastructure;
using ActivityDesk.Viewers;
using NooSphere.Model.Device;

namespace ActivityDesk.Visualizer.Visualizations
{
    public partial class VisualizationTablet : BaseVisualization
    {
        public event EventHandler<Device> Closed = delegate { };

        public Device Device { get; private set; }
        public ObservableCollection<FrameworkElement> ResourceComponents;
        public VisualizationTablet()
	    {
            InitializeComponent();

            ResourceComponents = new ObservableCollection<FrameworkElement>();

            DataContext = this;

            Drop+=VisualizationTablet_Drop;
            DragOver += VisualizationTablet_DragOver;

        }

        void VisualizationTablet_DragOver(object sender, DragEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        void VisualizationTablet_Drop(object sender, DragEventArgs e)
        {
            
        }

        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
        {
            OnLocked();
        }
    }
}