﻿using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Imms;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Export;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsMethodModel : MethodModelBase
    {
        static ImmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public ImmsMethodModel(
            MsdialImmsDataStorage storage)
            : base(storage.AnalysisFiles, storage.AlignmentFiles) {
            Storage = storage;
        }


        public ImmsAnalysisModel AnalysisModel {
            get => analysisModel;
            set {
                var old = analysisModel;
                if (SetProperty(ref analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAnalysisModel analysisModel;

        public ImmsAlignmentModel AlignmentModel {
            get => alignmentModel;
            set {
                var old = alignmentModel;
                if (SetProperty(ref alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAlignmentModel alignmentModel;

        public MsdialImmsDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialImmsDataStorage storage;

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; private set; }

        public int InitializeNewProject(Window window) {
            // Set analysis param
            if (!ProcessSetAnalysisParameter(window))
                return -1;

            var processOption = Storage.MsdialImmsParameter.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(window, Storage))
                    return -1;
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(window, Storage))
                    return -1;
            }

            return 0;
        }

        public void Load() {
            var parameter = Storage.MsdialImmsParameter;
            if (parameter.ProviderFactoryParameter is null) {
                parameter.ProviderFactoryParameter = new ImmsAverageDataProviderFactoryParameter(0.01, 0.02, 0, 100);
            }
            ProviderFactory = parameter?.ProviderFactoryParameter.Create(5, true);
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var parameter = Storage.MsdialImmsParameter;
            var analysisParameterSet = new ImmsAnalysisParameterSetModel(parameter, AnalysisFiles);
            using (var analysisParamSetVM = new ImmsAnalysisParameterSetViewModel(analysisParameterSet)) {
                var apsw = new AnalysisParamSetForImmsWindow
                {
                    DataContext = analysisParamSetVM,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                var apsw_result = apsw.ShowDialog();
                if (apsw_result != true) return false;

                Storage.DataBaseMapper = analysisParameterSet.BuildAnnotator();
                ProviderFactory = analysisParameterSet.Parameter.ProviderFactoryParameter.Create(5, true);

                if (parameter.TogetherWithAlignment) {
                    var filename = analysisParamSetVM.AlignmentResultFileName;
                        AlignmentFiles.Add(
                            new AlignmentFileBean
                            {
                                FileID = AlignmentFiles.Count,
                                FileName = filename,
                                FilePath = System.IO.Path.Combine(Storage.MsdialImmsParameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.arf),
                                EicFilePath = System.IO.Path.Combine(Storage.MsdialImmsParameter.ProjectFolderPath, filename + ".EIC.aef"),
                                SpectraFilePath = System.IO.Path.Combine(Storage.MsdialImmsParameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.dcl)
                            }
                        );
                        Storage.AlignmentFiles = AlignmentFiles.ToList();
                }

                return true;
            }
        }

        private bool ProcessAnnotaion(Window owner, MsdialImmsDataStorage storage) {
            var vm = new ProgressBarMultiContainerVM
            {
                MaxValue = storage.AnalysisFiles.Count,
                CurrentValue = 0,
                ProgressBarVMs = new ObservableCollection<ProgressBarVM>(
                        storage.AnalysisFiles.Select(file => new ProgressBarVM { Label = file.AnalysisFileName })
                    ),
            };
            var pbmcw = new ProgressBarMultiContainerWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbmcw.Loaded += async (s, e) => {
                foreach ((var analysisfile, var pbvm) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs)) {
                    await Task.Run(() => FileProcess.Run(analysisfile, storage, null, null, ProviderFactory, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v));
                    vm.CurrentValue++;
                }
                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        private bool ProcessAlignment(Window owner, MsdialImmsDataStorage storage) {
            var vm = new ProgressBarVM
            {
                IsIndeterminate = true,
                Label = "Process alignment..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Show();

            var factory = new ImmsAlignmentProcessFactory(storage.MsdialImmsParameter, storage.IupacDatabase, storage.DataBaseMapper);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = ProviderFactory; // TODO: I'll remove this later.
            var alignmentFile = storage.AlignmentFiles.Last();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());

            pbw.Close();

            return true;
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialImmsDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<System.IO.FileStream>();
            try {
                streams = files.Select(file => System.IO.File.OpenRead(file.DeconvolutionFilePath)).ToList();
                foreach (var spot in spots) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
                    var decResult = MsdecResultsReader.ReadMSDecResult(
                        streams[repID], pointerss[repID].pointers[peakID],
                        pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    yield return decResult;
                }
            }
            finally {
                streams.ForEach(stream => stream.Close());
            }
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }

            var provider = ProviderFactory.Create(analysisFile);
            AnalysisModel = new ImmsAnalysisModel(
                analysisFile,
                provider,
                Storage.DataBaseMapper,
                Storage.MsdialImmsParameter,
                Storage.DataBaseMapper.Annotators)
            .AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }

            AlignmentModel = new ImmsAlignmentModel(
                alignmentFile,
                Storage.MsdialImmsParameter,
                Storage.DataBaseMapper,
                Storage.DataBaseMapper.Annotators)
            .AddTo(Disposables);
        }

        public void ExportAlignment(Window owner) {
            var container = Storage;
            var metadataAccessor = new ImmsMetadataAccessor(container.DataBaseMapper, container.MsdialImmsParameter);
            var vm = new AlignmentResultExport2VM(AlignmentFile, container.AlignmentFiles, container);
            vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.MsdialImmsParameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.MsdialImmsParameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.MsdialImmsParameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.MsdialImmsParameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.MsdialImmsParameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.MsdialImmsParameter), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.MsdialImmsParameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.MsdialImmsParameter), "MsmsIncluded"),
                });
            var dialog = new AlignmentResultExportWin
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        public void ExportAnalysis(Window owner) {
            var container = Storage;
            var spectraTypes = new List<Export.SpectraType>
            {
                new Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.MsdialImmsParameter, ExportspectraType.deconvoluted)),
                new Export.SpectraType(
                    ExportspectraType.centroid,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.MsdialImmsParameter, ExportspectraType.centroid)),
                new Export.SpectraType(
                    ExportspectraType.profile,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.MsdialImmsParameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Export.SpectraFormat>
            {
                new Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(container.AnalysisFiles, spectraTypes, spectraFormats, ProviderFactory)) {
                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }
    }
}
