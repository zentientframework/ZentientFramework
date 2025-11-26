namespace Zentient.Codes
{
    using System;
    using Zentient.Metadata;

    // --- Builder ------------------------------------------------------------

    /// <summary>
    /// Default lightweight builder implementation.
    /// Ensures Metadata is never null and performs early validation.
    /// </summary>
    public sealed class CodeBuilder<TDefinition> : ICodeBuilder<TDefinition>
        where TDefinition : ICodeDefinition
    {
        private TDefinition? _definition;
        private string? _key;
        private string? _display;
        private IMetadata _metadata = Metadata.Empty;

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithDefinition(TDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            return this;
        }

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithKey(string key)
        {
            CodeValidation.ValidateKey(key);
            _key = key;
            return this;
        }

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithDisplayName(string displayName)
        {
            CodeValidation.ValidateDisplay(displayName);
            _display = displayName;
            return this;
        }

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithMetadata(string key, object? value)
        {
            if (_metadata is IMetadata m) m.Set(key, value);
            else
            {
                var b = Metadata.NewBuilder();
                b.DeepMerge(_metadata);
                b.Set(key, value);
                _metadata = b.Build();
            }
            return this;
        }

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithMetadata(IMetadata metadata)
        {
            if (metadata is null) return this;
            if (_metadata is IMetadata mm)
            {
                Zentient.Metadata.Metadata.DeepMerge(mm, metadata);
            }
            else
            {
                var b = Metadata.NewBuilder();
                var mergedMetadata = Zentient.Metadata.Metadata.DeepMerge(metadata, _metadata);
                b.SetRange(mergedMetadata);
                _metadata = b.Build();
            }
            return this;
        }

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithMetadataIf(bool condition, string key, object? value)
        {
            if (condition) WithMetadata(key, value);
            return this;
        }

        /// <inheritdoc/>
        public ICodeBuilder<TDefinition> WithMetadataIf(bool condition, IMetadata metadata)
        {
            if (condition) WithMetadata(metadata);
            return this;
        }

        /// <inheritdoc/>
        public ICode<TDefinition> Build()
        {
            if (_definition is null) throw new InvalidOperationException("Definition must be provided before building a Code.");
            if (string.IsNullOrWhiteSpace(_key)) throw new InvalidOperationException("Key must be provided before building a Code.");
            var meta = _metadata ?? Metadata.Empty;
            return Code<TDefinition>.GetOrCreate(_key!, _definition!, meta, _display);
        }
    }
}
