using Presentation.Wpf;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minimal.Mvvm.Wpf.Controls;

/// <summary>
/// Interaction logic for InputDialog.xaml
/// </summary>
public partial class InputDialog : InputDialogBase
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="CommandsSource"/> dependency property.</summary>
    public static readonly DependencyProperty CommandsSourceProperty = DependencyProperty.Register(
        nameof(CommandsSource), typeof(IEnumerable), typeof(InputDialog));

    /// <summary>
    /// Identifies the <see cref="ValidatesOnDataErrors"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValidatesOnDataErrorsProperty = DependencyProperty.Register(
        nameof(ValidatesOnDataErrors), typeof(bool), typeof(InputDialog), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="ValidatesOnNotifyDataErrors"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValidatesOnNotifyDataErrorsProperty = DependencyProperty.Register(
        nameof(ValidatesOnNotifyDataErrors), typeof(bool), typeof(InputDialog), new PropertyMetadata(false));

    #endregion

    private UICommand? _tcs;

    public InputDialog()
    {
        InitializeComponent();
    }

    #region UI Commands

    private UICommand? CancelCommand => CommandsSource?.Cast<UICommand>().FirstOrDefault(c => c.IsCancel);

    private UICommand? DefaultCommand => CommandsSource?.Cast<UICommand>().FirstOrDefault(c => c.IsDefault);

    #endregion

    #region Properties

    /// <summary>
    /// UI commands.
    /// </summary>
    public IEnumerable? CommandsSource
    {
        get => (IEnumerable)GetValue(CommandsSourceProperty);
        set => SetValue(CommandsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog should check for validation errors
    /// when closing. If true, the dialog will prevent closing if there are validation errors.
    /// This applies only if the ViewModel implements the <see cref="IDataErrorInfo"/> interface.
    /// </summary>
    public bool ValidatesOnDataErrors
    {
        get => (bool)GetValue(ValidatesOnDataErrorsProperty);
        set => SetValue(ValidatesOnDataErrorsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog should check for validation errors
    /// when closing. If true, the dialog will prevent closing if there are validation errors.
    /// This applies only if the ViewModel implements the <see cref="INotifyDataErrorInfo"/> interface.
    /// </summary>
    public bool ValidatesOnNotifyDataErrors
    {
        get => (bool)GetValue(ValidatesOnNotifyDataErrorsProperty);
        set => SetValue(ValidatesOnNotifyDataErrorsProperty, value);
    }

    #endregion

    #region Event Handlers

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Button { DataContext: UICommand command })
        {
            if (command != DefaultCommand || !HasValidationErrors())
            {
                _tcs = command;
                DialogResult = command == DefaultCommand;
            }
            e.Handled = true;
        }
    }

    private void OnKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e is { Key: Key.System, SystemKey: Key.F4 })
        {
            _tcs = CancelCommand;
            DialogResult = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            var result = DefaultCommand;
            if (e.OriginalSource is Button { DataContext: UICommand command })
            {
                result = command;
            }

            if (result != null)
            {
                if (result != DefaultCommand || !HasValidationErrors())
                {
                    _tcs = result;
                    DialogResult = result == DefaultCommand;
                }
            }
            e.Handled = true;
        }
    }

    #endregion

    #region Methods

    private bool HasValidationErrors()
    {
        if (!ValidatesOnDataErrors && !ValidatesOnNotifyDataErrors)
        {
            return false;
        }
        var viewModel = ViewModelHelper.GetViewModelFromView(Content);
        return viewModel switch
        {
            IDataErrorInfo dataErrorInfo when ValidatesOnDataErrors && !string.IsNullOrEmpty(dataErrorInfo.Error) => true,
            INotifyDataErrorInfo notifyDataErrorInfo when ValidatesOnNotifyDataErrors && notifyDataErrorInfo.HasErrors => true,
            _ => false
        };
    }

    private Lifetime SubscribeMetroDialog(CancellationToken cancellationToken)
    {
        var lifetime = new Lifetime()
#if DEBUG
                .SetDebugInfo()
#endif
            ;
        if (cancellationToken.CanBeCanceled)
        {
            lifetime.AddDisposable(cancellationToken.Register(Close, useSynchronizationContext: true));
        }

        if (DialogBottom != null && DialogButtons != null)
        {
            foreach (Button button in DialogButtons.FindChildren<Button>())
            {
                if (button.Command is null && button.DataContext is UICommand command)
                {
                    lifetime.AddBracket(() => button.Click += OnButtonClick, () => button.Click -= OnButtonClick);
                    lifetime.AddBracket(() => button.KeyDown += OnKeyDownHandler, () => button.KeyDown -= OnKeyDownHandler);
                }
            }
        }

        lifetime.AddBracket(() => KeyDown += OnKeyDownHandler, () => KeyDown -= OnKeyDownHandler);

        return lifetime;
    }

    public UICommand? ShowDialog(CancellationToken cancellationToken)
    {
        Lifetime? subscription = null;
        void OnDialogContentRendered(object? sender, EventArgs e)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            subscription ??= SubscribeMetroDialog(cancellationToken);
        }
        ContentRendered += OnDialogContentRendered;
        try
        {
            var res = ShowDialog();
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Assert(res == DialogResult && res != null);
            if (_tcs == null && res != true)
            {
                return CancelCommand;
            }
            return _tcs;
        }
        finally
        {
            ContentRendered -= OnDialogContentRendered;
            Disposable.DisposeAndNull(ref subscription);
        }
    }

    #endregion
}
