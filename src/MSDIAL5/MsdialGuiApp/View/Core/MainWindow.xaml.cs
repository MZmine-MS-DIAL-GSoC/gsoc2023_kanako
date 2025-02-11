﻿using CompMs.App.Msdial.Model.Notification;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.App.Msdial.View.Search;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.View.Statistics;
using CompMs.App.Msdial.View.Table;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.UI;
using CompMs.Graphics.UI.Message;
using CompMs.Graphics.UI.ProgressBar;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow() {
            InitializeComponent();

            var compoundSearchService = new DialogService<CompoundSearchWindow, CompoundSearchVM>(this);
            var peakSpotTableService = new DialogService<AlignmentSpotTable, PeakSpotTableViewModelBase>(this);
            var proteomicsTableService = new DialogService<ProteomicsSpotTable, PeakSpotTableViewModelBase>(this);
            var analysisFilePropertyResetService = new DialogService<AnalysisFilePropertyResettingWindow, AnalysisFilePropertyResetViewModel>(this);
            var processSettingDialogService = new DialogService<ProjectSettingDialog, ProcessSettingViewModel>(this);
            DataContext = new MainWindowVM(
                compoundSearchService,
                peakSpotTableService,
                analysisFilePropertyResetService,
                proteomicsTableService,
                processSettingDialogService);

            broker = MessageBroker.Default;

            broker.ToObservable<ProgressBarMultiContainerRequest>()
                .Subscribe(ShowMultiProgressBarWindow);
            broker.ToObservable<ProgressBarRequest>()
                .Subscribe(ShowProgressBarWindow);
            broker.ToObservable<ShortMessageRequest>()
                .Subscribe(ShowShortMessageDialog);
            broker.ToObservable<ProcessMessageRequest>()
                .Subscribe(ShowProcessMessageDialog);
            broker.ToObservable<FileClassSetViewModel>()
                .Subscribe(ShowFileClassSetView);
            broker.ToObservable<SaveFileNameRequest>()
                .Subscribe(GetSaveFilePath);
            broker.ToObservable<OpenFileRequest>()
                .Subscribe(OpenFileDialog);
            broker.ToObservable<ErrorMessageBoxRequest>()
                .Subscribe(ShowErrorConfirmationMessage);
            broker.ToObservable<AlignedChromatogramModificationViewModelLegacy>()
                .Subscribe(CreateAlignedChromatogramModificationDialog);
            broker.ToObservable<SampleTableViewerInAlignmentViewModelLegacy>()
                .Subscribe(CreateSampleTableViewerDialog);
            broker.ToObservable<InternalStandardSetViewModel>()
                .Subscribe(OpenInternalStandardSetView);
            broker.ToObservable<PCAPLSResultViewModel>()
                .Subscribe(OpenPCAPLSResultView);
            broker.ToObservable<ExperimentSpectrumViewModel>()
                .Subscribe(ShowChildView<ExperimentSpectrumView>);
            broker.ToObservable<ProteinGroupTableViewModel>()
                .Subscribe(ShowChildView<ProteinGroupTable>);
            broker.ToObservable<ChromatogramsViewModel>()
                .Subscribe(ShowChildView<DisplayChromatogramsView>);
            broker.ToObservable<DisplayEicSettingViewModel>()
                .Subscribe(ShowChildDialog<EICDisplaySettingView>);
            broker.ToObservable<NormalizationSetViewModel>()
                .Subscribe(ShowChildDialog<NormalizationSetView>);
            broker.ToObservable<MultivariateAnalysisSettingViewModel>()
                .Subscribe(ShowChildView<MultivariateAnalysisSettingView>);
            broker.ToObservable<AnalysisResultExportViewModel>()
                .Subscribe(ShowChildDialog<AnalysisResultExportWin>);
            broker.ToObservable<AlignmentResultExportViewModel>()
                .Subscribe(ShowChildDialog<AlignmentResultExportWin>);
            broker.ToObservable<TargetCompoundLibrarySettingViewModel>()
                .Subscribe(OpenTargetCompoundLibrarySettingView);
            broker.ToObservable<FindTargetCompoundsSpotViewModel>()
                .Subscribe(ShowFindTargetCompoundsSpotView);
#if RELEASE
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#elif DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;
#endif
        }

        private readonly IMessageBroker broker;

        public void CloseOwnedWindows() {
            Dispatcher.Invoke(() =>
            {
                foreach (var child in OwnedWindows.OfType<Window>()) {
                    if (child.IsLoaded) {
                        child.Close();
                    }
                }
            });
        }

        private void ShowChildView<TView>(object viewmodel) where TView : Window, new() {
            var view = new TView()
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            view.Show();
        }

        private void ShowChildViewWithDispose<TView>(object viewmodel) where TView : Window, new() {
            var view = new TView()
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            DataContextCleanupBehavior.SetIsEnabled(view, true);
            view.Show();
        }

        private void ShowChildDialog<TView>(object viewmodel) where TView : Window, new() {
            var view = new TView()
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            view.ShowDialog();
        }

        private void ShowMultiProgressBarWindow(ProgressBarMultiContainerRequest request) {
            using (var viewmodel = new ProgressBarMultiContainerVM(request)) {
                var dialog = new ProgressBarMultiContainerWindow
                {
                    DataContext = viewmodel,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                dialog.Loaded += async (s, e) =>
                {
                    await viewmodel.RunAsync().ConfigureAwait(false);
                    request.Result = true;
                    dialog.Dispatcher.Invoke(dialog.Close);
                };
                dialog.ShowDialog();
            }
        }

        private void ShowProgressBarWindow(ProgressBarRequest request) {
            using (var viewmodel = new ProgressBarVM(request)) {
                var dialog = new ProgressBarWindow
                {
                    DataContext = viewmodel,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                dialog.Loaded += async (s, e) =>
                {
                    await request.AsyncAction.Invoke(viewmodel).ConfigureAwait(false);
                    request.Result = true;
                    dialog.Dispatcher.Invoke(dialog.Close);
                };
                dialog.ShowDialog();
            }
        }

        private void ShowShortMessageDialog(ShortMessageRequest request) {
            var dialog = new ShortMessageWindow
            {
                DataContext = request.Content,
                Text = request.Content,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            request.Result = dialog.ShowDialog();
        }

        private void ShowProcessMessageDialog(ProcessMessageRequest request) {
            var dialog = new ShortMessageWindow
            {
                DataContext = request.Content,
                Text = request.Content,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            dialog.Loaded += async (s, e) =>
            {
                await request.AsyncAction();
                dialog.Dispatcher.Invoke(() =>
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                });
            };
            request.Result = dialog.ShowDialog();
        }

        private void ShowFileClassSetView(FileClassSetViewModel viewmodel) {
            var dialog = new SettingDialog
            {
                Height = 450, Width = 400,
                Title = "Class property setting",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new FileClassSetView
                {
                    DataContext = viewmodel,
                },
                ApplyCommand = viewmodel.ApplyCommand,
                FinishCommand = viewmodel.ApplyCommand,
                CancelCommand = viewmodel.CancelCommand,
            };
            dialog.Show();
        }

        private void OpenInternalStandardSetView(InternalStandardSetViewModel viewmodel) {
            var dialog = new SettingDialog
            {
                Height = 600, Width = 800,
                Title = "Internal standard settting",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new InternalStandardSetView
                {
                    DataContext = viewmodel,
                },
                ApplyCommand = null,
                FinishCommand = viewmodel.ApplyCommand,
                CancelCommand = viewmodel.CancelCommand,
            };
            dialog.Show();
        }

        private void ShowFindTargetCompoundsSpotView(FindTargetCompoundsSpotViewModel viewmodel) {
            var dialog = new SettingDialog
            {
                Height = 800, Width = 400,
                Title = "Find match peak spots",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new FindTargetCompoundsSpotView
                {
                    DataContext = viewmodel,
                },
                ApplyCommand = CommonMVVM.NeverCommand.Instance,
                FinishCommand = CommonMVVM.IdentityCommand.Instance,
                CancelCommand = CommonMVVM.NeverCommand.Instance,
            };
            dialog.Show();
        }

        private void OpenTargetCompoundLibrarySettingView(TargetCompoundLibrarySettingViewModel viewmodel) {
            var dialog = new SettingDialog
            {
                Height = 600, Width = 400,
                Title = "Select library file",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TargetCompoundsLibrarySettingView
                {
                    DataContext = viewmodel,
                },
                ApplyCommand = CommonMVVM.NeverCommand.Instance,
                FinishCommand = CommonMVVM.IdentityCommand.Instance,
                CancelCommand = CommonMVVM.NeverCommand.Instance,
            };
            dialog.ShowDialog();
        }

        private void OpenPCAPLSResultView(PCAPLSResultViewModel viewmodel) {
            var dialog = new Window
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new MultivariateAnalysisResultView(),
            };
            dialog.Show();
        }

        private void GetSaveFilePath(SaveFileNameRequest request) {
            var sfd = new SaveFileDialog
            {
                Title = request.Title,
                Filter = request.Filter,
                RestoreDirectory = request.RestoreDirectory,
                AddExtension = request.AddExtension,
            };

            request.Result = sfd.ShowDialog(this);
            if (request.Result == true) {
                request.Run(sfd.FileName);
            }
        }

        private void OpenFileDialog(OpenFileRequest request) {
            var ofd = new OpenFileDialog
            {
                Title = request.Title,
                Filter = request.Filter,
            };

            if (ofd.ShowDialog(this) == true) {
                request.Run(ofd.FileName);
            }
        }

        private void ShowErrorConfirmationMessage(ErrorMessageBoxRequest request) {
            Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(request.Content, request.Caption, request.ButtonType, MessageBoxImage.Error);
                request.Result = result;
            });
        }

        private void CreateAlignedChromatogramModificationDialog(AlignedChromatogramModificationViewModelLegacy vm) {
            Dispatcher.Invoke(() =>
            {
                var window = new AlignedPeakCorrectionWinLegacy()
                {
                    DataContext = vm,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Closed += (s, e) => vm.Dispose();
                window.Show();
            });
        }

        private void CreateSampleTableViewerDialog(SampleTableViewerInAlignmentViewModelLegacy vm) {
            Dispatcher.Invoke(() => {
                var window = new SampleTableViewerInAlignmentLegacy() {
                    DataContext = vm,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Closed += (s, e) => vm.Dispose();
                window.Show();
            });
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);

            if (GlobalResources.Instance.IsLabPrivate) {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            var window = new ShortMessageWindow() {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Checking for updates.."
            };
            window.Show();
            VersionUpdateNotificationService.CheckForUpdates();
            window.Close();

            Mouse.OverrideCursor = null;
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);

            if (DataContext is MainWindowVM vm && vm.TaskProgressCollection.Any()) {
                var result = MessageBox.Show(
                    "A process is running in the background.\n" +
                    "If the application is terminated, the project may be corrupted.\n" +
                    "Do you want to close the application?",
                    "Warning",
                    MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK) {
                    e.Cancel = true;
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
