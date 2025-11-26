// <copyright file="IdTypeConverter.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core.ComponentModel
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    using Zentient.Core;

    /// <summary>
    /// TypeConverter for <see cref="Id{T}"/> enabling string binding in ASP.NET Core / XAML.
    /// </summary>
    public sealed class IdTypeConverter<T> : TypeConverter
    {
        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        /// <inheritdoc/>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
            {
                return Id<T>.Parse(s, culture);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc/>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Id<T> id)
            {
                return id.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
