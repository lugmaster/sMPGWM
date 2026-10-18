using System;

namespace sMPGWM.Scripts.TagSystem.Exceptions;

/// <summary>
/// Reports an invalid tag-system operation.
/// </summary>
/// <param name="message">The description of the invalid operation.</param>
public class TagException(string message) : InvalidOperationException(message);
