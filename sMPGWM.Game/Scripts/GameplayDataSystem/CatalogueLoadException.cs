using System;

namespace sMPGWM.Scripts.GameplayDataSystem;

internal sealed class CatalogueLoadException(CatalogueSource source, string message, Exception? inner = null)
    : ArgumentException($"{source.UserPath} ({source.SchemaPath}): {message}", inner)
{
    internal CatalogueSource CatalogueSource { get; } = source;
}
