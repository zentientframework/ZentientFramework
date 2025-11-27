// <copyright file="TypeConverter.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Metadata
{
    using System;

    /// <summary>
    /// Internal conversion helpers used by metadata getters to coerce raw values into requested types.
    /// Optimized for common primitive types and enums while providing a safe fallback for other conversions.
    /// </summary>
    internal static class TypeConverter
    {
        /// <summary>
        /// Attempt to convert a raw stored object into the requested target type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target conversion type.</typeparam>
        /// <param name="raw">Raw value to convert; may be null.</param>
        /// <param name="value">When conversion succeeds, contains the converted value; otherwise default.</param>
        /// <returns><see langword="true"/> if conversion succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryConvert<T>(object? raw, out T? value)
        {
            value = default;
            if (raw is null) return default(T) is null;

            // Fast direct match
            if (raw is T direct)
            {
                value = direct;
                return true;
            }

            var target = typeof(T);

            // Common trivial cases: string/int/long/double/bool
            if (target == typeof(string))
            {
                if (raw is string s) { value = (T)(object)s; return true; }
                value = (T)(object)raw.ToString()!;
                return true;
            }

            if (target.IsPrimitive || target.IsValueType)
            {
                try
                {
                    if (target.IsEnum)
                    {
                        if (raw is string rs && Enum.TryParse(target, rs, true, out var ev)) { value = (T)ev!; return true; }
                        var underlying = Convert.ChangeType(raw, Enum.GetUnderlyingType(target));
                        value = (T)Enum.ToObject(target, underlying!);
                        return true;
                    }

                    var convTarget = Nullable.GetUnderlyingType(target) ?? target;
                    var converted = Convert.ChangeType(raw, convTarget);
                    value = (T)converted!;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // Fallback: attempt system conversion
            try
            {
                value = (T)Convert.ChangeType(raw, Nullable.GetUnderlyingType(target) ?? target);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
