using ReactiveUI.Builder;
using System.Windows;
using YoutubeDl.Wpf.ViewModels;
using YoutubeDl.Wpf.Views;

namespace YoutubeDl.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithWpf()
            .RegisterView<AddArgumentView, AddArgumentViewModel>()
            .RegisterView<ArgumentChipView, ArgumentChipViewModel>()
            .RegisterView<GetStartedDialogView, GetStartedDialogViewModel>()
            .RegisterView<HistoryItemView, HistoryItemViewModel>()
            .RegisterView<HomeView, HomeViewModel>()
            .RegisterView<PresetDialogView, PresetDialogViewModel>()
            .RegisterView<SettingsView, SettingsViewModel>()
            .BuildApp();
    }
}
