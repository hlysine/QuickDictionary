using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace QuickDictionary.UserInterface.Controls;

public class ScrollParentWhenAtMax : Behavior<FrameworkElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += previewMouseWheel;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= previewMouseWheel;
        base.OnDetaching();
    }

    private void previewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = getVisualChild<ScrollViewer>(AssociatedObject);
        double scrollPos = scrollViewer.ContentVerticalOffset;

        if (Math.Abs(scrollPos - scrollViewer.ScrollableHeight) < 1e-5 && e.Delta < 0
            || scrollPos == 0 && e.Delta > 0)
        {
            e.Handled = true;
            var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            e2.RoutedEvent = UIElement.MouseWheelEvent;
            AssociatedObject.RaiseEvent(e2);
        }
    }

    private static T getVisualChild<T>(DependencyObject parent) where T : Visual
    {
        var child = default(T);

        int numVisuals = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < numVisuals; i++)
        {
            var v = (Visual)VisualTreeHelper.GetChild(parent, i);
            child = v as T;
            if (child == null)
                child = getVisualChild<T>(v);

            if (child != null)
                break;
        }

        return child;
    }
}
