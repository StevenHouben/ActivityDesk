using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ActivityDesk.Visualizer.Visualizations;

namespace BaseVis
{
    public partial class VisualizeSmartPhone : BaseVisualization
    {
        private int enterCount;

        /// <summary>
        /// Constructor.
        /// </summary>
        public VisualizeSmartPhone()
        {
            InitializeComponent();

            Glow.RenderTransform = new ScaleTransform(1, 1);
        }
       
        private void SurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Clicked the pin button");
        }
    }
}