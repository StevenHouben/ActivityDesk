using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using System.Windows;

namespace ActivityDesk.Visualizer.Definitions
{
    public class TabletDefinition : TagVisualizationDefinition
    {
        protected override bool Matches(TagData tag)
        {
            return tag.Value == 170 || tag.Value == 171;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TabletDefinition();
        }
    }
}
