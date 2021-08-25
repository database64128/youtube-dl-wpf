using MaterialDesignThemes.Wpf;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using YoutubeDl.Wpf.Utils;

namespace YoutubeDl.Wpf.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class HomeView
    {
        public HomeView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                // Link and Start
                this.Bind(ViewModel,
                    viewModel => viewModel.Link,
                    view => view.linkTextBox.Text)
                    .DisposeWith(disposables);

                linkTextBox.Events().KeyDown
                           .Where(x => x.Key == Key.Enter)
                           .Select(x => Unit.Default)
                           .InvokeCommand(ViewModel!.StartDownloadCommand) // Null forgiving reason: upstream limitation.
                           .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.DownloadButtonProgressPercentageString,
                    view => view.downloadButton.Content)
                    .DisposeWith(disposables);

                // ButtonProgressAssist bindings
                ViewModel.WhenAnyValue(x => x.FreezeButton)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        ButtonProgressAssist.SetIsIndicatorVisible(downloadButton, x);
                        ButtonProgressAssist.SetIsIndicatorVisible(listFormatsButton, x);
                    })
                    .DisposeWith(disposables);

                ViewModel.WhenAnyValue(x => x.DownloadButtonProgressIndeterminate)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => ButtonProgressAssist.SetIsIndeterminate(downloadButton, x))
                    .DisposeWith(disposables);

                ViewModel.WhenAnyValue(x => x.DownloadButtonProgressPercentageValue)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => ButtonProgressAssist.SetValue(downloadButton, x))
                    .DisposeWith(disposables);

                ViewModel.WhenAnyValue(x => x.FormatsButtonProgressIndeterminate)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => ButtonProgressAssist.SetIsIndeterminate(listFormatsButton, x))
                    .DisposeWith(disposables);

                // containerComboBox
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.ContainerList,
                    view => view.containerComboBox.ItemsSource)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.Container,
                    view => view.containerComboBox.Text)
                    .DisposeWith(disposables);

                // formatComboBox
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.FormatList,
                    view => view.formatComboBox.ItemsSource)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.Format,
                    view => view.formatComboBox.Text)
                    .DisposeWith(disposables);

                // Options
                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.AddMetadata,
                    view => view.metadataToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.DownloadThumbnail,
                    view => view.thumbnailToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.DownloadSubtitles,
                    view => view.subtitlesToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.DownloadPlaylist,
                    view => view.playlistToggle.IsChecked)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.UseCustomPath,
                    view => view.pathToggle.IsChecked)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Settings.UseCustomPath,
                    view => view.pathTextBox.IsEnabled)
                    .DisposeWith(disposables);

                this.Bind(ViewModel,
                    viewModel => viewModel.Settings.DownloadPath,
                    view => view.pathTextBox.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Settings.UseCustomPath,
                    view => view.browseButton.IsEnabled)
                    .DisposeWith(disposables);

                // Output
                this.Bind(ViewModel,
                    viewModel => viewModel.Output,
                    view => view.resultTextBox.Text)
                    .DisposeWith(disposables);

                resultTextBox.Events().TextChanged
                             .Where(_ => WpfHelper.IsScrolledToEnd(resultTextBox))
                             .Subscribe(_ => resultTextBox.ScrollToEnd())
                             .DisposeWith(disposables);

                // Download, list, abort button
                this.BindCommand(ViewModel,
                    viewModel => viewModel.StartDownloadCommand,
                    view => view.downloadButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.ListFormatsCommand,
                    view => view.listFormatsButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.AbortDlCommand,
                    view => view.abortButton)
                    .DisposeWith(disposables);

                // Browse and open folder button
                this.BindCommand(ViewModel,
                    viewModel => viewModel.BrowseDownloadFolderCommand,
                    view => view.browseButton)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.OpenDownloadFolderCommand,
                    view => view.openFolderButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
