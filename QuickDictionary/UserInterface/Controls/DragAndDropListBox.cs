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

    private P FindVisualParent<P>(DependencyObject child)
        where P : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null)
            return null;

        if (parentObject is P parent)
            return parent;

        return FindVisualParent<P>(parentObject);
    }

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

    private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(null);
        var diff = dragStartPoint - point;
        if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            var lb = sender as ListBox;
            var lbi = FindVisualParent<ListBoxItem>(((DependencyObject)e.OriginalSource));
            if (lbi != null)
            {
                DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
            }
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
                var sourceIndex = Items.IndexOf(source);
                var targetIndex = Items.IndexOf(target);

                Move(source, sourceIndex, targetIndex);
            }
        }
    }

    private void Move(T source, int sourceIndex, int targetIndex)
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
                var removeIndex = sourceIndex + 1;
                if (items.Count + 1 > removeIndex)
                {
                    items.Insert(targetIndex, source);
                    items.RemoveAt(removeIndex);
                }
            }
        }
    }
}