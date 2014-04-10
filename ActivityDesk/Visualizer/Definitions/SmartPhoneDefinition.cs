
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using System.Windows;

namespace ActivityDesk.Visualizer.Definitions
{
    public class SmartPhoneDefinition : TagVisualizationDefinition
    {
        protected override bool Matches(TagData tag)
        {
            return tag.Value > 0 && tag.Value < 150;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new SmartPhoneDefinition();
        }
    }
}
