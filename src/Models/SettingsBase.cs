using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Minimal.Mvvm;

/// <summary>
/// Provides a base class for settings objects, extending the functionality of <see cref="ModelBase"/>.
/// This class supports property change notifications, initialization, and dirty state management.
/// It includes mechanisms to suspend and resume changes, as well as to mark the object as "dirty" when properties change.
/// </summary>
public abstract class SettingsBase : ModelBase
{
    #region Internal Classes

    /// <summary>
    /// A helper class to manage the suspension of the "dirty" state.
    /// </summary>
    private sealed class DirtySuspender : IDisposable
    {
        private readonly SettingsBase _this;
        private int _isDisposed;

        public DirtySuspender(SettingsBase self)
        {
            _this = self;
            Interlocked.Increment(ref _this._isDirtySuspended);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                Interlocked.Decrement(ref _this._isDirtySuspended);
            }
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Stores additional data not mapped to other properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }

    /// <summary>
    /// Gets a value indicating whether the object has been modified since its creation or last reset.
    /// </summary>
    [JsonIgnore]
    public bool IsDirty
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(EventArgsCache.IsDirtyPropertyChanged);
        }
    }

    /// <summary>
    /// Gets a value indicating whether property changes are currently suspended.
    /// </summary>
    [JsonIgnore]
    public bool IsSuspended
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(EventArgsCache.IsSuspendedPropertyChanged);
        }
    }

    private volatile int _isDirtySuspended;
    /// <summary>
    /// Gets a value indicating whether the dirty state tracking is currently suspended.
    /// </summary>
    private bool IsDirtySuspended => _isDirtySuspended != 0;

    #endregion

    #region Event Handlers

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MakeDirty(e.PropertyName);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Validates the specified property value before it is set.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is valid; otherwise, false.</returns>
    protected virtual bool IsValidPropertyValue<T>(string? propertyName, T value)
    {
        return true;
    }

    /// <inheritdoc />
    protected override bool CanSetProperty<T>(T oldValue, T newValue, [CallerMemberName] string? propertyName = null)
    {
        Debug.Assert(propertyName != nameof(IsDirty) && propertyName != nameof(IsSuspended));
        if (IsSuspended || !IsValidPropertyValue(propertyName, newValue))
        {
            return false;
        }
        return base.CanSetProperty(oldValue, newValue, propertyName);
    }

    /// <summary>
    /// Marks the object as dirty based on the provided property name.
    /// </summary>
    /// <param name="propertyName">The name of the property that has been changed.</param>
    protected void MakeDirty(string? propertyName)
    {
        if (!IsInitialized || IsDirtySuspended ||
            propertyName is null or nameof(IsInitialized) or nameof(IsDirty) or nameof(IsSuspended))
        {
            return;
        }
        IsDirty = true;
    }

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();
        PropertyChanged += OnPropertyChanged;
    }

    /// <inheritdoc />
    protected override void UninitializeCore()
    {
        PropertyChanged -= OnPropertyChanged;
        base.UninitializeCore();
    }

    /// <summary>
    /// Resets the dirty state of the object.
    /// </summary>
    public void ResetDirty()
    {
        IsDirty = false;
    }

    /// <summary>
    /// Resumes property change notifications after they were suspended.
    /// </summary>
    public void ResumeChanges()
    {
        IsSuspended = false;
    }

    /// <summary>
    /// Suspends property changes.
    /// </summary>
    public void SuspendChanges()
    {
        IsSuspended = true;
    }

    /// <summary>
    /// Suspends the dirty state tracking and returns a disposable object that resumes it when disposed.
    /// </summary>
    /// <returns>A disposable object that resumes dirty state tracking when disposed.</returns>
    public IDisposable SuspendDirty()
    {
        return new DirtySuspender(this);
    }

    #endregion
}

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs IsDirtyPropertyChanged = new(nameof(SettingsBase.IsDirty));
    internal static readonly PropertyChangedEventArgs IsSuspendedPropertyChanged = new(nameof(SettingsBase.IsSuspended));
}
