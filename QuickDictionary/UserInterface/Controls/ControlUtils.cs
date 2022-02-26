using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickDictionary.UserInterface.Controls;

public static class ControlUtils
{
    public static void HideBoundingBox(object root)
    {
        if (root is Control control)
            control.FocusVisualStyle = null;

        if (root is DependencyObject dependencyObject)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(dependencyObject))
                HideBoundingBox(child);
        }
    }

    public static void Show(this UIElement element)
    {
        element.Visibility = Visibility.Visible;
    }

    public static void Hide(this UIElement element, bool collapse = true)
    {
        element.Visibility = collapse ? Visibility.Collapsed : Visibility.Hidden;
    }

    public static Point RealPixelsToWpf(Window w, Point p)
    {
        Matrix t = PresentationSource.FromVisual(w).CompositionTarget.TransformFromDevice;
        return t.Transform(p);
    }

    public static Point WpfToRealPixels(Window w, Point p)
    {
        Matrix t = PresentationSource.FromVisual(w).CompositionTarget.TransformToDevice;
        return t.Transform(p);
    }
}
