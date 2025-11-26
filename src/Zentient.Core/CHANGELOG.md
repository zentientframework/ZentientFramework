CHANGES / RELEASE NOTES (v1.0.0)

Core packaged as single NuGet Zentient.Core v1.0.0.

Fixed ExecutionContext disposal race and ensured linked CTS disposal.

Standardized Try* reason messages and added disposal guards.

Inflight factory uses LazyThreadSafetyMode.PublicationOnly to avoid caching faulted factories.

Added ConfigureAwait(false) on awaited operations.

Added DebuggerDisplay and improved ToString() on registry results.

Serializer left as NotSupported; introduced StackTraceHidden to avoid noisy traces.

Improved XML documentation on public interfaces.