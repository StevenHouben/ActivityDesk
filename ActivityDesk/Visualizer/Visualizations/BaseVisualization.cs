using System;
using Microsoft.Surface.Presentation.Controls;
using NooSphere.Model;
using NooSphere.Model.Resources;

namespace ActivityDesk.Visualizer.Visualizations
{
    public abstract class BaseVisualization : TagVisualization
    {
        public event LockedEventHandler Locked = delegate { }; 
        public event EventHandler<FileResource> ReleaseResource = delegate { };

        protected BaseVisualization()
        {
            AllowDrop = true;
        }

        protected virtual void OnLocked()
        {
            if (Locked != null)
                Locked(this, new LockedEventArgs(VisualizedTag.Value.ToString()));
        }

        protected virtual void OnReleaseResource(FileResource resource)
        {
            if (ReleaseResource != null)
                ReleaseResource(this, resource);
        }
    }
    public delegate void LockedEventHandler(Object sender, LockedEventArgs e);
    public class LockedEventArgs
    {
        public string VisualizedTag { get; set; }
        public LockedEventArgs(string tag)
        {
            VisualizedTag = tag;
        }
    }
}
