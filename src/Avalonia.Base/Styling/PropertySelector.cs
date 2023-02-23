using System;
using Avalonia.Utilities;

namespace Avalonia.Styling;

/// <summary>
/// Base selector allow compare <see cref="AvaloniaObject"/> value.
/// </summary>
internal abstract class PropertySelector<T> : Selector
{
    private readonly Selector? _previous;
    private readonly AvaloniaProperty _property;
    private readonly T? _value;
    private string? _selectorString;

    public PropertySelector(Selector? previous, AvaloniaProperty property, T? value)
    {
        property = property ?? throw new ArgumentNullException(nameof(property));

        _previous = previous;
        _property = property;
        _value = value;
    }

    /// <summary>
    /// <see cref="AvaloniaProperty"/> to compare.
    /// </summary>
    protected AvaloniaProperty Property => _property;
    /// <summary>
    /// Reference vaule
    /// </summary>
    protected T? Value => _value;

    /// <inheritdoc/>
    public override bool InTemplate => _previous?.InTemplate ?? false;

    /// <inheritdoc/>
    public override bool IsCombinator => false;

    /// <inheritdoc/>
    public override Type? TargetType => _previous?.TargetType;

    /// <inheritdoc/>
    public sealed override string ToString(Style? owner)
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
            builder.Append(_value);
            builder.Append(']');

            _selectorString = StringBuilderCache.GetStringAndRelease(builder);
        }

        return _selectorString;
    }

    protected abstract string Operator { get; }

    /// <inheritdoc/>
    protected abstract override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe);

    sealed protected override Selector? MovePrevious() => _previous;
    sealed protected override Selector? MovePreviousOrParent() => _previous;

}
