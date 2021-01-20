﻿using CompMs.App.Msdial.LC;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public class LcmsMethodVM : MethodVM {
        public AnalysisLcmsVM AnalysisVM {
            get => analysisVM;
            set => SetProperty(ref analysisVM, value);
        }
        private AnalysisLcmsVM analysisVM;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set {
                if (SetProperty(ref analysisFiles, value)) {
                    _analysisFiles = CollectionViewSource.GetDefaultView(analysisFiles);
                }
            }
        }
        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ICollectionView _analysisFiles;

        public AlignmentLcmsVM AlignmentVM {
            get => alignmentVM;
            set => SetProperty(ref alignmentVM, value);
        }
        private AlignmentLcmsVM alignmentVM;

        public ObservableCollection<AlignmentFileBean> AlignmentFiles {
            get => alignmentFiles;
            set {
                if (SetProperty(ref alignmentFiles, value)) {
                    _alignmentFiles = CollectionViewSource.GetDefaultView(alignmentFiles);
                }
            }
        }
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private ICollectionView _alignmentFiles;

        public MsdialDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialDataStorage storage;

        public bool RefMatchedChecked {
            get => refMatchedChecked;
            set => SetProperty(ref refMatchedChecked, value);
        }
        public bool SuggestedChecked {
            get => suggestedChecked;
            set => SetProperty(ref suggestedChecked, value);
        }
        public bool UnknownChecked {
            get => unknownChecked;
            set => SetProperty(ref unknownChecked, value);
        }
        private bool refMatchedChecked, suggestedChecked, unknownChecked;

        public bool Ms2AcquiredChecked {
            get => ms2AcquiredChecked;
            set => SetProperty(ref ms2AcquiredChecked, value);
        }
        public bool MolecularIonChecked {
            get => molecularIonChecked;
            set => SetProperty(ref molecularIonChecked, value);
        }
        public bool BlankFilterChecked {
            get => blankFilterChecked;
            set => SetProperty(ref blankFilterChecked, value);
        }
        public bool UniqueIonsChecked {
            get => uniqueIonsChecked;
            set => SetProperty(ref uniqueIonsChecked, value);
        }
        private bool ms2AcquiredChecked, molecularIonChecked, blankFilterChecked, uniqueIonsChecked;

        private static readonly MsdialSerializer serializer;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        static LcmsMethodVM() {
            serializer = new MsdialLcMsApi.Parser.MsdialLcmsSerializer();
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", CompMs.Common.Components.ChromXType.RT);
        }

        public LcmsMethodVM(MsdialDataStorage storage, List<AnalysisFileBean> analysisFiles, List<AlignmentFileBean> alignmentFiles) : base(serializer) {
            Storage = storage;
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles ?? Enumerable.Empty<AlignmentFileBean>());
        }

        public override void InitializeNewProject(Window window) {
            // Set analysis param
            var success = ProcessSetAnalysisParameter(window);
            if (!success) return;

            // Run Identification
            ProcessAnnotaion(window, Storage);

            // Run Alignment
            ProcessAlignment(window, Storage);

            AnalysisVM = LoadAnalysisFile(Storage.AnalysisFiles.FirstOrDefault());
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var analysisParamSetVM = new AnalysisParamSetForLcVM((MsdialLcmsParameter)Storage.ParameterBase, Storage.AnalysisFiles);
            var apsw = new AnalysisParamSetForLcWindow
            {
                DataContext = analysisParamSetVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var apsw_result = apsw.ShowDialog();
            if (apsw_result != true) return false;

            if (AlignmentFiles == null)
                AlignmentFiles = new ObservableCollection<AlignmentFileBean>();
            var filename = analysisParamSetVM.AlignmentResultFileName;
            AlignmentFiles.Add(
                new AlignmentFileBean
                {
                    FileID = AlignmentFiles.Count,
                    FileName = filename,
                    FilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.arf),
                    EicFilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + ".EIC.aef"),
                }
            );
            Storage.AlignmentFiles = AlignmentFiles.ToList();
            Storage.MspDB = analysisParamSetVM.MspDB;
            Storage.TextDB = analysisParamSetVM.TextDB;
            return true;
        }

        private bool ProcessAnnotaion(Window owner, MsdialDataStorage storage) {
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
                var numThreads = Math.Min(Math.Min(storage.ParameterBase.NumThreads, storage.AnalysisFiles.Count), Environment.ProcessorCount);
                var semaphore = new SemaphoreSlim(0, numThreads);
                var tasks = new Task[storage.AnalysisFiles.Count];
                var counter = 0;
                foreach (((var analysisfile, var pbvm), var idx) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs).WithIndex()) {
                    tasks[idx] = Task.Run(async () => {
                        await semaphore.WaitAsync();
                        MsdialLcMsApi.Process.FileProcess.Run(analysisfile, storage, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v);
                        Interlocked.Increment(ref counter);
                        vm.CurrentValue = counter;
                        semaphore.Release();
                    });
                }
                semaphore.Release(numThreads);

                await Task.WhenAll(tasks);

                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        private bool ProcessAlignment(Window owner, MsdialDataStorage storage) {
            AlignmentProcessFactory factory = new LcmsAlignmentProcessFactory(storage.ParameterBase as MsdialLcmsParameter, storage.IupacDatabase);
            var alignmentFile = storage.AlignmentFiles.Last();
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            return true;
        }

        public override void LoadProject() {
            LoadSelectedAnalysisFile();
        }

        public DelegateCommand LoadAnalysisFileCommand {
            get => loadAnalysisFileCommand ?? (loadAnalysisFileCommand = new DelegateCommand(LoadSelectedAnalysisFile));
        }
        private DelegateCommand loadAnalysisFileCommand;

        private void LoadSelectedAnalysisFile() {
            if (_analysisFiles.CurrentItem is AnalysisFileBean analysis)
                AnalysisVM = LoadAnalysisFile(analysis);
        }

        public DelegateCommand LoadAlignmentFileCommand {
            get => loadAlignmentFileCommand ?? (loadAlignmentFileCommand = new DelegateCommand(LoadSelectedAlignmentFile));
        }
        private DelegateCommand loadAlignmentFileCommand;
        private void LoadSelectedAlignmentFile() {
            if (_alignmentFiles.CurrentItem is AlignmentFileBean alignment)
                AlignmentVM = LoadAlignmentFile(alignment);
        }

        private AnalysisLcmsVM LoadAnalysisFile(AnalysisFileBean analysis) {
            return new AnalysisLcmsVM(analysis, Storage.ParameterBase, Storage.MspDB);
        }

        private AlignmentLcmsVM LoadAlignmentFile(AlignmentFileBean alignment) {
            return new AlignmentLcmsVM(alignment, Storage.ParameterBase);
        }

        public override void SaveProject() {
            throw new NotImplementedException();
        }
    }
}