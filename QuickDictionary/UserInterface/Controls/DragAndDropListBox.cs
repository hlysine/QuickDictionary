using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuickDictionary.UserInterface.Controls;

public class DragAndDropListBox<T> : ListBox
    where T : class
{
    private Point dragStartPoint;

    public DragAndDropListBox()
    {
        PreviewMouseMove += ListBox_PreviewMouseMove;

        var style = new Style(typeof(ListBoxItem));

        style.Setters.Add(new Setter(AllowDropProperty, true));

        style.Setters.Add(
            new EventSetter(
                PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonDown)));

        style.Setters.Add(
            new EventSetter(
                DropEvent,
                new DragEventHandler(ListBoxItem_Drop)));

        ItemContainerStyle = style;
    }

    private TParent FindVisualParent<TParent>(DependencyObject child)
        where TParent : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null)
            return null;

        if (parentObject is TParent parent)
            return parent;

        return FindVisualParent<TParent>(parentObject);
    }

    private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        Point point = e.GetPosition(null);
        Vector diff = dragStartPoint - point;

        if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            var lbi = FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (lbi != null)
                DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
        }
    }

    private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStartPoint = e.GetPosition(null);
    }

    private void ListBoxItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is ListBoxItem item)
        {
            if (e.Data.GetData(typeof(T)) is T source && item.DataContext is T target)
            {
                int sourceIndex = Items.IndexOf(source);
                int targetIndex = Items.IndexOf(target);

                move(source, sourceIndex, targetIndex);
            }
        }
    }

    private void move(T source, int sourceIndex, int targetIndex)
    {
        if (sourceIndex < targetIndex)
        {
            if (DataContext is IList<T> items)
            {
                items.Insert(targetIndex + 1, source);
                items.RemoveAt(sourceIndex);
            }
        }
        else
        {
            if (DataContext is IList<T> items)
            {
                int removeIndex = sourceIndex + 1;

                if (items.Count + 1 > removeIndex)
                {
                    items.Insert(targetIndex, source);
                    items.RemoveAt(removeIndex);
                }
            }
        }
    }
}
