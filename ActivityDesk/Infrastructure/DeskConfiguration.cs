using System.Collections.Generic;
using NooSphere.Model;
using NooSphere.Model.Configuration;
using NooSphere.Model.Device;
using NooSphere.Model.Primitives;

namespace ActivityDesk.Infrastructure
{
    public class DeskConfiguration : Noo, ISituatedConfiguration
    {

        public IDevice Device { get; set; }

        public List<IResourceConfiguration> Configurations { get; set; }

        public List<DeviceConfiguration> DeviceConfigurations { get; set; }

        public DeskConfiguration()
        {
            Configurations = new List<IResourceConfiguration>();
            DeviceConfigurations = new List<DeviceConfiguration>();
        }
    }

    public class DefaultResourceConfiguration : IResourceConfiguration
    {
        public FileResource Resource { get; set; }
    }

    public class DeskResourceConfiguration : IResourceConfiguration
    {
        public FileResource Resource { get; set; }

        public DockStates DockState { get; set; }

        public System.Windows.Point Center { get; set; }

        public System.Windows.Size Size { get; set; }

        public double Orientation { get; set; }

        public bool Iconized { get; set; }
    }
    public class DeviceConfiguration:IResourceConfiguration
    {
        public IDevice Device { get; set; }

        public List<IResourceConfiguration> Configurations { get; set; }

        public double Orientation { get; set; }

        public System.Windows.Point Center { get; set; }

        public bool Thumbnail { get; set; }

        public string TagValue { get; set; }

        public bool Pinned { get; set; }

        public DeviceConfiguration()
        {
            Configurations = new List<IResourceConfiguration>();
        }


        public FileResource Resource { get; set; }
    }
}
