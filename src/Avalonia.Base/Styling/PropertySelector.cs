using System;
using Avalonia.Utilities;

namespace Avalonia.Styling;

/// <summary>
/// Base selector allow compare <see cref="AvaloniaObject"/> value.
/// </summary>
internal abstract class PropertySelector : Selector
{
    private readonly Selector? _previous;
    private readonly AvaloniaProperty _property;
    private string? _selectorString;

    public PropertySelector(Selector? previous, AvaloniaProperty property)
    {
        property = property ?? throw new ArgumentNullException(nameof(property));

        _previous = previous;
        _property = property;
    }

    /// <summary>
    /// <see cref="AvaloniaProperty"/> to compare.
    /// </summary>
    protected AvaloniaProperty Property => _property;

    /// <inheritdoc/>
    public override bool InTemplate => _previous?.InTemplate ?? false;

    /// <inheritdoc/>
    public override bool IsCombinator => false;

    /// <inheritdoc/>
    public override Type? TargetType => _previous?.TargetType;

    /// <inheritdoc/>
    sealed public override string ToString(Style? owner)
    {
        if (_selectorString == null)
        {
            var builder = StringBuilderCache.Acquire();

            if (_previous != null)
            {
                builder.Append(_previous.ToString(owner));
            }

            builder.Append('[');

            if (_property.IsAttached)
            {
                builder.Append('(');
                builder.Append(_property.OwnerType.Name);
                builder.Append('.');
            }

            builder.Append(_property.Name);
            if (_property.IsAttached)
            {
                builder.Append(')');
            }
            builder.Append(Operator);
            OnToString(owner, builder);
            builder.Append(']');

            _selectorString = StringBuilderCache.GetStringAndRelease(builder);
        }

        return _selectorString;
    }

    protected abstract void OnToString(Style? owner, System.Text.StringBuilder builder);

    protected abstract string Operator { get; }

    /// <inheritdoc/>
    protected abstract override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe);

    sealed protected override Selector? MovePrevious() => _previous;
    sealed protected override Selector? MovePreviousOrParent() => _previous;

}
