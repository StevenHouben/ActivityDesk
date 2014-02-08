using System;
using System.Security.Cryptography.X509Certificates;
using ActivityDesk.Infrastructure;

namespace ActivityDesk.Viewers
{
    public interface IResourceContainer
    {
        LoadedResource Resource { get; set; }

        bool Iconized { get; set; }

        event EventHandler<LoadedResource> Copied;

        string ResourceType { get; set; }
    }
}
