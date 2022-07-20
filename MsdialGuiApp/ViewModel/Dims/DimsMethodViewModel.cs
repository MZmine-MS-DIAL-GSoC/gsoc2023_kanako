﻿using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.Export;
using CompMs.MsdialDimsCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class DimsMethodViewModel : MethodViewModel {
        private readonly DimsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControlManager;

        private DimsMethodViewModel(
            DimsMethodModel model,
            IMessageBroker broker,
            IReadOnlyReactiveProperty<DimsAnalysisViewModel> analysisVM,
            IReadOnlyReactiveProperty<DimsAlignmentViewModel> alignmentVM,
            ViewModelSwitcher chromatogramViewModels,
            ViewModelSwitcher massSpectrumViewModels,
            FocusControlManager focusControlManager)
            : base(model, analysisVM, alignmentVM, chromatogramViewModels, massSpectrumViewModels) {

            _model = model;
            _broker = broker;
            _focusControlManager = focusControlManager.AddTo(Disposables);

            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilterModel).AddTo(Disposables);
        }

        public bool RefMatchedChecked {
            get => ReadDisplayFilter(DisplayFilter.RefMatched);
            set => WriteDisplayFilter(DisplayFilter.RefMatched, value);
        }
        public bool SuggestedChecked {
            get => ReadDisplayFilter(DisplayFilter.Suggested);
            set => WriteDisplayFilter(DisplayFilter.Suggested, value);
        }
        public bool UnknownChecked {
            get => ReadDisplayFilter(DisplayFilter.Unknown);
            set => WriteDisplayFilter(DisplayFilter.Unknown, value);
        }
        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilter(DisplayFilter.Ms2Acquired);
            set => WriteDisplayFilter(DisplayFilter.Ms2Acquired, value);
        }
        public bool MolecularIonChecked {
            get => ReadDisplayFilter(DisplayFilter.MolecularIon);
            set => WriteDisplayFilter(DisplayFilter.MolecularIon, value);
        }
        public bool BlankFilterChecked {
            get => ReadDisplayFilter(DisplayFilter.Blank);
            set => WriteDisplayFilter(DisplayFilter.Blank, value);
        }
        public bool UniqueIonsChecked {
            get => ReadDisplayFilter(DisplayFilter.UniqueIons);
            set => WriteDisplayFilter(DisplayFilter.UniqueIons, value);
        }
        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilter(DisplayFilter.ManuallyModified);
            set => WriteDisplayFilter(DisplayFilter.ManuallyModified, value);
        }
        private DisplayFilter displayFilters = 0;

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || _model.AnalysisFile == analysisFile.File) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File == null || _model.AlignmentFile == alignmentFile.File) {
                return Task.CompletedTask;
            }
            return _model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;

        private void ExportAnalysis(Window owner) {
            var container = _model.Storage;
            var spectraTypes = new List<Model.Export.SpectraType>
            {
                new Model.Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted)),
                new Model.Export.SpectraType(
                    ExportspectraType.centroid,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                new Model.Export.SpectraType(
                    ExportspectraType.profile,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Model.Export.SpectraFormat>
            {
                new Model.Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(container.AnalysisFiles, spectraTypes, spectraFormats, _model.ProviderFactory)) {

                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(ExportAlignment));

        public PeakFilterViewModel PeakFilterViewModel { get; }

        private DelegateCommand<Window> exportAlignmentResultCommand;

        private void ExportAlignment(Window owner) {
            var container = _model.Storage;
            var metadataAccessor = new DimsMetadataAccessor(container.DataBaseMapper, container.Parameter);
            var vm = new AlignmentResultExport2VM(_model.AlignmentFile, _model.AlignmentFiles, container, _broker);
            vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.Parameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.Parameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.Parameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.Parameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.Parameter), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.Parameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.Parameter), "MsmsIncluded"),
                });
            var dialog = new AlignmentResultExportWin
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        private bool ReadDisplayFilter(DisplayFilter flag) {
            return (displayFilters & flag) != 0;
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            if (set) {
                displayFilters |= flag;
            }
            else {
                displayFilters &= (~flag);
            }
            OnPropertyChanged(nameof(displayFilters));
        }

        private static IReadOnlyReactiveProperty<DimsAnalysisViewModel> ConvertToAnalysisViewModel(
            DimsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<DimsAnalysisViewModel> result;
            using (var subject = new Subject<DimsAnalysisModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new DimsAnalysisViewModel(m, compoundSearchService, peakSpotTableService, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<DimsAlignmentViewModel> ConvertToAlignmentViewModel(
            DimsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            return method.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new DimsAlignmentViewModel(m, compoundSearchService, peakSpotTableService, broker, focusControlManager))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim();
        }

        public static DimsMethodViewModel Create(
            DimsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisVM = ConvertToAnalysisViewModel(model, compoundSearchService, peakSpotTableService, focusControlManager);
            var alignmentVM = ConvertToAlignmentViewModel(model, compoundSearchService, peakSpotTableService, broker, focusControlManager);
            var chromvms = PrepareChromatogramViewModels(analysisVM, alignmentVM);
            var msvms = PrepareMassSpectrumViewModels(analysisVM, alignmentVM);
            return new DimsMethodViewModel(model, broker, analysisVM, alignmentVM, chromvms, msvms, focusControlManager);
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<DimsAnalysisViewModel> analysisAsObservable, IObservable<DimsAlignmentViewModel> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<DimsAnalysisViewModel> analysisAsObservable, IObservable<DimsAlignmentViewModel> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = Observable.Return<ViewModelBase>(null); // analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = Observable.Return<ViewModelBase>(null); // ms2 chrom
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase>[] { rawdec, ms2chrom, rawpur, repref});
        }
    }
}