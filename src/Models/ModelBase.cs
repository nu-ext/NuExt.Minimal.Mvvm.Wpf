using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;

namespace Minimal.Mvvm;

/// <summary>
/// Provides a base class for models that supports property change notification and initialization.
/// It leverages reflection to manage properties and provides methods to get property types, initialize the object,
/// and set property values dynamically.
/// </summary>
public abstract class ModelBase : BindableBase
{
    private static readonly ConcurrentDictionary<Type, IDictionary<string, PropertyInfo>> s_typeProperties = new();

    private static readonly Func<Type, IDictionary<string, PropertyInfo>> s_typePropertiesFactory = GetProperties;

    private readonly Lock _initLock = new();
    private volatile bool _isInitialized;

    #region Properties
    /// <summary>
    /// Gets a value indicating whether the object has been initialized.
    /// </summary>
    [JsonIgnore]
    public bool IsInitialized => _isInitialized;

    #endregion

    #region Methods

    /// <summary>
    /// Gets the properties of the current type.
    /// </summary>
    /// <returns>A dictionary containing property information.</returns>
    protected IDictionary<string, PropertyInfo> GetProperties()
    {
        return s_typeProperties.GetOrAdd(GetType(), s_typePropertiesFactory);
    }

    /// <summary>
    /// Gets the properties of the specified type.
    /// </summary>
    /// <param name="type">The type to get properties for.</param>
    /// <returns>A dictionary containing property information.</returns>
    private static IDictionary<string, PropertyInfo> GetProperties(Type type)
    {
        var props = type.GetAllProperties(typeof(ModelBase), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var dict = new Dictionary<string, PropertyInfo>();
        foreach (var prop in props)
        {
            if (dict.TryAdd(prop.Name, prop))
            {
                continue;
            }
            Debug.Assert(prop.GetGetMethod()?.IsVirtual == true, $"Property '{prop.Name}' is not virtual");
        }
        return dict;
    }

    /// <summary>
    /// Gets the type of the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The type of the property, or null if the property is not found.</returns>
    public Type? GetPropertyType(string propertyName)
    {
        Debug.Assert(!string.IsNullOrEmpty(propertyName), "propertyName is null or empty");
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        Debug.Assert(GetProperties().ContainsKey(propertyName), $"propertyName '{propertyName}' is not defined");
        return GetProperties().TryGetValue(propertyName, out var pi) ? pi.PropertyType : null;
    }

    /// <summary>
    /// Initializes the object if it is not already initialized.
    /// </summary>
    /// <remarks>
    /// If the object is already initialized, this method does nothing.
    /// </remarks>
    public void Initialize()
    {
        lock (_initLock)
        {
            if (_isInitialized) return;
            InitializeCore();
            _isInitialized = true;
        }

        OnPropertyChanged(EventArgsCache.IsInitializedPropertyChanged);
    }

    /// <summary>
    /// Uninitializes the object if it is currently initialized, performing the necessary cleanup.
    /// </summary>
    /// <remarks>
    /// If the object is already uninitialized, this method does nothing. 
    /// </remarks>
    public void Uninitialize()
    {
        lock (_initLock)
        {
            if (!_isInitialized) return;
            UninitializeCore();
            _isInitialized = false;
        }

        OnPropertyChanged(EventArgsCache.IsInitializedPropertyChanged);
    }

    /// <summary>
    /// Called during initialization to allow derived classes to perform custom initialization logic.
    /// </summary>
    protected virtual void InitializeCore()
    {
    }

    /// <summary>
    /// Called during uninitialization to allow derived classes to perform custom uninitialization logic.
    /// </summary>
    protected virtual void UninitializeCore()
    {
    }

    /// <summary>
    /// Sets the value of the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>true if the property value was set successfully; otherwise, false.</returns>
    public bool SetProperty(string propertyName, object? value)
    {
        Debug.Assert(!string.IsNullOrEmpty(propertyName), "propertyName is null or empty");
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        Debug.Assert(GetProperties().ContainsKey(propertyName), $"propertyName '{propertyName}' is not defined");
        if (!GetProperties().TryGetValue(propertyName, out var pi) || !pi.CanWrite)
        {
            return false;
        }
        pi.SetValue(this, value);
        return true;
    }

    #endregion
}

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs IsInitializedPropertyChanged = new(nameof(ModelBase.IsInitialized));
}
