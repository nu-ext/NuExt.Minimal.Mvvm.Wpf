using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Minimal.Mvvm.Wpf.Controls;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Provides asynchronous methods to show and manage modal dialogs.
/// Extends DialogServiceBase and implements IAsyncDialogService interface.
/// </summary>
/// <remarks>
/// The dialog window is created with the style of the active theme, so it looks native in Fluent,
/// Aero and HighContrast without any special window type; set <see cref="WindowType"/> only when
/// a custom window is needed. The view and the command buttons are shown by
/// <see cref="InputDialogContent"/>; the buttons can be re-templated with
/// <see cref="ButtonTemplateSelector"/> or <see cref="ButtonTemplate"/>, and their spacing adjusted
/// with <see cref="ButtonRowMargin"/>.
/// </remarks>
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
    /// Identifies the <see cref="ButtonRowMargin"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ButtonRowMarginProperty = DependencyProperty.Register(
        nameof(ButtonRowMargin), typeof(Thickness), typeof(InputDialogService), new PropertyMetadata(new Thickness(12)));

    /// <summary>
    /// Identifies the <see cref="ButtonTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ButtonTemplateProperty = DependencyProperty.Register(
        nameof(ButtonTemplate), typeof(DataTemplate), typeof(InputDialogService));

    /// <summary>
    /// Identifies the <see cref="ButtonTemplateSelector"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ButtonTemplateSelectorProperty = DependencyProperty.Register(
        nameof(ButtonTemplateSelector), typeof(DataTemplateSelector), typeof(InputDialogService));

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InputDialogService"/> class with dialog friendly defaults:
    /// the dialog window has an owner and is centred on it.
    /// </summary>
    public InputDialogService()
    {
        // Dialogs are modal and centred on their parent by default; both can be changed in XAML.
        WindowHelper.SetSetWindowOwner(this, true);
        WindowHelper.SetWindowStartupLocation(this, WindowStartupLocation.CenterOwner);
    }

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
    /// when closing. If true, the service will prevent the dialog from closing if there are validation errors.
    /// This applies only if the ViewModel implements the <see cref="INotifyDataErrorInfo"/> interface.
    /// </summary>
    public bool ValidatesOnNotifyDataErrors
    {
        get => (bool)GetValue(ValidatesOnNotifyDataErrorsProperty);
        set => SetValue(ValidatesOnNotifyDataErrorsProperty, value);
    }

    /// <summary>
    /// Gets or sets the margin around the dialog button row: the gap to the dialog content above it
    /// and to the dialog edges.
    /// </summary>
    public Thickness ButtonRowMargin
    {
        get => (Thickness)GetValue(ButtonRowMarginProperty);
        set => SetValue(ButtonRowMarginProperty, value);
    }

    /// <summary>
    /// Gets or sets the template of a dialog button. A template has priority over a selector:
    /// when both are set, the selector is never called.
    /// </summary>
    public DataTemplate? ButtonTemplate
    {
        get => (DataTemplate?)GetValue(ButtonTemplateProperty);
        set => SetValue(ButtonTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template selector of the dialog buttons. When not set, the buttons use the
    /// theme-like templates of the library.
    /// </summary>
    public DataTemplateSelector? ButtonTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ButtonTemplateSelectorProperty);
        set => SetValue(ButtonTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog window should have an owner window.
    /// </summary>
    public bool SetWindowOwner
    {
        get => WindowHelper.GetSetWindowOwner(this);
        set => WindowHelper.SetSetWindowOwner(this, value);
    }

    /// <summary>
    /// Gets or sets the startup location of the dialog window.
    /// </summary>
    public WindowStartupLocation WindowStartupLocation
    {
        get => WindowHelper.GetWindowStartupLocation(this);
        set => WindowHelper.SetWindowStartupLocation(this, value);
    }

    /// <summary>
    /// Gets or sets the style applied to the dialog window.
    /// </summary>
    public Style? WindowStyle
    {
        get => WindowHelper.GetWindowStyle(this);
        set => WindowHelper.SetWindowStyle(this, value);
    }

    /// <summary>
    /// Gets or sets the resource key of the style applied to the dialog window.
    /// </summary>
    public string? WindowStyleKey
    {
        get => WindowHelper.GetWindowStyleKey(this);
        set => WindowHelper.SetWindowStyleKey(this, value);
    }

    /// <summary>
    /// Gets or sets the style selector choosing the style of the dialog window.
    /// </summary>
    public StyleSelector? WindowStyleSelector
    {
        get => WindowHelper.GetWindowStyleSelector(this);
        set => WindowHelper.SetWindowStyleSelector(this, value);
    }

    /// <summary>
    /// Gets or sets the type of window hosting the dialog. A plain <see cref="Window"/> is used when not set;
    /// a custom type must derive from <see cref="Window"/> and have a parameterless constructor.
    /// </summary>
    public Type? WindowType
    {
        get => WindowHelper.GetWindowType(this);
        set => WindowHelper.SetWindowType(this, value);
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
        var commands = dialogCommands as ICollection<UICommand> ?? dialogCommands.ToList();

        // Design note (internal): the dialog is deliberately not a Window subclass with its own
        // DefaultStyleKey. Under Application.ThemeMode WPF applies the implicit style of the base Window type
        // first, and its OverridesDefaultStyle disables the theme style lookup for the subclass, so such a
        // dialog loses the theme template and SizeToContent. Instead the layout lives in InputDialogContent
        // and the window is created by WindowHelper and keeps the style of the active theme.
        var content = new InputDialogContent
        {
            Content = view,
            CommandsSource = commands,
            ButtonRowMargin = ButtonRowMargin
        };

        if (ButtonTemplate != null)
        {
            content.ButtonTemplate = ButtonTemplate;
        }

        if (ButtonTemplateSelector != null)
        {
            content.ButtonTemplateSelector = ButtonTemplateSelector;
        }

        var dialog = WindowHelper.CreateWindow(this, content, viewModel, window => ConfigureDialog(window, title, viewModel));
        ViewModelHelper.SetDataContextBinding(view, FrameworkElement.DataContextProperty, dialog);

        if (SetTitleBinding)
        {
            ViewModelHelper.SetViewTitleBinding(view, Window.TitleProperty, dialog);
        }

        // await using: the registration is disposed asynchronously, so a cancellation callback that is
        // still running on another thread cannot block the UI thread we are on. IAsyncDisposable on
        // CancellationTokenRegistration exists from .NET Core 3.0 only, the net4x targets fall back to using.
#if NET
        await
#endif
        using var cancellationRegistration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(dialog.Close, useSynchronizationContext: true)
            : default;

        try
        {
            await ViewModelHelper.InitializeViewAsync(view, viewModel, parentViewModel, parameter, cancellationToken);
            return ShowDialog(dialog, view, commands, cancellationToken);
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

    /// <summary>
    /// Applies the dialog specific settings to the window created by <see cref="WindowHelper"/>.
    /// </summary>
    private void ConfigureDialog(Window window, string? title, object? viewModel)
    {
        window.Title = title ?? (viewModel != null ? ViewModelHelper.GetViewModelTitle(viewModel) : "Untitled");

        if (WindowType is { } windowType && windowType != typeof(Window))
        {
            // A custom window type is responsible for its own sizing and appearance.
            return;
        }

        // Local values beat any style setter, so the plain dialog stays content-sized in every theme:
        // under Application.ThemeMode the window visuals come from the theme style.
        // UseLayoutRounding is the modern way to keep edges crisp at fractional DPI; SnapsToDevicePixels
        // is the pre-4.0 legacy of the same idea, and mixing the two is explicitly discouraged.
        window.SizeToContent = SizeToContent.WidthAndHeight;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.UseLayoutRounding = true;
    }

    /// <summary>
    /// Shows the dialog and resolves the dialog result from the commands and the user's input.
    /// </summary>
    private UICommand? ShowDialog(Window dialog, object? view, ICollection<UICommand> commands, CancellationToken cancellationToken)
    {
        var cancelCommand = commands.FirstOrDefault(c => c.IsCancel);
        var defaultCommand = commands.FirstOrDefault(c => c.IsDefault);
        UICommand? selectedCommand = null;

        void OnButtonClick(object sender, RoutedEventArgs e)
        {
            // Buttons inside the dialog view carry a view model, not a UICommand, and are left alone.
            // A command button does its own work, so only commands without a Command are resolved here.
            if (e.OriginalSource is Button { Command: null, DataContext: UICommand command })
            {
                if (command != defaultCommand || !HasValidationErrors(view))
                {
                    selectedCommand = command;
                    dialog.DialogResult = command == defaultCommand;
                }
                e.Handled = true;
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e is { Key: Key.System, SystemKey: Key.F4 })
            {
                selectedCommand = cancelCommand;
                dialog.DialogResult = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                var result = defaultCommand;
                if (e.OriginalSource is Button { DataContext: UICommand command })
                {
                    result = command;
                }

                if (result != null)
                {
                    if (result != defaultCommand || !HasValidationErrors(view))
                    {
                        selectedCommand = result;
                        dialog.DialogResult = result == defaultCommand;
                    }
                }
                e.Handled = true;
            }
        }

        var onButtonClick = new RoutedEventHandler(OnButtonClick);
        dialog.AddHandler(ButtonBase.ClickEvent, onButtonClick);
        dialog.KeyDown += OnKeyDown;
        try
        {
            var result = dialog.ShowDialog();

            // The cancellation registration closes the dialog; the caller must see the cancellation,
            // not a silently dismissed dialog.
            cancellationToken.ThrowIfCancellationRequested();

            if (selectedCommand == null && result != true)
            {
                return cancelCommand;
            }
            return selectedCommand;
        }
        finally
        {
            dialog.RemoveHandler(ButtonBase.ClickEvent, onButtonClick);
            dialog.KeyDown -= OnKeyDown;
        }
    }

    private bool HasValidationErrors(object? view)
    {
        if (!ValidatesOnDataErrors && !ValidatesOnNotifyDataErrors)
        {
            return false;
        }
        var viewModel = ViewModelHelper.GetViewModelFromView(view);
        return viewModel switch
        {
            IDataErrorInfo dataErrorInfo when ValidatesOnDataErrors && !string.IsNullOrEmpty(dataErrorInfo.Error) => true,
            INotifyDataErrorInfo notifyDataErrorInfo when ValidatesOnNotifyDataErrors && notifyDataErrorInfo.HasErrors => true,
            _ => false
        };
    }

    #endregion
}
