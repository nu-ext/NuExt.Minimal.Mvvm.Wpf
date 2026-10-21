using Minimal.Mvvm.Wpf.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides asynchronous methods to show and manage modal dialogs.
/// Extends DialogServiceBase and implements IAsyncDialogService interface.
/// </summary>
public class InputDialogService : DialogServiceBase, IAsyncDialogService
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="SetTitleBinding"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SetTitleBindingProperty = DependencyProperty.Register(
        nameof(SetTitleBinding), typeof(bool), typeof(InputDialogService), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="ValidatesOnDataErrors"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValidatesOnDataErrorsProperty = DependencyProperty.Register(
        nameof(ValidatesOnDataErrors), typeof(bool), typeof(InputDialogService), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="ValidatesOnNotifyDataErrors"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValidatesOnNotifyDataErrorsProperty = DependencyProperty.Register(
        nameof(ValidatesOnNotifyDataErrors), typeof(bool), typeof(InputDialogService), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the <see cref="WindowStartupLocation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WindowStartupLocationProperty = DependencyProperty.Register(
        nameof(WindowStartupLocation), typeof(WindowStartupLocation), typeof(InputDialogService), new PropertyMetadata(WindowStartupLocation.CenterOwner));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether the service should set dialog title binding
    /// </summary>
    public bool SetTitleBinding
    {
        get => (bool)GetValue(SetTitleBindingProperty);
        set => SetValue(SetTitleBindingProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the service should check for validation errors
    /// when closing the dialog. If true, the service will prevent the dialog from closing if there are validation errors.
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

    public WindowStartupLocation WindowStartupLocation
    {
        get => (WindowStartupLocation)GetValue(WindowStartupLocationProperty);
        set => SetValue(WindowStartupLocationProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Displays a dialog asynchronously with the specified parameters.
    /// </summary>
    /// <param name="dialogCommands">A collection of UICommand objects representing the buttons available in the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="documentType">The type of the view to display within the dialog.</param>
    /// <param name="viewModel">The ViewModel associated with the view.</param>
    /// <param name="parentViewModel">The parent ViewModel for context.</param>
    /// <param name="parameter">The optional parameter for context.</param>
    /// <param name="cancellationToken">A token to cancel the dialog operation if needed.</param>
    /// <returns>A task whose result is the command selected by the user, or <see langword="null"/> if no command is available for the dialog result.</returns>
    public async ValueTask<UICommand?> ShowDialogAsync(IEnumerable<UICommand> dialogCommands, string? title, string? documentType, object? viewModel,
        object? parentViewModel, object? parameter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var view = await CreateViewAsync(documentType, cancellationToken);

        var dialog = new InputDialog
        {
            CommandsSource = dialogCommands,
            Content = view,
            Owner = GetWindow(),
            Title = title ?? (viewModel != null ? ViewModelHelper.GetViewModelTitle(viewModel) : "Untitled"),
            WindowStartupLocation = WindowStartupLocation,
        };
        ViewModelHelper.SetDataContextBinding(view, FrameworkElement.DataContextProperty, dialog);
        BindingOperations.SetBinding(dialog, InputDialog.ValidatesOnDataErrorsProperty, new Binding()
        {
            Path = new PropertyPath(ValidatesOnDataErrorsProperty),
            Source = this,
            Mode = BindingMode.OneWay
        });
        BindingOperations.SetBinding(dialog, InputDialog.ValidatesOnNotifyDataErrorsProperty, new Binding()
        {
            Path = new PropertyPath(ValidatesOnNotifyDataErrorsProperty),
            Source = this,
            Mode = BindingMode.OneWay
        });

        if (SetTitleBinding)
        {
            ViewModelHelper.SetViewTitleBinding(view, Window.TitleProperty, dialog);
        }

        try
        {
            await ViewModelHelper.InitializeViewAsync(view, viewModel, parentViewModel, parameter, cancellationToken);
            return dialog.ShowDialog(cancellationToken);
        }
        finally
        {
            BindingOperations.ClearAllBindings(dialog);
        }
    }

    /// <summary>
    /// Displays a dialog asynchronously with the specified parameters.
    /// </summary>
    /// <param name="dialogButtons">The buttons to be displayed in the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="documentType">The type of the view to display within the dialog.</param>
    /// <param name="viewModel">The ViewModel associated with the view.</param>
    /// <param name="parentViewModel">The parent ViewModel for context.</param>
    /// <param name="parameter">The optional parameter for context.</param>
    /// <param name="cancellationToken">A token to cancel the dialog operation if needed.</param>
    /// <returns>A <see cref="ValueTask{MessageBoxResult}"/> representing the user's action.</returns>
    public async ValueTask<MessageBoxResult> ShowDialogAsync(MessageBoxButton dialogButtons, string? title, string? documentType, object? viewModel,
        object? parentViewModel, object? parameter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetMessageBoxResult(await ShowDialogAsync(GetUICommands(dialogButtons), title, documentType, viewModel, parentViewModel, parameter, cancellationToken));
    }

    #endregion
}
