using ActivityDesk.Infrastructure;

namespace ActivityDesk.Viewers
{
    public interface IResourceContainer
    {
        LoadedResource Resource { get; set; }

        bool Iconized { get; set; }

    }
}
