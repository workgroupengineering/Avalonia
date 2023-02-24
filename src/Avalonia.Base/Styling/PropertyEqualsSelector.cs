using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    /// <summary>
    /// A selector that matches the common case of a type and/or name followed by a collection of
    /// style classes and pseudoclasses.
    /// </summary>
    sealed internal class PropertyEqualsSelector : PropertySelector
    {
        public PropertyEqualsSelector(Selector? previous, AvaloniaProperty property, object? value)
            :base(previous,property)
        {
            Value = value;
        }

        protected override string Operator => "=";
        protected override void OnToString(Style? owner, System.Text.StringBuilder builder) =>
            builder.Append(Value);

        public object? Value { get; }

        /// <inheritdoc/>
        internal override bool InTemplate => _previous?.InTemplate ?? false;

        /// <inheritdoc/>
        internal override bool IsCombinator => false;

        /// <inheritdoc/>
        internal override Type? TargetType => _previous?.TargetType;

        /// <inheritdoc/>
        public override string ToString(Style? owner)
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
                builder.Append('=');
                builder.Append(_value ?? string.Empty);
                builder.Append(']');

                _selectorString = StringBuilderCache.GetStringAndRelease(builder);
            }

            return _selectorString;
        }

        /// <inheritdoc/>
        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (subscribe)
            {
                return new SelectorMatch(new PropertyEqualsActivator(control, Property, Value));
            }
            else
            {
                return Compare(Property.PropertyType, control.GetValue(Property), Value)
                    ? SelectorMatch.AlwaysThisInstance
                    : SelectorMatch.NeverThisInstance;
            }
        }

        private protected override Selector? MovePrevious() => _previous;
        private protected override Selector? MovePreviousOrParent() => _previous;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.TypeConversionSupressWarningMessage)]
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = TrimmingMessages.TypeConversionSupressWarningMessage)]
        internal static bool Compare(Type propertyType, object? propertyValue, object? value)
        {
            if (propertyType == typeof(object) &&
                propertyValue?.GetType() is Type inferredType)
            {
                propertyType = inferredType;
            }

            var valueType = value?.GetType();

            if (valueType is null || propertyType.IsAssignableFrom(valueType))
            {
                return Equals(propertyValue, value);
            }

            var converter = TypeDescriptor.GetConverter(propertyType);
            if (converter?.CanConvertFrom(valueType) == true)
            {
                return Equals(propertyValue, converter.ConvertFrom(null, CultureInfo.InvariantCulture, value!));
            }

            return false;
        }
    }
}
