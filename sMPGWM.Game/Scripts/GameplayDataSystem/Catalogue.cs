using System;
using Newtonsoft.Json.Linq;

namespace sMPGWM.Scripts.GameplayDataSystem;

internal readonly record struct Catalogue<T>(CatalogueSource Source, Func<JToken, T> ParseDefinition);
