using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Shapes;
using ActivityDesk.Infrastructure;
using Microsoft.Surface.Presentation.Controls;

namespace ActivityDesk.Viewers
{
    public interface IResourceContainer
    {
        LoadedResource LoadedResource { get; set; }

        bool Iconized { get; set; }

        event EventHandler<LoadedResource> Copied;

        string ResourceType { get; set; }

        Connection Connector { get; set; }
    }

    public class Connection
    {
        public Line ConnectionLine { get; set; }
        public ScatterViewItem Item { get; set; }
    }
}
