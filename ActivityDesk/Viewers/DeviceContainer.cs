
using ActivityDesk.Visualizer.Visualizations;
using NooSphere.Model.Device;

namespace ActivityDesk.Viewers
{
    public class DeviceContainer
    {
        public DeviceThumbnail DeviceTumbnail { get; set; }
        public Device Device { get; set; }
        public VisualizationTablet DeviceVisualization { get; set; }

        public DeviceVisual VisualStyle { get; set; }

        public string TagValue { get; set; }

        public bool Docked { get; set; }

        public bool Connected { get; set; }

        public DeviceContainer()
        {
            Docked = false;
            VisualStyle=DeviceVisual.Visualisation;
            Device = new Device();
        }

    }

    public enum DeviceVisual
    {
        Thumbnail,
        Visualisation
    }

}
