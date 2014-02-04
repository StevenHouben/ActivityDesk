using System.Drawing;
using ActivityDesk.Infrastructure;
using NooSphere.Model.Device;

namespace ActivityDesk
{

    public class ResourceReleasedEventArgs
    {
        public Device Device { get; set; }
        public LoadedResource LoadedResource { get; set; }
        public System.Windows.Point Position { get; set; }
    }
}
