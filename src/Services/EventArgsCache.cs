using System.ComponentModel;

namespace Minimal.Mvvm.Wpf;

internal static partial class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs CountPropertyChanged = new(nameof(IAsyncDocumentManagerService.Count));
    internal static readonly PropertyChangedEventArgs TitlePropertyChanged = new(nameof(IAsyncDocument.Title));
    internal static readonly PropertyChangedEventArgs ViewModelPropertyChanged = new(nameof(IViewModelProvider.ViewModel));
}