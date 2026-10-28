using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Minimal.Mvvm.Wpf.Serialization;

/// <summary>
/// Provides methods for serializing and deserializing object state as JSON.
/// </summary>
public static class JsonObjectStateSerializer
{
    private static readonly JsonDocumentOptions s_documentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Deserializes JSON object properties and assigns their values using the specified delegates.
    /// </summary>
    /// <param name="json">The JSON string containing the object state.</param>
    /// <param name="getPropertyType">A function that returns the expected type of a property by its name.</param>
    /// <param name="setPropertyValue">A function that assigns a deserialized value to a property.</param>
    /// <param name="options">Options for JSON deserialization.</param>
    /// <returns>
    /// A dictionary containing properties for which <paramref name="getPropertyType"/> returned
    /// <see langword="null"/>, or <see langword="null"/> if all properties were recognized.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="getPropertyType"/> or <paramref name="setPropertyValue"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A deserialized property value could not be assigned.
    /// </exception>
    /// <exception cref="JsonException">
    /// The JSON is invalid, does not represent an object, or a property value cannot be deserialized
    /// to its expected type.
    /// </exception>
    public static Dictionary<string, JsonElement>? Deserialize(string json, Func<string, Type?> getPropertyType, Func<string, object?, bool> setPropertyValue, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(getPropertyType);
        ArgumentNullException.ThrowIfNull(setPropertyValue);

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        //var json = JsonSerializer.Deserialize<JsonDocument>(json);
        Dictionary<string, JsonElement>? additionalData = null;

        using var document = JsonDocument.Parse(json, s_documentOptions);
        JsonElement root = document.RootElement;

        foreach (JsonProperty property in root.EnumerateObject())
        {
            string name = property.Name;
            var value = property.Value;
            var type = getPropertyType(name);

            if (type == null)
            {
                additionalData ??= [];
                additionalData[name] = value.Clone();
                continue;
            }

            var propertyValue = value.Deserialize(type, options);
            if (!setPropertyValue(name, propertyValue))
            {
                throw new InvalidOperationException($"Failed to set property '{name}'.");
            }
        }

        return additionalData;
    }

    /// <summary>
    /// Serializes an object's runtime state to a JSON string.
    /// </summary>
    /// <typeparam name="T">The declared type of the object.</typeparam>
    /// <param name="source">The object to serialize.</param>
    /// <param name="options">Options for JSON serialization.</param>
    /// <returns>A JSON string, or <see langword="null"/> if <paramref name="source"/> is <see langword="null"/>.</returns>
    public static string? Serialize<T>(T? source, JsonSerializerOptions? options = null)
    {
        if (source is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(source, source.GetType(), options);
    }
}