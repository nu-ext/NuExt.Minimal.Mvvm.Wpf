using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;

namespace Minimal.Mvvm;

/// <summary>
/// Provides a base class for models that track changes to their properties.
/// </summary>
public abstract class ChangeTrackingModelBase : ModelBase, IChangeTracking
{
    private sealed class ChangeTrackingSuspender : IDisposable
    {
        private readonly ChangeTrackingModelBase _owner;
        private int _isDisposed;

        public ChangeTrackingSuspender(ChangeTrackingModelBase owner)
        {
            _owner = owner;
            Interlocked.Increment(ref owner._changeTrackingSuspensionCount);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
                Interlocked.Decrement(ref _owner._changeTrackingSuspensionCount);
        }
    }

    private volatile int _changeTrackingSuspensionCount;

    /// <inheritdoc />
    [JsonIgnore]
    public bool IsChanged
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(EventArgsCache.IsChangedPropertyChanged);
        }
    }

    private bool IsChangeTrackingSuspended => _changeTrackingSuspensionCount != 0;

    /// <inheritdoc />
    public void AcceptChanges()
    {
        if (!IsChanged)
            return;

        AcceptChangesCore();
        IsChanged = false;
    }

    /// <summary>
    /// Called before the model is marked as unchanged to allow derived classes to accept the current state.
    /// </summary>
    /// <remarks>
    /// Derived classes can override this method to discard any state retained for change tracking.
    /// </remarks>
    protected virtual void AcceptChangesCore()
    {
    }

    /// <summary>
    /// Marks the model as changed.
    /// </summary>
    protected void MarkChanged()
    {
        if (IsInitialized && !IsChangeTrackingSuspended)
            IsChanged = true;
    }

    /// <summary>
    /// Temporarily suspends change tracking.
    /// </summary>
    /// <returns>
    /// An object that resumes change tracking when disposed.
    /// </returns>
    public IDisposable SuspendChangeTracking()
    {
        return new ChangeTrackingSuspender(this);
    }

    /// <summary>
    /// Determines whether a property change affects the changed state.
    /// </summary>
    protected virtual bool IsTrackedProperty(string? propertyName)
    {
        return propertyName is not null and not nameof(IsInitialized) and not nameof(IsChanged);
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsTrackedProperty(e.PropertyName))
            MarkChanged();
    }
}

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs IsChangedPropertyChanged = new(nameof(IChangeTracking.IsChanged));
}