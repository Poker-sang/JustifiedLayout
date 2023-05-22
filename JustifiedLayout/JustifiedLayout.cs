using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JustifiedLayout;

public sealed class JustifiedLayout : VirtualizingLayout
{
    private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is JustifiedLayout jp)
        {
            jp.InvalidateMeasure();
            jp.InvalidateArrange();
        }
    }

    /// <inheritdoc />
    protected override void InitializeForContextCore(VirtualizingLayoutContext context)
    {
        context.LayoutState = new JustifiedLayoutState(context);
        base.InitializeForContextCore(context);
    }

    /// <inheritdoc />
    protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
    {
        context.LayoutState = null;
        base.UninitializeForContextCore(context);
    }

    /// <inheritdoc />
    protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
    {
        var state = (JustifiedLayoutState)context.LayoutState;

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                state.RemoveFromIndex(args.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Move:
                var minIndex = Math.Min(args.NewStartingIndex, args.OldStartingIndex);
                state.RemoveFromIndex(minIndex);
                state.RecycleElementAt(args.OldStartingIndex);
                state.RecycleElementAt(args.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                state.RemoveFromIndex(args.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                state.RemoveFromIndex(args.NewStartingIndex);
                state.RecycleElementAt(args.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                state.Clear();
                break;
        }

        base.OnItemsChangedCore(context, source, args);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size parentMeasure)
    {
        var spacingMeasure = new Size(HorizontalSpacing, VerticalSpacing);

        var state = (JustifiedLayoutState)context.LayoutState;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (state.AvailableWidth != parentMeasure.Width
            || spacingMeasure != state.Spacing)
        {
            state.ClearAll();
            state.AvailableWidth = parentMeasure.Width;
            state.Spacing = spacingMeasure;
        }
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (RowHeight != state.RowHeight)
        {
            state.ClearPositions();
            state.RowHeight = RowHeight;
        }

        var realizationBounds = context.RealizationRect;
        Point? nextPosition = new Point();
        var currentRow = new List<JustifiedItem>();
        var currentRowLength = .0;
        for (var i = 0; i < context.ItemCount; ++i)
        {
            Point currentPosition;
            var item = state.GetItemAt(i);

            if (nextPosition is not null)
            {
                item.Position = currentPosition = nextPosition.Value;
                nextPosition = null;
            }
            else if (item.Position is not null)
                currentPosition = item.Position.Value;
            else
            {
                currentPosition = state.GetItemAt(i - 1).Position!.Value;
                currentPosition.Y += RowHeight + spacingMeasure.Height;
                item.Position = currentPosition;
            }

            if (currentPosition.Y + RowHeight < realizationBounds.Top)
            {
                // Item is "above" the bounds
                if (item.Element is not null)
                {
                    context.RecycleElement(item.Element);
                    item.Element = null;
                }

                continue;
            }
            else if (currentPosition.Y > realizationBounds.Bottom)
            {
                // Item is "below" the bounds.
                if (item.Element is not null)
                {
                    context.RecycleElement(item.Element);
                    item.Element = null;
                }

                // We don't need to measure anything below the bounds
                break;
            }

            item.Element = context.GetOrCreateElementAt(i);
            item.Element.Measure(new(double.PositiveInfinity, RowHeight));
            if (item.DesiredSize is null)
            {
                item.DesiredSize = item.Element.DesiredSize;
            }
            else if (item.DesiredSize != item.Element!.DesiredSize)
            {
                state.RemoveFromIndex(i + 1);
                item.DesiredSize = item.Element.DesiredSize;
            }

            if (CalcNextPosition(item.DesiredSize.Value) && currentPosition.Y > realizationBounds.Bottom)
            {
                // Item is "below" the bounds.
                if (item.Element is not null)
                {
                    context.RecycleElement(item.Element);
                    item.Element = null;
                }

                // We don't need to measure anything below the bounds
                break;
            }

            bool CalcNextPosition(Size desiredSize)
            {
                item.Measure = desiredSize;

                if (desiredSize.Width is 0)
                {
                    nextPosition = currentPosition;
                    return false;
                }

                var excessLength = currentPosition.X + desiredSize.Width - parentMeasure.Width;

                if (excessLength + spacingMeasure.Width > 0)
                {
                    var shrinkScale = (parentMeasure.Width - currentRow.Count * spacingMeasure.Width) / (currentRowLength + desiredSize.Width);
                    var enlargeScale = (parentMeasure.Width - (currentRow.Count - 1) * spacingMeasure.Width) / currentRowLength;

                    // shrinkScale < enlargeScale
                    // find the one that is closer to 1
                    // length excessed
                    if (1 / shrinkScale < enlargeScale)
                    {
                        currentRow.Add(item);
                        // is not used before next assignment
                        // currentRowLength += currentMeasure.Width;
                        Resize(shrinkScale);
                        currentRow.Clear();
                        currentRowLength = 0;
                        // New Row
                        nextPosition = currentPosition = new(0, currentPosition.Y + RowHeight + spacingMeasure.Height);
                    }
                    // length exceeded after adding space
                    else
                    {
                        Resize(enlargeScale);
                        currentRow.Clear();
                        currentRowLength = 0;
                        // New Row
                        item.Position = currentPosition = new(0, currentPosition.Y + RowHeight + spacingMeasure.Height);

                        currentRow.Add(item);
                        currentRowLength += desiredSize.Width;

                        currentPosition.X += desiredSize.Width + spacingMeasure.Width;
                        nextPosition = currentPosition;

                        return true;
                    }

                    void Resize(double scale)
                    {
                        var nextPositionX = .0;
                        var tempPosition = currentPosition;
                        foreach (var justifiedItem in currentRow)
                        {
                            tempPosition.X = nextPositionX;
                            justifiedItem.Position = tempPosition;
                            var tempMeasure = justifiedItem.Measure!.Value;
                            tempMeasure.Width *= scale;
                            justifiedItem.Measure = tempMeasure;
                            nextPositionX = tempPosition.X + tempMeasure.Width + spacingMeasure.Width;
                        }
                    }
                }
                else
                {
                    currentRow.Add(item);
                    currentRowLength += desiredSize.Width;

                    currentPosition.X += desiredSize.Width + spacingMeasure.Width;
                    nextPosition = currentPosition;
                }

                return false;
            }
        }
        // update value with the last line
        // if the the last loop is(parentMeasure.Width > currentMeasure.Width + lineMeasure.Width) the total isn't calculated then calculate it
        // if the last loop is (parentMeasure.Width > currentMeasure.Width) the currentMeasure isn't added to the total so add it here
        // for the last condition it is zeros so adding it will make no difference
        // this way is faster than an if condition in every loop for checking the last item
        var totalMeasure = new Size
        {
            Width = parentMeasure.Width
        };

        // Propagating an infinite size causes a crash. This can happen if the parent is scrollable and infinite in the opposite
        // axis to the panel. Clearing to zero prevents the crash.
        // This is likely an incorrect use of the control by the developer, however we need stability here so setting a default that wont crash.
        if (double.IsInfinity(totalMeasure.Width))
        {
            totalMeasure.Width = 0;
        }

        totalMeasure.Height = state.GetHeight();

        totalMeasure.Width = Math.Ceiling(totalMeasure.Width);
        Task.Delay(1);
        return totalMeasure;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size parentMeasure)
    {
        if (context.ItemCount > 0)
        {
            var realizationBounds = context.RealizationRect;

            var state = (JustifiedLayoutState)context.LayoutState;
            bool ArrangeItem(JustifiedItem item)
            {
                if (item is { Measure: null } or { Position: null })
                {
                    return false;
                }

                var desiredMeasure = item.Measure.Value;

                var position = item.Position.Value;

                if (position.Y + desiredMeasure.Height >= realizationBounds.Top && position.Y <= realizationBounds.Bottom)
                {
                    // place the item
                    var child = context.GetOrCreateElementAt(item.Index);
                    child.Arrange(new(position, desiredMeasure));
                }
                else if (position.Y > realizationBounds.Bottom)
                {
                    return false;
                }

                return true;
            }

            for (var i = 0; i < context.ItemCount; ++i)
            {
                if (!ArrangeItem(state.GetItemAt(i)))
                {
                    break;
                }
            }
        }

        return parentMeasure;
    }

    #region DependencyProperties

    public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(JustifiedLayout), new PropertyMetadata(0d, LayoutPropertyChanged));
    public double HorizontalSpacing { get => (double)GetValue(HorizontalSpacingProperty); set => SetValue(HorizontalSpacingProperty, value); }

    public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(JustifiedLayout), new PropertyMetadata(0d, LayoutPropertyChanged));
    public double VerticalSpacing { get => (double)GetValue(VerticalSpacingProperty); set => SetValue(VerticalSpacingProperty, value); }

    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(JustifiedLayout), new PropertyMetadata(50d, LayoutPropertyChanged));
    public double RowHeight { get => (double)GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

    #endregion
}
