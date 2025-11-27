// <copyright file="Code.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Zentient.Exceptions
{
    internal class DefaultConstructorMissingException : Exception
    {
        public DefaultConstructorMissingException(string key, string message) : base($"Failed to resolve definition for key '{key}': {message}")
        { }
    }
}