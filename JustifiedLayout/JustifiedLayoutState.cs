using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JustifiedLayout;

internal class JustifiedLayoutState
{
    private readonly List<JustifiedItem> _items = new();
    private readonly VirtualizingLayoutContext _context;

    public JustifiedLayoutState(VirtualizingLayoutContext context) => _context = context;

    public Size Spacing { get; internal set; }

    public double RowHeight { get; internal set; }

    public double AvailableWidth { get; internal set; }

    internal JustifiedItem GetItemAt(int index)
    {
        if (index < 0)
        {
            throw new IndexOutOfRangeException();
        }

        if (index <= _items.Count - 1)
        {
            return _items[index];
        }
        else
        {
            var item = new JustifiedItem(index);
            _items.Add(item);
            return item;
        }
    }

    internal void Clear()
    {
        _items.Clear();
    }

    internal void RemoveFromIndex(int index)
    {
        if (index >= _items.Count)
        {
            // Item was added/removed but we haven't realized that far yet
            return;
        }

        var numToRemove = _items.Count - index;
        _items.RemoveRange(index, numToRemove);
    }

    internal void ClearAll()
    {
        foreach (var item in _items)
        {
            item.Position = null;
            item.Measure = null;
            item.DesiredSize = null;
        }
    }

    internal void ClearPositions()
    {
        foreach (var item in _items)
        {
            item.Position = null;
        }
    }

    internal double GetHeight()
    {
        if (_items.Count is 0)
        {
            return 0;
        }

        Point? lastPosition = null;

        for (var i = _items.Count - 1; i >= 0; --i)
        {
            var item = _items[i];
            if (item.Position is null)
            {
                continue;
            }

            if (lastPosition != null)
            {
                if (lastPosition.Value.Y > item.Position.Value.Y)
                {
                    // This is a row above the last item. Exit and calculate the average
                    break;
                }
            }

            lastPosition = item.Position;
        }

        return lastPosition?.Y + RowHeight ?? 0;
    }

    internal void RecycleElementAt(int index)
    {
        var element = _context.GetOrCreateElementAt(index);
        _context.RecycleElement(element);
    }
}
