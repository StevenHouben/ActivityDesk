using System.Windows;

namespace ActivityDesk
{
    public class DockStateManager
    {
        public static DependencyProperty DockState;
        public static DockStates GetDockState(DependencyObject obj)
        {
            return (DockStates)obj.GetValue(DockState);
        }
        public static void SetDockState(DependencyObject obj, DockStates value)
        {
            obj.SetValue(DockState, value);
        }
    }
}
