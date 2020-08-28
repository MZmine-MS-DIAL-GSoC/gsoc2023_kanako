﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Windows;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;

using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Common;
using System.Windows.Input;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.Common.DataObj.Database;

namespace MsdialDimsCoreUiTestApp
{
    internal class MainWindowVM : INotifyPropertyChanged
    {
        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }

        public ObservableCollection<AlignmentFileBean> AlignmentFiles {
            get => alignmentFiles;
            set => SetProperty(ref alignmentFiles, value);
        }

        public Rect Ms1Area {
            get => ms1Area;
            set => SetProperty(ref ms1Area, value);
        }

        public Rect Ms2Area {
            get => ms2Area;
            set => SetProperty(ref ms2Area, value);
        }

        public ObservableCollection<ChromatogramPeak> Ms1Peaks {
            get => ms1Peaks;
            set => SetProperty(ref ms1Peaks, value);
        }

        public ObservableCollection<Ms2Info> Ms2Features {
            get => ms2Features;
            set => SetProperty(ref ms2Features, value);
        }

        public AlignmentResultContainer AlignmentContainer {
            get => alignmentContainer;
            set => SetProperty(ref alignmentContainer, value);
        }

        public ICommand SelectionChangedCmd { get; }

        private ChromatogramSerializer<ChromatogramSpotInfo> chromSpotSerializer;
        private ChromatogramSerializer<ChromatogramPeakInfo> chromPeakSerializer;
        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private ObservableCollection<ChromatogramPeak> ms1Peaks;
        private ObservableCollection<Ms2Info> ms2Features;
        private AlignmentResultContainer alignmentContainer;
        private Rect ms1Area, ms2Area;
        private List<MoleculeMsReference> mspDB, textDB;
        private MsdialDimsParameter param;
        private IupacDatabase iupac;
        private Dictionary<int, Stream> rawChromatogramStreams;

        public MainWindowVM()
        {
            // testfiles
            var lbmFile = @"C:\Users\YUKI MATSUZAWA\works\data\lbm\MSDIAL_LipidDB_Test.lbm2";
            // var lbmFile = @"C:\Users\YUKI MATSUZAWA\works\data\lbm\LipidMsmsBinaryDB-VS68-AritaM.lbm2";
            var textLibraryFile = @"C:\Users\YUKI MATSUZAWA\works\data\textlib\TestLibrary.txt";
            analysisFiles = new ObservableCollection<AnalysisFileBean> {
                new AnalysisFileBean { AnalysisFileId = 0,
                                       // AnalysisFileName = "703_Egg2 Egg White",
                                       // AnalysisFilePath = @"C:\Users\YUKI MATSUZAWA\works\data\sciex_msmsall\703_Egg2 Egg White.abf",
                                       // AnalysisFileName = "Neg_Infusion_IDA_Liver1",
                                       // AnalysisFilePath = @"D:\infusion_project\data\abf\infusion_IDA_Negative\20200717_Neg_Infusion_IDA_Liver1.abf",
                                       AnalysisFileName = "Neg_MSMSALL_Liver1",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Liver1.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 1,
                                       // AnalysisFileName = "704_Egg2 Egg Yolk",
                                       // AnalysisFilePath = @"C:\Users\YUKI MATSUZAWA\works\data\sciex_msmsall\704_Egg2 Egg Yolk.abf",
                                       // AnalysisFileName = "Neg_Infusion_IDA_Liver2",
                                       // AnalysisFilePath = @"D:\infusion_project\data\abf\infusion_IDA_Negative\20200717_Neg_Infusion_IDA_Liver2.abf",
                                       AnalysisFileName = "Neg_MSMSALL_Liver2",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Liver2.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 2,
                                       // AnalysisFileName = "Neg_Infusion_IDA_Liver3",
                                       // AnalysisFilePath = @"D:\infusion_project\data\abf\infusion_IDA_Negative\20200717_Neg_Infusion_IDA_Liver3.abf",
                                       AnalysisFileName = "Neg_MSMSALL_Liver3",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Liver3.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                /*
                new AnalysisFileBean { AnalysisFileId = 3,
                                       AnalysisFileName = "Neg_MSMSALL_Muscle1",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Muscle1.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 4,
                                       AnalysisFileName = "Neg_MSMSALL_Muscle2",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Muscle2.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 5,
                                       AnalysisFileName = "Neg_MSMSALL_Muscle3",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Muscle3.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 6,
                                       AnalysisFileName = "Neg_MSMSALL_Plasma1",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Plasma1.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 7,
                                       AnalysisFileName = "Neg_MSMSALL_Plasma2",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Plasma2.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 8,
                                       AnalysisFileName = "Neg_MSMSALL_Plasma3",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_Plasma3.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 9,
                                       AnalysisFileName = "Neg_MSMSALL_WAT1",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_WAT1.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 10,
                                       AnalysisFileName = "Neg_MSMSALL_WAT2",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_WAT2.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 11,
                                       AnalysisFileName = "Neg_MSMSALL_WAT3",
                                       AnalysisFilePath = @"D:\infusion_project\data\abf\MSMSALL_Negative\20200717_Neg_MSMSALL_WAT3.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                */
            };

            alignmentFiles = new ObservableCollection<AlignmentFileBean> {
                new AlignmentFileBean {
                    FileID = 0,
                    FileName = "Alignment 1",
                    FilePath = System.IO.Path.GetTempFileName(),
                    EicFilePath = System.IO.Path.GetTempFileName(),
                },
            };

            param = new MsdialDimsParameter() {
                IonMode = CompMs.Common.Enum.IonMode.Negative,
                MspFilePath = lbmFile,
                TextDBFilePath = textLibraryFile,
                TargetOmics = CompMs.Common.Enum.TargetOmics.Lipidomics,
                LipidQueryContainer = new CompMs.Common.Query.LipidQueryBean() {
                    SolventType = CompMs.Common.Enum.SolventType.HCOONH4,
                    LbmQueries = LbmQueryParcer.GetLbmQueries(true),
                },
                MspSearchParam = new CompMs.Common.Parameter.MsRefSearchParameterBase() {
                    WeightedDotProductCutOff = 0.1F, SimpleDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.4F, MatchedPeaksPercentageCutOff = 0.8F,
                    MinimumSpectrumMatch = 1
                }
            };

            iupac = IupacResourceParser.GetIUPACDatabase();

            mspDB = LibraryHandler.ReadLipidMsLibrary(param.MspFilePath, param);
            mspDB.Sort((a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));


            // textDB = TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out _);
            // textDB.Sort((a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));

            chromSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            chromPeakSerializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPS1", ChromXType.Mz);

            rawChromatogramStreams = new Dictionary<int, Stream>();

            foreach (var analysisFile in analysisFiles) {
                var stream = new MemoryStream();
                RunAnnotation(analysisFile, mspDB, textDB, stream);
                stream.Seek(0, SeekOrigin.Begin);
                rawChromatogramStreams[analysisFile.AnalysisFileId] = stream;
            }
            RunAlignment(analysisFiles, alignmentFiles[0], iupac);

            SelectionChangedCmd = new SelectionChangedCommand(this);

            ReadAndSetMs1RawSpectrum(analysisFiles[0].AnalysisFileId);
            ReadAndSetMs1Peaks(analysisFiles[0].PeakAreaBeanInformationFilePath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value == null && property == null || value.Equals(property)) return false;
            property = value;
            RaisePropertyChanged(propertyname);
            return true;
        }

        private void RunAnnotation(AnalysisFileBean analysisFileBean, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, Stream stream = null) {
            var spectras = DataAccess.GetAllSpectra(analysisFileBean.AnalysisFilePath);
            var ms1spectra = spectras.Where(spectra => spectra.MsLevel == 1)
                                     .Where(spectra => spectra.Spectrum != null)
                                     .Argmax(spectra => spectra.Spectrum.Length);

            var chromPeaks = ComponentsConverter.ConvertRawPeakElementToChromatogramPeakList(ms1spectra.Spectrum);
            var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
            if (stream != null)
                chromPeakSerializer?.Serialize(stream, new ChromatogramPeakInfo(analysisFileBean.AnalysisFileId, sChromPeaks, -1, -1, -1));

            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            var chromatogramPeakFeatures = GetChromatogramPeakFeatures(peakPickResults, ms1spectra, spectras);
            SetSpectrumPeaks(chromatogramPeakFeatures, spectras);

            foreach (var feature in chromatogramPeakFeatures) {
                var isotopes = DataAccess.GetIsotopicPeaks(spectras, feature.MS1RawSpectrumIdTop, (float)feature.Mass, param.CentroidMs1Tolerance);
                _ = CalculateAndSetAnnotatedReferences(feature, mspDB, textDB, param, isotopes);
            }

            MsdialSerializer.SaveChromatogramPeakFeatures(analysisFileBean.PeakAreaBeanInformationFilePath, chromatogramPeakFeatures);
        }

        private void RunAlignment(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile, IupacDatabase iupac) {
            var aligner = new PeakAligner(new DimsPeakComparer(param.Ms1AlignmentTolerance), param, iupac);
            var result = aligner.Alignment(analysisFiles, alignmentFile, chromSpotSerializer);
            CompMs.Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, List<RawSpectrum> allSpectra) {
            var peakFeatures = new List<ChromatogramPeakFeature>();
            var ms2SpecObjects = allSpectra.Where(n => n.MsLevel == 2 && n.Precursor != null).OrderBy(n => n.Precursor.SelectedIonMz).ToList();

            foreach (var result in peakPickResults) {

                // here, the chrom scan ID should be matched to the scan number of RawSpectrum Element
                var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz, ms1Spectrum.Spectrum[result.ScanNumAtPeakTop].Mz);
                var chromScanID = peakFeature.ChromScanIdTop;
                peakFeature.ChromXs.RT = new RetentionTime(0);
                peakFeature.ChromXsTop.RT = new RetentionTime(0);
                peakFeature.IonMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? CompMs.Common.Enum.IonMode.Positive : CompMs.Common.Enum.IonMode.Negative;
                peakFeature.PrecursorMz = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.ScanNumber;
                peakFeature.ScanID = ms1Spectrum.ScanNumber;
                peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDs(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumID2CE, allSpectra);
                peakFeatures.Add(peakFeature);

                // result check
                Console.WriteLine("Peak ID={0}, Scan ID={1}, MZ={2}, MS2SpecID={3}, Height={4}, Area={5}",
                    peakFeature.PeakID, peakFeature.ChromScanIdTop, peakFeature.ChromXsTop.Mz.Value, peakFeature.MS2RawSpectrumID, peakFeature.PeakHeightTop, peakFeature.PeakAreaAboveZero);
            }

            return peakFeatures;
        }

        private int GetRepresentativeMS2RawSpectrumID(Dictionary<int, double> ms2RawSpectrumID2CE, List<RawSpectrum> allSpectra) {
            if (ms2RawSpectrumID2CE.Count == 0) return -1;

            var maxIntensity = 0.0;
            var maxIntensityID = -1;
            foreach (var pair in ms2RawSpectrumID2CE) {
                var specID = pair.Key;
                var specObj = allSpectra[specID];
                if (specObj.TotalIonCurrent > maxIntensity) {
                    maxIntensity = specObj.TotalIonCurrent;
                    maxIntensityID = specID;
                }
            }
            return maxIntensityID;
        }

        private Dictionary<int, double> GetMS2RawSpectrumIDs(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var ID2CE = new Dictionary<int, double>();
            var target = new RawSpectrum { Precursor = new RawPrecursorIon { SelectedIonMz = precursorMz - mzTolerance } };
            var startID =  SearchCollection.LowerBound(ms2SpecObjects, target, (a, b) => a.Precursor.SelectedIonMz.CompareTo(b.Precursor.SelectedIonMz));
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                var precursorMzObj = spec.Precursor.SelectedIonMz;
                if (precursorMzObj < precursorMz - mzTolerance) continue;
                if (precursorMzObj > precursorMz + mzTolerance) break;

                ID2CE[spec.ScanNumber] = spec.CollisionEnergy;
            }
            return ID2CE; // maybe, in msmsall, the id count is always one but for just in case
        }

        private void SetSpectrumPeaks(List<ChromatogramPeakFeature> chromFeatures, List<RawSpectrum> spectra) {
            foreach (var feature in chromFeatures) {
                if (feature.MS2RawSpectrumID >= 0 && feature.MS2RawSpectrumID <= spectra.Count - 1) {
                    var peakElements = spectra[feature.MS2RawSpectrumID].Spectrum;
                    var spectrumPeaks = ComponentsConverter.ConvertToSpectrumPeaks(peakElements);
                    var centroidSpec = SpectralCentroiding.Centroid(spectrumPeaks);
                    feature.Spectrum = centroidSpec;
                }
                Console.WriteLine("Peak ID={0}, Scan ID={1}, Spectrum count={2}", feature.PeakID, feature.ScanID, feature.Spectrum.Count);
            }
        }

        private List<SpectrumPeak> ScalingSpectrumPeaks(IEnumerable<SpectrumPeak> spectrumPeaks)
        {
            if (!spectrumPeaks.Any()) return new List<SpectrumPeak>();
            var min = spectrumPeaks.Min(peak => peak.Intensity);
            var width = spectrumPeaks.Max(peak => peak.Intensity) - min;

            return spectrumPeaks.Select(peak => new SpectrumPeak(peak.Mass, (peak.Intensity - min) / width)).ToList();
        }

        private (List<MsScanMatchResult> Msp, List<MsScanMatchResult> Text) CalculateAndSetAnnotatedReferences(
            ChromatogramPeakFeature chromatogramPeakFeature, 
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            MsdialDimsParameter param, List<IsotopicPeak> isotopes)
        {
            AnnotationProcess.Run(chromatogramPeakFeature, mspDB, textDB, param.MspSearchParam, param.TargetOmics, isotopes, out List<MsScanMatchResult> mspResult, out List<MsScanMatchResult> textResult);
            Console.WriteLine("PeakID={0}, Annotation={1}", chromatogramPeakFeature.PeakID, chromatogramPeakFeature.Name);
            return (mspResult, textResult);
        }

        void ReadAndSetMs1Peaks(string serializedPeakPath) {
            var chromatogramPeakFeatures = MsdialSerializer.LoadChromatogramPeakFeatures(serializedPeakPath);

            Ms2Features = new ObservableCollection<Ms2Info>(
                chromatogramPeakFeatures.Select(feature =>
                    new Ms2Info
                    {
                        ChromatogramPeakFeature = feature,
                        PeakID = feature.PeakID,
                        Mass = feature.Mass,
                        Intensity = feature.PeakHeightTop,
                        Centroids = ScalingSpectrumPeaks(feature.Spectrum),
                        RefMatched = feature.Name != string.Empty && !feature.Name.Contains("w/o"),
                        Suggested = feature.Name.Contains("w/o"),
                        Ms2Acquired = feature.Spectrum.Count != 0,
                    }
                ));
            Ms2Area = new Rect(param.Ms2MassRangeBegin, 0, param.Ms2MassRangeEnd, 1);
        }

        void ReadAndSetMs1RawSpectrum(int id) {
            var stream = rawChromatogramStreams[id];
            var peakInfo = chromPeakSerializer.Deserialize(stream);
            var peaks = peakInfo.Chromatogram.Cast<ChromatogramPeak>();
            foreach (var peak in peaks)
                peak.Mass = peak.ChromXs.Mz.Value;
            Ms1Peaks = new ObservableCollection<ChromatogramPeak>(peaks);

            Ms1Area = new Rect(new Point(Ms1Peaks.Min(peak => peak.Mass), Ms1Peaks.Min(peak => peak.Intensity)),
                               new Point(Ms1Peaks.Max(peak => peak.Mass), Ms1Peaks.Max(peak => peak.Intensity)));

            stream.Seek(0, SeekOrigin.Begin);
        }

        void ReadAndSetAlignmentResultContainer(int id) {
            var alignmentFile = alignmentFiles.FirstOrDefault(alignment => alignment.FileID == id);
            if (alignmentFile == null) return;

            AlignmentContainer = CompMs.Common.MessagePack.MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
        }

        public class SelectionChangedCommand : ICommand
        {
            MainWindowVM mainWindowVM;

            public SelectionChangedCommand(MainWindowVM mainWindowVM) {
                this.mainWindowVM = mainWindowVM;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) {
                return true;
            }

            public void Execute(object parameter) {
                var analysisFile = parameter as AnalysisFileBean;
                mainWindowVM.ReadAndSetMs1RawSpectrum(analysisFile.AnalysisFileId);
                mainWindowVM.ReadAndSetMs1Peaks(analysisFile.PeakAreaBeanInformationFilePath);
            }
        }

    }

    internal class Ms2Info
    {
        public ChromatogramPeakFeature ChromatogramPeakFeature { get; set; }
        public int PeakID { get; set; }
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public List<SpectrumPeak> Centroids { get; set; }
        public bool RefMatched { get; set; }
        public bool Suggested { get; set; }
        public bool Ms2Acquired { get; set; }
    }
}
