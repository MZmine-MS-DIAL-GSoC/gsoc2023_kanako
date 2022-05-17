﻿using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialCore.Utility {
    public sealed class DataAccess {
        private DataAccess() { }

        // raw data support
        public static bool IsDataFormatSupported(string filepath) {
            if (System.IO.File.Exists(filepath)) {
                var extension = System.IO.Path.GetExtension(filepath).ToLower().Substring(1); // .abf -> abf
                foreach (var item in System.Enum.GetNames(typeof(SupportMsRawDataExtension))) {
                    if (item == extension) return true;
                }
            }
            return false;
        }

        // raw data access
        public static List<RawSpectrum> GetAllSpectra(string filepath) {
            List<RawSpectrum> rawSpectra = null;
            using (var rawDataAccess = new RawDataAccess(filepath, 0, false, false)) {
                var measurment = rawDataAccess.GetMeasurement();
                rawSpectra = measurment.SpectrumList;
            }
            return rawSpectra;
        }

        public static void GetAllSpectraWithAccumulatedMS1(string filepath, out List<RawSpectrum> allSpectrumList, out List<RawSpectrum> accumulatedSpectrumList) {
            allSpectrumList = new List<RawSpectrum>();
            accumulatedSpectrumList = new List<RawSpectrum>();
            using (var rawDataAccess = new RawDataAccess(filepath, 0, false, false)) {
                var measurment = rawDataAccess.GetMeasurement();
                allSpectrumList = measurment.SpectrumList;
                accumulatedSpectrumList = measurment.AccumulatedSpectrumList;
            }
        }

        public static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isGuiProcess, int retry, int sleepMilliSeconds) {
            using (var access = new RawDataAccess(file.AnalysisFilePath, 0, false, isGuiProcess)) {
                for (var i = 0; i < retry; i++) {
                    var rawObj = access.GetMeasurement();
                    if (rawObj != null)
                        return rawObj;
                    Thread.Sleep(sleepMilliSeconds);
                }
                throw new FileLoadException($"Loading {file.AnalysisFilePath} failed.");
            }
        }

        public static RawCalibrationInfo ReadIonMobilityCalibrationInfo(string filepath) {
            using (var rawDataAccess = new RawDataAccess(filepath, 0, false, false)) {
                return rawDataAccess.ReadIonmobilityCalibrationInfo();
            }
        }

        // smoother
        public static List<ChromatogramPeak> GetSmoothedPeaklist(IReadOnlyList<IChromatogramPeak> peaklist, SmoothingMethod smoothingMethod, int smoothingLevel) {
            switch (smoothingMethod) {
                case SmoothingMethod.SimpleMovingAverage:
                    return Smoothing.SimpleMovingAverage(peaklist, smoothingLevel);
                case SmoothingMethod.LinearWeightedMovingAverage:
                    return Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return Smoothing.SavitxkyGolayFilter(peaklist, smoothingLevel);
                case SmoothingMethod.BinomialFilter:
                    return Smoothing.BinomialFilter(peaklist, smoothingLevel);
                case SmoothingMethod.LowessFilter:
                    return Smoothing.LowessFilter(peaklist, smoothingLevel);
                case SmoothingMethod.LoessFilter:
                    return Smoothing.LoessFilter(peaklist, smoothingLevel);
                default:
                    return Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
            }
        }

        // converter
        public static ChromatogramPeakFeature GetChromatogramPeakFeature(PeakDetectionResult result, ChromXType type, ChromXUnit unit, double mass) {
            if (result == null) return null;

            var peakFeature = new ChromatogramPeakFeature() {

                MasterPeakID = result.PeakID,
                PeakID = result.PeakID,

                ChromScanIdLeft = result.ScanNumAtLeftPeakEdge,
                ChromScanIdTop = result.ScanNumAtPeakTop,
                ChromScanIdRight = result.ScanNumAtRightPeakEdge,

                ChromXsLeft = new ChromXs(result.ChromXAxisAtLeftPeakEdge, type, unit),
                ChromXsTop = new ChromXs(result.ChromXAxisAtPeakTop, type, unit),
                ChromXsRight = new ChromXs(result.ChromXAxisAtRightPeakEdge, type, unit),

                ChromXs = new ChromXs(result.ChromXAxisAtPeakTop, type, unit),

                PeakHeightLeft = result.IntensityAtLeftPeakEdge,
                PeakHeightTop = result.IntensityAtPeakTop,
                PeakHeightRight = result.IntensityAtRightPeakEdge,

                PeakAreaAboveZero = result.AreaAboveZero,
                PeakAreaAboveBaseline = result.AreaAboveBaseline,

                Mass = mass,

                PeakShape = new ChromatogramPeakShape() {
                    SignalToNoise = result.SignalToNoise,
                    EstimatedNoise = result.EstimatedNoise,
                    BasePeakValue = result.BasePeakValue,
                    GaussianSimilarityValue = result.GaussianSimilarityValue,
                    IdealSlopeValue = result.IdealSlopeValue,
                    PeakPureValue = result.PeakPureValue,
                    ShapenessValue = result.ShapnessValue,
                    SymmetryValue = result.SymmetryValue,
                    AmplitudeOrderValue = result.AmplitudeOrderValue,
                    AmplitudeScoreValue = result.AmplitudeScoreValue
                }
            };

            if (type != ChromXType.Mz) {
                peakFeature.ChromXs.Mz = new MzValue(mass);
                peakFeature.ChromXsLeft.Mz = new MzValue(mass);
                peakFeature.ChromXsTop.Mz = new MzValue(mass);
                peakFeature.ChromXsRight.Mz = new MzValue(mass);
            }

            return peakFeature;
        }

        public static List<IsotopicPeak> GetIsotopicPeaks(IReadOnlyList<RawSpectrum> rawSpectrumList, int scanID, float targetedMz, float massTolerance, int maxIsotopes = 2) {
            if (scanID < 0 || rawSpectrumList == null || scanID > rawSpectrumList.Count - 1) return null;
            var spectrum = rawSpectrumList[scanID].Spectrum;
            return GetIsotopicPeaks(spectrum, targetedMz, massTolerance, maxIsotopes);
        }

        public static List<IsotopicPeak> GetIsotopicPeaks(IReadOnlyList<RawPeakElement> spectrum, float targetedMz, float massTolerance, int maxIsotopes = 2) {
            var startID = SearchCollection.LowerBound(spectrum, new RawPeakElement() { Mz = targetedMz - massTolerance }, (a, b) => a.Mz.CompareTo(b.Mz));
            //var startID = GetMs1StartIndex(targetedMz, massTolerance, spectrum);
            var massDiffBase = MassDiffDictionary.CHNO_AverageStepSize;
            var maxIsotopeRange = (double)maxIsotopes;
            var isotopes = new List<IsotopicPeak>();
            for (int i = 0; i < maxIsotopes; i++) {
                isotopes.Add(new IsotopicPeak() {
                    Mass = targetedMz + (double)i * massDiffBase,
                    MassDifferenceFromMonoisotopicIon = (double)i * massDiffBase
                });
            }

            for (int i = startID; i < spectrum.Count; i++) {
                var peak = spectrum[i];
                if (peak.Mz < targetedMz - massTolerance) continue;
                if (peak.Mz > targetedMz + massDiffBase * maxIsotopeRange + massTolerance) break;

                foreach (var isotope in isotopes) {
                    if (Math.Abs(isotope.Mass - peak.Mz) < massTolerance) {
                        isotope.AbsoluteAbundance += peak.Intensity;
                    }
                }
            }

            if (isotopes[0].AbsoluteAbundance <= 0) return null;
            var baseIntensity = isotopes[0].AbsoluteAbundance;

            foreach (var isotope in isotopes)
                isotope.RelativeAbundance = isotope.AbsoluteAbundance / baseIntensity * 100;

            return isotopes;
        }


        // index access
        public static int GetScanStartIndexByRt(float focusedRt, float rtTol, IReadOnlyList<RawSpectrum> spectrumList) {

            var targetRt = focusedRt - rtTol;
            int startIndex = 0, endIndex = spectrumList.Count - 1;

            int counter = 0;
            int limit = spectrumList.Count > 50000 ? 20 : 10;
            while (counter < limit) {
                if (spectrumList[startIndex].ScanStartTime <= targetRt && targetRt < spectrumList[(startIndex + endIndex) / 2].ScanStartTime) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (spectrumList[(startIndex + endIndex) / 2].ScanStartTime <= targetRt && targetRt < spectrumList[endIndex].ScanStartTime) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetTargetCEIndexForMS2RawSpectrum(ChromatogramPeakFeature chromPeakFeature, double targetCE) {
            var targetSpecID = chromPeakFeature.MS2RawSpectrumID;
            if (targetCE >= 0) {
                var targetCEs = chromPeakFeature.MS2RawSpectrumID2CE;
                var isTargetCEFound = false;
                foreach (var pair in targetCEs) {
                    if (Math.Abs(pair.Value - targetCE) < 0.01) {
                        targetSpecID = pair.Key;
                        isTargetCEFound = true;
                    }
                }
                if (!isTargetCEFound) Console.WriteLine("Target CE cannot be found.");
            }
            return targetSpecID;
        }

        // get chromatograms
        public static List<ChromatogramPeak> GetMs1Peaklist(IReadOnlyList<RawSpectrum> spectrumList, double targetMass, double ms1Tolerance, IonMode ionmode,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min, double chromBegin = double.MinValue, double chromEnd = double.MaxValue) {
            if (spectrumList == null || spectrumList.Count == 0) return null;
            var peaklist = new List<ChromatogramPeak>();
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            foreach (var (spectrum, index) in spectrumList.WithIndex().Where(n => n.Item1.ScanPolarity == scanPolarity && n.Item1.MsLevel <= 1)) {
                var chromX = type == ChromXType.Drift ? spectrum.DriftTime : spectrum.ScanStartTime;
                if (chromX < chromBegin) continue;
                if (chromX > chromEnd) break;
                var massSpectra = spectrum.Spectrum;
                //var startIndex = GetMs1StartIndex(targetMass, ms1Tolerance, massSpectra);
                //bin intensities for focused MZ +- ms1Tolerance
                RetrieveBinnedMzIntensity(massSpectra, targetMass, ms1Tolerance, out double basepeakMz, out double basepeakIntensity, out double summedIntensity);
                peaklist.Add(new ChromatogramPeak() { ID = index, ChromXs = new ChromXs(chromX, type, unit), Mass = basepeakMz, Intensity = summedIntensity });
            }

            return peaklist;
        }

        public static Dictionary<int, ChromXs> GetID2ChromXs(IReadOnlyList<RawSpectrum> spectrumList, IonMode ionmode,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min) {
            var dict = new Dictionary<int, ChromXs>();
            if (spectrumList == null || spectrumList.Count == 0) return null;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            foreach (var (spectrum, index) in spectrumList.WithIndex().Where(n => n.Item1.ScanPolarity == scanPolarity && n.Item1.MsLevel <= 1)) {
                var chromX = type == ChromXType.Drift ? spectrum.DriftTime : spectrum.ScanStartTime;
                dict[index] = new ChromXs(chromX, type, unit);
            }
            return dict;
        }

        public static List<ChromatogramPeak> GetMs1Peaklist(IReadOnlyList<RawSpectrum> spectrumList, Dictionary<int, ChromXs> id2ChromXs,
            double targetMass, double ms1Tolerance, IonMode ionmode,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min, double chromBegin = double.MinValue, double chromEnd = double.MaxValue) {
            if (spectrumList == null || spectrumList.Count == 0) return null;
            var peaklist = new List<ChromatogramPeak>();
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            foreach (var (spectrum, index) in spectrumList.WithIndex().Where(n => n.Item1.ScanPolarity == scanPolarity && n.Item1.MsLevel <= 1)) {
                var chromX = type == ChromXType.Drift ? spectrum.DriftTime : spectrum.ScanStartTime;
                if (chromX < chromBegin) continue;
                if (chromX > chromEnd) break;
                var massSpectra = spectrum.Spectrum;
                //var startIndex = GetMs1StartIndex(targetMass, ms1Tolerance, massSpectra);
                //bin intensities for focused MZ +- ms1Tolerance
                RetrieveBinnedMzIntensity(massSpectra, targetMass, ms1Tolerance, out double basepeakMz, out double basepeakIntensity, out double summedIntensity);
                peaklist.Add(new ChromatogramPeak() { ID = index, ChromXs = id2ChromXs[index], Mass = basepeakMz, Intensity = summedIntensity });
            }

            return peaklist;
        }

        public static void RetrieveBinnedMzIntensity(RawPeakElement[] peaks, double targetMz, double mzTol, out double basepeakMz, out double basepeakIntensity, out double summedIntensity) {
            var startIndex = SearchCollection.LowerBound(peaks, new RawPeakElement() { Mz = targetMz - mzTol }, (a, b) => a.Mz.CompareTo(b.Mz));
            summedIntensity = 0; basepeakIntensity = 0; basepeakMz = 0;
            for (int i = startIndex; i < peaks.Length; i++) {
                var peak = peaks[i];
                if (peak.Mz < targetMz - mzTol) continue;
                else if (Math.Abs(peak.Mz - targetMz) < mzTol) {
                    summedIntensity += peak.Intensity;
                    if (basepeakIntensity < peak.Intensity) {
                        basepeakIntensity = peak.Intensity;
                        basepeakMz = peak.Mz;
                    }
                }
                else if (peak.Mz > targetMz + mzTol) {
                    break;
                }
            }
        }

        public static List<ChromatogramPeak> GetEicPeaklistByHighestBasePeakMz(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> features, double mzTol, IonMode ionmode,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min, double chromBegin = double.MinValue, double chromEnd = double.MaxValue) {
            if (spectrumList.IsEmptyOrNull()) return null;
            if (features.IsEmptyOrNull()) return null;

            var maxSpotID = 0;
            var maxIntensity = double.MinValue;
            for (int i = 0; i < features.Count; i++) {
                if (features[i].PeakHeightTop > maxIntensity) {
                    maxIntensity = features[i].PeakHeightTop;
                    maxSpotID = i;
                }
            }
            var hSpot = features[maxSpotID];
            return GetMs1Peaklist(spectrumList, hSpot.PrecursorMz, mzTol, ionmode, type, unit, chromBegin, chromEnd);
        }

        public static List<ChromatogramPeak> GetTicPeaklist(IReadOnlyList<RawSpectrum> spectrumList, IonMode ionmode,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min, double chromBegin = double.MinValue, double chromEnd = double.MaxValue) {
            if (spectrumList == null || spectrumList.Count == 0) return null;
            var peaklist = new List<ChromatogramPeak>();
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            foreach (var (spectrum, index) in spectrumList.WithIndex().Where(n => n.Item1.ScanPolarity == scanPolarity && n.Item1.MsLevel <= 1)) {
                var chromX = type == ChromXType.Drift ? spectrum.DriftTime : spectrum.ScanStartTime;
                if (chromX < chromBegin) continue;
                if (chromX > chromEnd) break;
                var massSpectra = spectrum.Spectrum;
                RetrieveTotalIntensity(massSpectra, out double basepeakMz, out double basepeakIntensity, out double summedIntensity);
                peaklist.Add(new ChromatogramPeak() { ID = index, ChromXs = new ChromXs(chromX, type, unit), Mass = basepeakMz, Intensity = summedIntensity });
            }

            return peaklist;
        }

        public static List<ChromatogramPeak> GetBpcPeaklist(IReadOnlyList<RawSpectrum> spectrumList, IonMode ionmode,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min, double chromBegin = double.MinValue, double chromEnd = double.MaxValue) {
            if (spectrumList == null || spectrumList.Count == 0) return null;
            var peaklist = new List<ChromatogramPeak>();
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            foreach (var (spectrum, index) in spectrumList.WithIndex().Where(n => n.Item1.ScanPolarity == scanPolarity && n.Item1.MsLevel <= 1)) {
                var chromX = type == ChromXType.Drift ? spectrum.DriftTime : spectrum.ScanStartTime;
                if (chromX < chromBegin) continue;
                if (chromX > chromEnd) break;
                var massSpectra = spectrum.Spectrum;
                RetrieveTotalIntensity(massSpectra, out double basepeakMz, out double basepeakIntensity, out double summedIntensity);
                peaklist.Add(new ChromatogramPeak() { ID = index, ChromXs = new ChromXs(chromX, type, unit), Mass = basepeakMz, Intensity = basepeakIntensity });
            }

            return peaklist;
        }

        public static void RetrieveTotalIntensity(RawPeakElement[] peaks, out double basepeakMz, out double basepeakIntensity, out double summedIntensity) {
            summedIntensity = 0; basepeakIntensity = 0; basepeakMz = 0;
            for (int i = 0; i < peaks.Length; i++) {
                var peak = peaks[i];
                summedIntensity += peak.Intensity;
                if (basepeakIntensity < peak.Intensity) {
                    basepeakIntensity = peak.Intensity;
                    basepeakMz = peak.Mz;
                }
            }
        }


        /// <summary>
        /// from the list of m/z and intensity
        /// the list of scan (auto), times (by m/z), m/z, and intensity is created
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public static List<ChromatogramPeak> ConvertRawPeakElementToChromatogramPeakList(RawPeakElement[] spectrum) {
            return ConvertRawPeakElementToChromatogramPeakList(spectrum, double.MinValue, double.MaxValue);
        }

        public static List<ChromatogramPeak> ConvertRawPeakElementToChromatogramPeakList(RawPeakElement[] spectrum, double minMz, double maxMz) {
            var chromPeaks = new List<ChromatogramPeak>();
            for (int i = 0; i < spectrum.Length; i++) {
                var mz = spectrum[i].Mz;
                var intensity = spectrum[i].Intensity;
                if (mz < minMz || mz > maxMz) continue;
                chromPeaks.Add(new ChromatogramPeak(i, mz, intensity, new MzValue(mz)));
            }
            return chromPeaks;
        }


        public static List<List<ChromatogramPeak>> GetMs2Peaklistlist(IReadOnlyList<RawSpectrum> spectrumList, double precursorMz,
            int startScanID, int endScanID, List<double> productMzList, ParameterBase param, double targetCE = -1,
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min) {
            var chromPeakslist = new List<List<ChromatogramPeak>>();

            foreach (var productMz in productMzList) {
                var chromPeaks = GetMs2Peaklist(spectrumList, precursorMz, productMz, startScanID, endScanID, param, targetCE, type, unit);
                chromPeakslist.Add(chromPeaks);
            }
            return chromPeakslist;
        }

        public static List<ChromatogramPeak> GetMs2Peaklist(IReadOnlyList<RawSpectrum> spectrumList,
            double precursorMz, double productMz, int startID, int endID, ParameterBase param, double targetCE, ChromXType type, ChromXUnit unit) {
            var chromPeaks = new List<ChromatogramPeak>();
            for (int i = startID; i <= endID; i++) {
                var spec = spectrumList[i];
                if (spec.MsLevel == 2 && spec.Precursor != null) {
                    if (targetCE >= 0 && spec.CollisionEnergy >= 0 && Math.Abs(targetCE - spec.CollisionEnergy) > 1) continue; // for AIF mode

                    if (IsInMassWindow(precursorMz, spec, param.CentroidMs1Tolerance, param.AcquisitionType)) {
                        RetrieveBinnedMzIntensity(spec.Spectrum, productMz, param.CentroidMs2Tolerance,
                            out double basepeakMz, out double basepeakIntensity, out double summedIntensity);
                        var chromX = type == ChromXType.Drift ? new ChromXs(spec.DriftTime, type, unit) : new ChromXs(spec.ScanStartTime, type, unit);
                        var id = type == ChromXType.Drift ? spec.OriginalIndex : spec.ScanNumber;
                        chromPeaks.Add(new ChromatogramPeak() { ID = id, ChromXs = chromX, Mass = basepeakMz, Intensity = summedIntensity });
                    }
                }
            }
            return chromPeaks;
        }

        private static bool IsInMassWindow(double mass, RawSpectrum spec, double msTol, AcquisitionType type) {
            var specPreMz = spec.Precursor.IsolationTargetMz;
            switch (type) {
                case AcquisitionType.AIF:
                case AcquisitionType.SWATH:
                    var upperOffset = spec.Precursor.IsolationWindowUpperOffset;
                    var lowerOffset = spec.Precursor.IsolationWindowLowerOffset;
                    return specPreMz - lowerOffset <= mass && mass < specPreMz + upperOffset;
                case AcquisitionType.DDA:
                    return Math.Abs(specPreMz - mass) <= msTol;
                default:
                    throw new NotSupportedException(nameof(type));
            }
        }

        public static List<ChromatogramPeak> GetBaselineCorrectedPeaklistByMassAccuracy(IReadOnlyList<RawSpectrum> spectrumList, double centralRt, double rtBegin, double rtEnd,
            double quantMass, ParameterBase param) {
            var peaklist = new List<ChromatogramPeak>();
            var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var sliceWidth = param.CentroidMs1Tolerance;

            for (int i = 0; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.ScanStartTime < rtBegin) continue;
                if (spectrum.ScanStartTime > rtEnd) break;

                var massSpectra = spectrum.Spectrum;

                var sum = 0.0;
                var maxIntensityMz = double.MinValue;
                var maxMass = quantMass;
                //var startIndex = GetMs1StartIndex(quantMass, sliceWidth, massSpectra);
                var startIndex = SearchCollection.LowerBound(massSpectra, new RawPeakElement() { Mz = quantMass - sliceWidth }, (a, b) => a.Mz.CompareTo(b.Mz));

                for (int j = startIndex; j < massSpectra.Length; j++) {
                    if (massSpectra[j].Mz < quantMass - sliceWidth) continue;
                    else if (quantMass - sliceWidth <= massSpectra[j].Mz && massSpectra[j].Mz < quantMass + sliceWidth) {
                        sum += massSpectra[j].Intensity;
                        if (maxIntensityMz < massSpectra[j].Intensity) {
                            maxIntensityMz = massSpectra[j].Intensity; maxMass = (float)massSpectra[j].Mz;
                        }
                    }
                    else if (massSpectra[j].Mz >= quantMass + sliceWidth) break;
                }
                peaklist.Add(new ChromatogramPeak() {
                    ID = spectrum.ScanNumber,
                    ChromXs = new ChromXs(new RetentionTime(spectrum.ScanStartTime)),
                    Mass = maxMass,
                    Intensity = sum
                });
            }

            var minLeftIntensity = double.MaxValue;
            var minRightIntensity = double.MaxValue;
            var minLeftID = 0;
            var minRightID = peaklist.Count - 1;

            //searching local left minimum
            for (int i = 0; i < peaklist.Count - 1; i++) {
                var peak = peaklist[i];
                if (peak.ChromXs.Value >= centralRt) break;
                if (peak.Intensity < minLeftIntensity) {
                    minLeftIntensity = peak.Intensity;
                    minLeftID = i;
                }
            }

            //searching local right minimum
            for (int i = peaklist.Count - 1; i >= 0; i--) {
                var peak = peaklist[i];
                if (peak.ChromXs.Value <= centralRt) break;
                if (peak.Intensity < minRightIntensity) {
                    minRightIntensity = peak.Intensity;
                    minRightID = i;
                }
            }

            var baselineCorrectedPeaklist = GetBaselineCorrectedPeaklist(peaklist, minLeftID, minRightID);

            return baselineCorrectedPeaklist;
        }

        public static List<ChromatogramPeak> GetBaselineCorrectedPeaklist(List<ChromatogramPeak> peaklist, int minLeftID, int minRightID) {
            var baselineCorrectedPeaklist = new List<ChromatogramPeak>();
            if (peaklist == null || peaklist.Count == 0) return baselineCorrectedPeaklist;

            double coeff = (peaklist[minRightID].Intensity - peaklist[minLeftID].Intensity) /
                (peaklist[minRightID].ChromXs.Value - peaklist[minLeftID].ChromXs.Value);
            double intercept = (peaklist[minRightID].ChromXs.Value * peaklist[minLeftID].Intensity -
                peaklist[minLeftID].ChromXs.Value * peaklist[minRightID].Intensity) /
                (peaklist[minRightID].ChromXs.Value - peaklist[minLeftID].ChromXs.Value);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Count; i++) {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].ChromXs.Value + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist.Add(
                        new ChromatogramPeak() {
                            ID = peaklist[i].ID,
                            ChromXs = peaklist[i].ChromXs,
                            Mass = peaklist[i].Mass,
                            Intensity = correctedIntensity
                        });
                else
                    baselineCorrectedPeaklist.Add(
                        new ChromatogramPeak() {
                            ID = peaklist[i].ID,
                            ChromXs = peaklist[i].ChromXs,
                            Mass = peaklist[i].Mass,
                            Intensity = 0
                        });
            }

            return baselineCorrectedPeaklist;
        }

        // get chromatogram (ion mobility data)
        public static List<ChromatogramPeak> GetDriftChromatogramByScanRtMz(IReadOnlyList<RawSpectrum> spectrumList, int scanID, float rt, float rtWidth, float mz, float mztol) {

            var driftBinToChromPeak = new Dictionary<int, ChromatogramPeak>();
            var driftBinToBasePeakIntensity = new Dictionary<int, double>();

            void SetChromatogramPeak(RawSpectrum spectrum) {
                var driftTime = spectrum.DriftTime;
                var driftBin = (int)(driftTime * 1000);

                var intensity = GetIonAbundanceOfMzInSpectrum(spectrum.Spectrum, mz, mztol, out double basepeakMz, out double basepeakIntensity);
                if (driftBinToChromPeak.TryGetValue(driftBin, out var chromPeak)) {
                    chromPeak.Intensity += intensity;
                    if (driftBinToBasePeakIntensity[driftBin] < basepeakIntensity) {
                        driftBinToBasePeakIntensity[driftBin] = basepeakIntensity;
                        chromPeak.Mass = basepeakMz;
                    }
                }
                else {
                    driftBinToChromPeak[driftBin] = new ChromatogramPeak()
                    {
                        ID = spectrum.OriginalIndex,
                        ChromXs = new ChromXs(driftTime, ChromXType.Drift, ChromXUnit.Msec),
                        Mass = basepeakMz,
                        Intensity = intensity
                    };
                    driftBinToBasePeakIntensity[driftBin] = basepeakIntensity;
                }
            }

            //accumulating peaks from peak top to peak left
            for (int i = scanID + 1; i >= 0; i--) {
                var spectrum = spectrumList[i];
                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanStartTime < rt - rtWidth * 0.5) break;
                SetChromatogramPeak(spectrum);
            }

            //accumulating peaks from peak top to peak right
            for (int i = scanID + 2; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];
                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanStartTime > rt + rtWidth * 0.5) break;
                SetChromatogramPeak(spectrum);
            }

            return driftBinToChromPeak.Values.OrderBy(n => n.ChromXs.Value).ToList();
        }

        public static List<ChromatogramPeak> GetDriftChromatogramByRtRange(IReadOnlyList<RawSpectrum> spectrumList,
           float rtBegin, float rtEnd, float mz, float mztol, float minDt, float maxDt) {
            var minRt = Math.Min(rtBegin, rtEnd);
            var maxRt = Math.Max(rtBegin, rtEnd);
            var centerRt = (maxRt + minRt) * 0.5F;
            var rtWidth = maxRt - minRt;

            return GetDriftChromatogramByRtMz(spectrumList, centerRt, rtWidth, mz, mztol, minDt, maxDt);
        }

        public static List<ChromatogramPeak> GetDriftChromatogramByRtMz(IReadOnlyList<RawSpectrum> spectrumList,
           float rt, float rtWidth, float mz, float mztol, float minDt, float maxDt) {

            var startID = GetScanStartIndexByRt(rt, rtWidth * 0.5F, spectrumList);
            var driftBinToPeak = new Dictionary<int, ChromatogramPeak>();
            var driftBinToBasePeak = new Dictionary<int, SpectrumPeak>();

            for (int i = startID; i < spectrumList.Count; i++) {

                var spectrum = spectrumList[i];
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftScan = spectrum.DriftScanNumber;
                var driftBin = (int)(driftTime * 1000);

                if (retention < rt - rtWidth * 0.5) continue;
                if (driftTime < minDt || driftTime > maxDt) continue;
                if (retention > rt + rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                    out basepeakMz, out basepeakIntensity);
                if (!driftBinToPeak.ContainsKey(driftBin)) {
                    driftBinToPeak[driftBin] = new ChromatogramPeak() {
                        ID = driftScan, ChromXs = new ChromXs(driftTime, ChromXType.Drift, ChromXUnit.Msec), Mass = basepeakMz, Intensity = intensity
                    };
                    driftBinToBasePeak[driftBin] = new SpectrumPeak() { Mass = basepeakMz, Intensity = basepeakIntensity };
                }
                else {
                    driftBinToPeak[driftBin].Intensity += intensity;
                    if (driftBinToBasePeak[driftBin].Intensity < basepeakIntensity) {
                        driftBinToBasePeak[driftBin].Mass = basepeakMz;
                        driftBinToBasePeak[driftBin].Intensity = basepeakIntensity;
                        driftBinToPeak[driftBin].Mass = basepeakMz;
                    }
                }
            }
            var peaklist = new List<ChromatogramPeak>();
            foreach (var value in driftBinToPeak.Values) {
                peaklist.Add(value);
            }

            peaklist = peaklist.OrderBy(n => n.ChromXs.Value).ToList();
            return peaklist;
        }

        public static double GetIonAbundanceOfMzInSpectrum(RawPeakElement[] massSpectra,
            float mz, float mztol, out double basepeakMz, out double basepeakIntensity) {
            var startIndex = SearchCollection.LowerBound(massSpectra, new RawPeakElement() { Mz = mz - mztol }, (a, b) => a.Mz.CompareTo(b.Mz));
            // var startIndex = GetMs1StartIndex(mz, mztol, massSpectra);
            double sum = 0, maxIntensityMz = 0.0, maxMass = mz;

            //bin intensities for focused MZ +- ms1Tolerance
            for (int j = startIndex; j < massSpectra.Length; j++) {
                if (massSpectra[j].Mz < mz - mztol) continue;
                else if (mz - mztol <= massSpectra[j].Mz &&
                    massSpectra[j].Mz <= mz + mztol) {
                    sum += massSpectra[j].Intensity;
                    if (maxIntensityMz < massSpectra[j].Intensity) {
                        maxIntensityMz = massSpectra[j].Intensity;
                        maxMass = massSpectra[j].Mz;
                    }
                }
                else if (massSpectra[j].Mz > mz + mztol) break;
            }
            basepeakMz = maxMass;
            basepeakIntensity = maxIntensityMz;
            return sum;
        }

        public static List<SpectrumPeak> GetAverageSpectrum(IReadOnlyList<RawSpectrum> spectrumList, double start, double end, double bin, int targetExperimentID = -1) {
            var min = Math.Min(start, end);
            var max = Math.Max(start, end);
            var lo = SearchCollection.LowerBound(spectrumList, new RawSpectrum() { ScanStartTime = min }, (a, b) => a.ScanStartTime.CompareTo(b.ScanStartTime));
            var hi = SearchCollection.UpperBound(spectrumList, new RawSpectrum() { ScanStartTime = max }, (a, b) => a.ScanStartTime.CompareTo(b.ScanStartTime));
            var points = new List<int>();
            for (int i = lo; i < hi; i++) {
                var spec = spectrumList[i];
                if (targetExperimentID == -1) {
                    points.Add(i);
                }
                else if (targetExperimentID == spec.ExperimentID) {
                    points.Add(i);
                }
            }
            return GetAverageSpectrum(spectrumList, points, bin);
        }


        public static List<SpectrumPeak> GetAverageSpectrum(IReadOnlyList<RawSpectrum> spectrumList, List<int> points, double bin) {
            var peaks = new List<SpectrumPeak>();
            var mass2peaks = new Dictionary<int, List<SpectrumPeak>>();
            var factor = 1.0 / bin;

            foreach (var point in points) {
                if (point < 0 || point > spectrumList.Count - 1) continue;
                var spec = spectrumList[point];
                foreach (var peak in spec.Spectrum) {
                    var mass = (int)(peak.Mz * factor);
                    var intensity = peak.Intensity;
                    var spectrumPeak = new SpectrumPeak() { Mass = peak.Mz, Intensity = intensity };
                    if (mass2peaks.ContainsKey(mass)) {
                        mass2peaks[mass].Add(spectrumPeak);
                    }
                    else {
                        mass2peaks[mass] = new List<SpectrumPeak>() { spectrumPeak };
                    }
                }
            }

            foreach (var item in mass2peaks) {
                var repMass = item.Value.Argmax(n => n.Intensity).Mass;
                var aveIntensity = item.Value.Sum(n => n.Intensity) / (double)points.Count;
                var peak = new SpectrumPeak() { Mass = repMass, Intensity = aveIntensity };
                peaks.Add(peak);
            }
            return peaks;
        }

        public static List<SpectrumPeak> GetSubtractSpectrum(IReadOnlyList<RawSpectrum> spectrumList, 
            double mainStart, double mainEnd, 
            double subtractStart, double subtractEnd,
            double bin, int targetExperimentID = -1) {
            var mainAveSpec = GetAverageSpectrum(spectrumList, mainStart, mainEnd, bin, targetExperimentID);
            var subtractAveSpec = GetAverageSpectrum(spectrumList, subtractStart, subtractEnd, bin, targetExperimentID);

            return GetSubtractSpectrum(mainAveSpec, subtractAveSpec, bin);
        }

        public static List<SpectrumPeak> GetSubtractSpectrum(IReadOnlyList<SpectrumPeak> mainPeaks, IReadOnlyList<SpectrumPeak> subtractPeaks, double bin) {
            var peaks = new List<SpectrumPeak>();
            var mass2peaks = new Dictionary<int, List<SpectrumPeak>>();
            var factor = 1.0 / bin;

            foreach (var peak in mainPeaks) {
                var mass = (int)(peak.Mass * factor);
                var intensity = peak.Intensity;
                var spectrumPeak = new SpectrumPeak() { Mass = peak.Mass, Intensity = intensity };
                if (mass2peaks.ContainsKey(mass)) {
                    mass2peaks[mass].Add(spectrumPeak);
                }
                else {
                    mass2peaks[mass] = new List<SpectrumPeak>() { spectrumPeak };
                }
            }

            var mass2peak = new Dictionary<int, SpectrumPeak>();
            foreach (var item in mass2peaks) {
                var repMass = item.Value.Argmax(n => n.Intensity).Mass;
                var intensity = item.Value.Sum(n => n.Intensity);
                var peak = new SpectrumPeak() { Mass = repMass, Intensity = intensity };

                var binnedMass = (int)(repMass * factor);
                if (mass2peak.ContainsKey(binnedMass)) {
                    mass2peak[binnedMass].Intensity += intensity;
                }
                else {
                    mass2peak[binnedMass] = peak;
                }
            }

            foreach (var peak in subtractPeaks) {
                var mass = (int)(peak.Mass * factor);
                var intensity = peak.Intensity;

                if (!mass2peak.ContainsKey(mass)) continue;
                else {
                    mass2peak[mass].Intensity -= intensity;
                }
            }

            foreach (var item in mass2peak) {
                var peak = item.Value;
                if (peak.Intensity > 0) {
                    peaks.Add(peak);
                }
            }
            return peaks;
        }

        public static List<List<ChromatogramPeak>> GetAccumulatedMs2PeakListList(IReadOnlyList<RawSpectrum> spectrumList,
             ChromatogramPeakFeature rtChromPeakFeature, List<SpectrumPeak> curatedSpectrum, double minDriftTime, double maxDriftTime, IonMode ionMode) {
            var ms2peaklistlist = new List<List<ChromatogramPeak>>();
            var scanPolarity = ionMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            var rt = rtChromPeakFeature.ChromXsTop.Value;
            var rtLeft = rtChromPeakFeature.ChromXsLeft.Value;
            var rtRight = rtChromPeakFeature.ChromXsRight.Value;

            var binMultiplyFactor = 1000;
            var accumulatedRtRange = 1f;
            if (rtRight - rt > accumulatedRtRange) {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtRight = rt + accumulatedRtRange;
            }
            if (rt - rtLeft > accumulatedRtRange) {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtLeft = rt - accumulatedRtRange;
            }

            var mz = rtChromPeakFeature.Mass;
            var scanID = rtChromPeakFeature.MS1RawSpectrumIdTop;

            // <mzBin, <driftTimeIndex, [driftTimeBin, accumulatedIntensity]>>
            var chromatogramBin = new Dictionary<int, Dictionary<int, double[]>>();

            // <mzBin, <driftTimeBin, driftTimeIndex>>
            var driftTimeIndexDic = new Dictionary<int, Dictionary<int, int>>();

            // <mz, driftTimeIndex>
            var driftTimeCounter = new Dictionary<int, int>();

            var driftTimeBinSet = new HashSet<int>();
            //set initial mz
            foreach (var peak in curatedSpectrum) {
                var massBin = (int)(peak.Mass * binMultiplyFactor + 0.5);
                var index = 0;
                driftTimeCounter.Add(massBin, index + 1);
            }

            //accumulating peaks from peak top to peak left
            for (int i = scanID; i >= 0; i--) {
                var spectrum = spectrumList[i];
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.MsLevel <= 1) continue;
                if (spectrum.ScanStartTime < rtLeft) break;
                if (spectrum.DriftTime < minDriftTime || spectrum.DriftTime > maxDriftTime) continue;
                var massSpectra = spectrum.Spectrum;
                foreach (var s in massSpectra) {
                    var massBin = (int)(s.Mz * binMultiplyFactor + 0.5);
                    var driftBin = (int)(spectrum.DriftTime * binMultiplyFactor + 0.5);

                    driftTimeBinSet.Add(driftBin);
                    if (!driftTimeCounter.ContainsKey(massBin)) continue;
                    if (driftTimeIndexDic.ContainsKey(massBin)) {
                        if (driftTimeIndexDic[massBin].ContainsKey(driftBin)) {
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]][1] += s.Intensity;
                        }
                        else {
                            driftTimeIndexDic[massBin].Add(driftBin, driftTimeCounter[massBin]);
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]] = new double[] { driftBin, s.Intensity };
                            driftTimeCounter[massBin] += 1;
                        }
                    }
                    else {
                        // <driftBint, driftTimeIndex>
                        var tmp1 = new Dictionary<int, int>();
                        tmp1.Add(driftBin, 0);
                        driftTimeIndexDic.Add(massBin, tmp1);

                        // <driftTimeIndex, [driftBin, intensity]>
                        var tmp2 = new Dictionary<int, double[]>();
                        tmp2.Add(0, new double[] { driftBin, s.Intensity });
                        chromatogramBin.Add(massBin, tmp2);
                    }
                }
            }

            for (int i = scanID + 1; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.MsLevel == 1) continue;
                if (spectrum.DriftTime < minDriftTime || spectrum.DriftTime > maxDriftTime) continue;
                if (spectrum.ScanStartTime > rtRight) break;

                var massSpectra = spectrum.Spectrum;

                foreach (var s in massSpectra) {
                    var massBin = (int)(s.Mz * binMultiplyFactor + 0.5);
                    var driftBin = (int)(spectrum.DriftTime * binMultiplyFactor + 0.5);
                    driftTimeBinSet.Add(driftBin);
                    if (!driftTimeCounter.ContainsKey(massBin)) continue;

                    if (driftTimeIndexDic.ContainsKey(massBin)) {
                        if (driftTimeIndexDic[massBin].ContainsKey(driftBin)) {
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]][1] += s.Intensity;
                        }
                        else {
                            driftTimeIndexDic[massBin].Add(driftBin, driftTimeCounter[massBin]);
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]] = new double[] { driftBin, s.Intensity };
                            driftTimeCounter[massBin] += 1;
                        }
                    }
                    else {
                        // <driftBint, driftTimeIndex>
                        var tmp1 = new Dictionary<int, int>();
                        tmp1.Add(driftBin, 0);
                        driftTimeIndexDic.Add(massBin, tmp1);

                        // <driftTimeIndex, [driftBin, intensity]>
                        var tmp2 = new Dictionary<int, double[]>();
                        tmp2.Add(0, new double[] { driftBin, s.Intensity });
                        chromatogramBin.Add(massBin, tmp2);
                    }
                }
            }

            foreach (var mzBin in chromatogramBin.Keys) {
                var peaklist = new List<double[]>();
                var targetMz = Math.Round((double)mzBin / binMultiplyFactor, 3);
                // <driftTimeIndex, [driftBin, accumulatedIntensity]>
                var targetChromato = chromatogramBin[mzBin];
                var counter = 0;
                var tmpDriftTimeBinSet = new HashSet<int>();
                foreach (var values in targetChromato.Values) {
                    tmpDriftTimeBinSet.Add((int)(values[0] + 0.5));
                    var driftTime = Math.Round(values[0] / binMultiplyFactor, 3);
                    peaklist.Add(new double[] { 0, driftTime, targetMz, values[1] });
                }
                foreach (var df in driftTimeBinSet.Except(tmpDriftTimeBinSet)) {
                    // add not detected driftTime
                    var driftTime = Math.Round((double)df / binMultiplyFactor, 3);
                    peaklist.Add(new double[] { 0, driftTime, targetMz, 0 });
                }
                var sortedPeaklist = peaklist.OrderBy(n => n[1]).ToList();
                var ms2peaklist = new List<ChromatogramPeak>();
                foreach (var peaks in sortedPeaklist) {
                    ms2peaklist.Add(new ChromatogramPeak() {
                        ID = counter++, ChromXs = new ChromXs(peaks[1], ChromXType.Drift, ChromXUnit.Msec), Mass = peaks[2], Intensity = peaks[3]
                    });
                }
                ms2peaklistlist.Add(ms2peaklist);
            }
            return ms2peaklistlist;
        }


        // get spectrum
        public static List<SpectrumPeak> GetMassSpectrum(IReadOnlyList<RawSpectrum> spectrumList, MSDecResult msdecResult, ExportspectraType type, int msScanPoint, ParameterBase param) {
            if (msScanPoint < 0) return new List<SpectrumPeak>();
            if (type == ExportspectraType.deconvoluted) return msdecResult.Spectrum;
            if (type == ExportspectraType.centroid && param.AcquisitionType == AcquisitionType.DDA) return msdecResult.Spectrum;

            var spectra = new List<SpectrumPeak>();
            var spectrum = spectrumList[msScanPoint];
            var massSpectra = spectrum.Spectrum;

            var mzBegin = param.MachineCategory == MachineCategory.GCMS ? param.MassRangeBegin : param.Ms2MassRangeBegin;
            var mzEnd = param.MachineCategory == MachineCategory.GCMS ? param.MassRangeEnd : param.Ms2MassRangeEnd;

            for (int i = 0; i < massSpectra.Length; i++) {
                if (massSpectra[i].Mz < mzBegin) continue;
                if (massSpectra[i].Mz > mzEnd) continue;
                spectra.Add(new SpectrumPeak() { Mass = massSpectra[i].Mz, Intensity = massSpectra[i].Intensity });
            }

            if (param.MS2DataType == MSDataType.Centroid) return spectra.Where(n => n.Intensity > param.AmplitudeCutoff).ToList();
            if (spectra.Count == 0) return new List<SpectrumPeak>();
            if (type == ExportspectraType.profile) return spectra;

           // var centroidedSpectra = SpectralCentroiding.Centroid(spectra, 0.0);
            var centroidedSpectra = SpectralCentroiding.CentroidByLocalMaximumMethod(spectra);
            if (centroidedSpectra != null && centroidedSpectra.Count != 0)
                return centroidedSpectra;
            else
                return spectra;
        }

        public static List<SpectrumPeak> GetCentroidMassSpectra(IReadOnlyList<RawSpectrum> spectrumList, MSDataType dataType,
            int msScanPoint, float amplitudeThresh, float mzBegin, float mzEnd) {
            if (msScanPoint < 0) return new List<SpectrumPeak>();

            return GetCentroidMassSpectra(spectrumList[msScanPoint], dataType, amplitudeThresh, mzBegin, mzEnd);
        }

        public static List<SpectrumPeak> GetCentroidMassSpectra(RawSpectrum spectrum, MSDataType dataType,
            float amplitudeThresh, float mzBegin, float mzEnd) {
            if (spectrum == null) return new List<SpectrumPeak>();

            var spectra = new List<SpectrumPeak>();
            var massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++) {
                if (massSpectra[i].Mz < mzBegin) continue;
                if (massSpectra[i].Mz > mzEnd) continue;
                spectra.Add(new SpectrumPeak() { Mass = massSpectra[i].Mz, Intensity = massSpectra[i].Intensity });
            }

            if (dataType == MSDataType.Centroid) return spectra.Where(n => n.Intensity > amplitudeThresh).ToList();

            if (spectra.Count == 0) return new List<SpectrumPeak>();

            //var centroidedSpectra = SpectralCentroiding.Centroid(spectra, amplitudeThresh);
            var centroidedSpectra = SpectralCentroiding.CentroidByLocalMaximumMethod(spectra, absThreshold: amplitudeThresh);

            if (centroidedSpectra != null && centroidedSpectra.Count != 0)
                return centroidedSpectra;
            else
                return spectra;
        }

        public static List<SpectrumPeak> ConvertToSpectrumPeaks(RawPeakElement[] peakElements) {
            var peaks = new List<SpectrumPeak>();
            foreach (var peak in peakElements) {
                peaks.Add(new SpectrumPeak(peak.Mz, peak.Intensity));
            }
            return peaks;
        }

        public static RawPeakElement[] AccumulateMS1Spectrum(IReadOnlyList<RawSpectrum> spectra, double rtBegin, double rtEnd, int bin = 5) {
            var factor = Math.Pow(10, 5);
            var dict = new Dictionary<long, double>();
            var counter = 0;
            foreach (var spec in spectra.Where(n => n.MsLevel == 1)) {
                if (spec.ScanStartTime < rtBegin) continue;
                if (spec.ScanStartTime > rtEnd) break;
                counter++;
                foreach (var peak in spec.Spectrum) {
                    var mz = peak.Mz;
                    var intensity = peak.Intensity;
                    var mzlong = (long)(mz * factor);

                    if (dict.ContainsKey(mzlong)) {
                        dict[mzlong] += intensity;
                    }
                    else {
                        dict[mzlong] = intensity;
                    }
                }
            }
            var revFact = Math.Pow(0.1, 5);
            var elements = new List<RawPeakElement>();
            foreach (var pair in dict) {
                var mz = (double)pair.Key * revFact;
                var intensity = Math.Round(pair.Value / (double)counter, 0);
                elements.Add(new RawPeakElement() { Mz = mz, Intensity = intensity });
            }
            return elements.OrderBy(n => n.Mz).ToArray();
        }

        public static RawPeakElement[] AccumulateMS1Spectrum(IReadOnlyList<RawSpectrum> spectra, int bin = 5) {
            return AccumulateMS1Spectrum(spectra, double.MinValue, double.MaxValue, bin);
        }

        public static List<SpectrumPeak> GetAccumulatedMs2Spectra(IReadOnlyList<RawSpectrum> spectrumList,
           ChromatogramPeakFeature driftSpot, ChromatogramPeakFeature peakSpot, ParameterBase param) {
            var massSpectrum = CalcAccumulatedMs2Spectra(spectrumList, peakSpot, driftSpot, param.CentroidMs1Tolerance);
            if (param.MS2DataType == MSDataType.Profile && massSpectrum.Count > 0) {
                //return SpectralCentroiding.Centroid(massSpectrum);
                return SpectralCentroiding.CentroidByLocalMaximumMethod(massSpectrum);
            }
            else {
                return massSpectrum;
            }
        }

        public static List<SpectrumPeak> CalcAccumulatedMs2Spectra(IReadOnlyList<RawSpectrum> spectrumList,
            ChromatogramPeakFeature rtChromFeature, ChromatogramPeakFeature dtChromFeature, double mzTol) {
            var rt = rtChromFeature.ChromXsTop.Value;
            var rtLeft = rtChromFeature.ChromXsLeft.Value;
            var rtRight = rtChromFeature.ChromXsRight.Value;

            var rtRange = 1f;

            if (rtRight - rt > rtRange) {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtRight = rt + rtRange;
            }
            if (rt - rtLeft > rtRange) {
                //Console.WriteLine("Peak: " + rtChromFeature.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtLeft = rt - rtRange;
            }

            var mz = rtChromFeature.Mass;
            var scanID = dtChromFeature.MS1RawSpectrumIdTop;
            var dataPointDriftBin = (int)(dtChromFeature.ChromXsTop.Value * 1000);

            var spectrumBin = new Dictionary<int, double[]>();
            //accumulating peaks from peak top to peak left
            for (int i = scanID; i >= 0; i--) {
                var spectrum = spectrumList[i];
                if (spectrum.MsLevel == 1) continue;

                var driftTime = spectrum.DriftTime;
                var driftBin = (int)(driftTime * 1000);
                if (driftBin != dataPointDriftBin) continue;

                var retention = spectrum.ScanStartTime;
                if (retention < rtLeft) break;

                var massSpectra = spectrum.Spectrum;
                foreach (var s in massSpectra) {
                    var massBin = (int)(s.Mz * 1000);
                    if (!spectrumBin.ContainsKey(massBin)) {
                        spectrumBin[massBin] = new double[3] { s.Mz, s.Intensity, s.Intensity };
                    }
                    else {
                        spectrumBin[massBin][1] += s.Intensity;
                        if (spectrumBin[massBin][2] < s.Intensity) {
                            spectrumBin[massBin][0] = s.Mz;
                            spectrumBin[massBin][2] = s.Intensity;
                        }
                    }
                }
            }

            for (int i = scanID + 1; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];
                if (spectrum.MsLevel == 1) continue;

                var driftTime = spectrum.DriftTime;
                var driftBin = (int)(driftTime * 1000);
                if (driftBin != dataPointDriftBin) continue;

                var retention = spectrum.ScanStartTime;
                if (retention > rtRight) break;

                var massSpectra = spectrum.Spectrum;
                foreach (var s in massSpectra) {
                    var massBin = (int)(s.Mz * 1000);
                    if (!spectrumBin.ContainsKey(massBin)) {
                        // [accurate mass, intensity, max intensity]
                        spectrumBin[massBin] = new double[3] { s.Mz, s.Intensity, s.Intensity };
                    }
                    else {
                        spectrumBin[massBin][1] += s.Intensity;
                        if (spectrumBin[massBin][2] < s.Intensity) {
                            spectrumBin[massBin][0] = s.Mz;
                            spectrumBin[massBin][2] = s.Intensity;
                        }
                    }
                }
            }

            var peaklist = new List<SpectrumPeak>();
            foreach (var value in spectrumBin.Values) {
                peaklist.Add(new SpectrumPeak() { Mass = value[0], Intensity = value[1] });
            }
            peaklist = peaklist.OrderBy(n => n.Mass).ToList();
            return peaklist;
        }

        public static MSScanProperty GetNormalizedMSScanProperty(IMSScanProperty scanProp, ParameterBase param) {
            return GetNormalizedMSScanProperty(scanProp, param.MspSearchParam);
        }

        public static MSScanProperty GetNormalizedMSScanProperty(IMSScanProperty scanProp, MsRefSearchParameterBase specMatchParam) {
            var prop = new MSScanProperty() {
                ChromXs = scanProp.ChromXs, IonMode = scanProp.IonMode, PrecursorMz = scanProp.PrecursorMz,
                Spectrum = GetNormalizedMs2Spectra(scanProp.Spectrum, specMatchParam.AbsoluteAmpCutoff, specMatchParam.RelativeAmpCutoff), ScanID = scanProp.ScanID
            };
            return prop;
        }

        public static MSScanProperty GetNormalizedMSScanProperty(ChromatogramPeakFeature chromPeak, MSDecResult result, ParameterBase param) {
            var specMatchParam = param.MspSearchParam;
            var prop = new MSScanProperty() {
                ChromXs = chromPeak.ChromXs, IonMode = chromPeak.IonMode, PrecursorMz = chromPeak.PrecursorMz,
                Spectrum = GetNormalizedMs2Spectra(result.Spectrum, specMatchParam.AbsoluteAmpCutoff, specMatchParam.RelativeAmpCutoff), ScanID = chromPeak.ScanID
            };
            return prop;
        }

        public static List<SpectrumPeak> GetNormalizedMs2Spectra(List<SpectrumPeak> spectrum, double abscutoff, double relcutoff) {
            if (spectrum.IsEmptyOrNull()) return null;
            var massSpec = new List<SpectrumPeak>();
            var maxIntensity = spectrum.Max(n => n.Intensity);
            foreach (var peak in spectrum) {
                if (peak.Intensity > maxIntensity * relcutoff && peak.Intensity > abscutoff) {
                    massSpec.Add(new SpectrumPeak() { Mass = peak.Mass, Intensity = peak.Intensity / maxIntensity * 100.0 });
                }
            }
            return massSpec;
        }

        public static List<SpectrumPeak> GetAndromedaMS2Spectrum(List<SpectrumPeak> spectrum, ParameterBase param, IupacDatabase iupac, int precursorCharge) {
            if (spectrum.IsEmptyOrNull()) return new List<SpectrumPeak>();
            var peaks = ConvertToDeisotopeAndSingleChargeStateMS2Spectrum(spectrum, param, iupac, precursorCharge);
            if (peaks.IsEmptyOrNull()) return spectrum;
            return GetBinnedMS2Spectrum(peaks, param.AndromedaDelta, param.AndromedaMaxPeaks);
        }

        /// <summary>
        /// Give centroid spectrum
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="abscutoff"></param>
        /// <param name="relcutoff"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> ConvertToDeisotopeAndSingleChargeStateMS2Spectrum(List<SpectrumPeak> spectrum, ParameterBase param, IupacDatabase iupac, int precursorCharge) {
            if (spectrum.IsEmptyOrNull()) return new List<SpectrumPeak>();
            IsotopeEstimator.EstimateIsotopes(spectrum, param, iupac, param.CentroidMs2Tolerance, precursorCharge);
            var peaks = new List<SpectrumPeak>();

            // De isotoping
            foreach (var peak in spectrum.OrderBy(n => n.IsotopeParentPeakID).ThenBy(n => n.IsotopeWeightNumber)) {
                if (peak.IsotopeWeightNumber == 0) {
                    peaks.Add(peak);
                }
                else {
                    peaks[peaks.Count - 1].Intensity += peak.Intensity;
                }
            }

            // collapse charge state
            foreach (var peak in peaks) {
                if (peak.Charge >= 2) {
                    peak.Mass = peak.Mass * (double)peak.Charge;
                }
            }

            return peaks.OrderBy(n => n.PeakID).ToList();
        }

      
        public static List<SpectrumPeak> GetBinnedMS2Spectrum(List<SpectrumPeak> spectrum, double delta = 100, int maxPeaks = 12) {

            var peaks = new List<SpectrumPeak>();
            var range2Peaks = new Dictionary<int, List<SpectrumPeak>>();

            foreach (var peak in spectrum) {
                var mass = peak.Mass;
                var massframe = (int)(mass / delta);
                if (range2Peaks.ContainsKey(massframe))
                    range2Peaks[massframe].Add(peak);
                else
                    range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
            }

            foreach (var pair in range2Peaks) {
                var counter = 1;
                foreach (var peak in pair.Value.OrderByDescending(n => n.Intensity)) {
                    if (counter > maxPeaks) break;
                    peaks.Add(peak);
                    counter++;
                }
            }
            return peaks.OrderBy(n => n.PeakID).ToList();
        }

        // get properties
        /// <summary>
        /// 
        /// </summary>
        /// <param name="spectrumList"></param>
        /// <param name="ionmode"></param>
        /// <returns>[0] min Mz [1] max Mz</returns>
        public static float[] GetMs1Range(IReadOnlyList<RawSpectrum> spectrumList, IonMode ionmode) {
            float minMz = float.MaxValue, maxMz = float.MinValue;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++) {
                if (spectrumList[i].MsLevel > 1) continue;
                if (spectrumList[i].ScanPolarity != scanPolarity) continue;
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;

                if (spectrumList[i].LowestObservedMz < minMz) minMz = (float)spectrumList[i].LowestObservedMz;
                if (spectrumList[i].HighestObservedMz > maxMz) maxMz = (float)spectrumList[i].HighestObservedMz;
            }
            return new float[] { minMz, maxMz };
        }

        // annotation

        public static void SetPeptideMsProperty(ChromatogramPeakFeature feature, PeptideMsReference reference, MsScanMatchResult result) {
            if (reference == null) return;
            SetPeptidePropertyCore(feature, reference);
            feature.Name = result.Name;

            var chargeNum = feature.PeakCharacter.Charge;
            var chargeString = chargeNum == 1 ? string.Empty : chargeNum.ToString();
            var adductString = "[M+" + chargeString + "H]" + chargeString + "+";
            var type = AdductIonParser.GetAdductIonBean(adductString);

            feature.AddAdductType(type);
        }

        private static void SetPeptidePropertyCore(IMoleculeProperty property, PeptideMsReference reference) {
            property.Formula = reference.Peptide.Formula;
            property.Ontology = "Peptide";
            property.SMILES = reference.Peptide.Position.ToString();
            property.InChIKey = reference.Peptide.DatabaseOrigin;
        }

        public static void SetPeptideMsPropertyAsSuggested(ChromatogramPeakFeature feature, PeptideMsReference reference, MsScanMatchResult result) {
            if (reference == null) return;
            SetPeptidePropertyCore(feature, reference);

            var chargeNum = feature.PeakCharacter.Charge;
            var chargeString = chargeNum == 1 ? string.Empty : chargeNum.ToString();
            var adductString = "[M+" + chargeString + "H]" + chargeString + "+";
            var type = AdductIonParser.GetAdductIonBean(adductString);

            feature.AddAdductType(type);
            feature.Name = "w/o MS2: " + result.Name;
        }

        public static void SetMoleculeMsProperty(ChromatogramPeakFeature feature, MoleculeMsReference reference, MsScanMatchResult result, bool isTextDB = false) {
            SetMoleculePropertyCore(feature, reference);
            feature.Name = result.Name;
            feature.AddAdductType(reference.AdductType);
            if (isTextDB) return;
            //if (!result.IsSpectrumMatch) {
            //    feature.Name = "w/o MS2: " + result.Name;
            //}
        }

        public static void SetTextDBMoleculeMsProperty(ChromatogramPeakFeature feature, MoleculeMsReference reference, MsScanMatchResult result) {
            SetMoleculeMsProperty(feature, reference, result, true);
        }

        public static void SetMoleculeMsPropertyAsSuggested(ChromatogramPeakFeature feature, MoleculeMsReference reference, MsScanMatchResult result) {
            SetMoleculePropertyCore(feature, reference);
            feature.AddAdductType(reference.AdductType);
            feature.Name = "w/o MS2: " + result.Name;
        }

        public static void SetMoleculeMsPropertyAsConfidence<T>(T feature, MoleculeMsReference reference, MsScanMatchResult result)
            where T: IMoleculeProperty, IIonProperty {
            SetMoleculePropertyCore(feature, reference);
            feature.AdductType = reference.AdductType;
            feature.Name = result.Name;
        }

        public static void SetMoleculeMsPropertyAsUnsettled<T>(T feature, MoleculeMsReference reference, MsScanMatchResult result)
            where T: IMoleculeProperty, IIonProperty {
            SetMoleculePropertyCore(feature, reference);
            feature.AdductType = reference.AdductType;
            feature.Name = $"Unsettled: {result.Name}";
        }

        private static void SetMoleculePropertyCore(IMoleculeProperty property, MoleculeMsReference reference) {
            property.Formula = reference.Formula;
            property.Ontology = string.IsNullOrEmpty(reference.Ontology) ? reference.CompoundClass : reference.Ontology;
            property.SMILES = reference.SMILES;
            property.InChIKey = reference.InChIKey;
        }

        public static void ClearMoleculePropertyInfomation(IMoleculeProperty property) {
            property.Name = string.Empty;
            property.Formula = new Formula();
            property.Ontology = string.Empty;
            property.SMILES = string.Empty;
            property.InChIKey = string.Empty;
        }

        public static int GetAnnotationCode(MsScanMatchResult result, MachineCategory category) {
            var code = 999; // unknown
            if (result == null) return code;
            if (category == MachineCategory.GCMS) {
                if (result.IsSpectrumMatch) code = 440; //440: EI-MS matched
                if (result.IsRiMatch || result.IsRtMatch) code = 340; //340: RT/RI+EI-MS matched
                return code;
            }
            else if (category == MachineCategory.LCIMMS || category == MachineCategory.IMMS) {
                if (result.IsPrecursorMzMatch) code = 530; // 430: m/z+MS/MS matched
                if (result.IsSpectrumMatch) code = 430; // 430: m/z+MS/MS matched
                if (code == 430) {
                    if (result.IsLipidPositionMatch) {
                        code = 400; // 400: MS/MS matched, lipid acyl position resolved
                    }
                    else if (result.IsLipidChainsMatch) {
                        code = 410; //410: MS/MS matched, lipid acyl chain resolved
                    }
                    else if (result.IsLipidClassMatch) {
                        code = 420; //420: MS/MS matched, lipid class resolved
                    }
                }

                if (result.IsRtMatch && result.IsCcsMatch) {
                    if (result.IsSpectrumMatch) {
                        switch (code) {
                            case 400: return 100;
                            case 410: return 110;
                            case 420: return 120;
                            case 430: return 130;
                        }
                    }
                    else if (result.IsPrecursorMzMatch) {
                        return 500;
                    }
                }
                else if (result.IsRtMatch) {
                    if (result.IsSpectrumMatch) {
                        switch (code) {
                            case 400: return 300;
                            case 410: return 310;
                            case 420: return 320;
                            case 430: return 330;
                        }
                    }
                    else if (result.IsPrecursorMzMatch) {
                        return 520;
                    }
                }
                else if (result.IsCcsMatch) {
                    if (result.IsSpectrumMatch) {
                        switch (code) {
                            case 400: return 200;
                            case 410: return 210;
                            case 420: return 220;
                            case 430: return 230;
                        }
                    }
                    else if (result.IsPrecursorMzMatch) {
                        return 510;
                    }
                }

                return code;
            }
            else {
                if (result.IsPrecursorMzMatch) code = 530; // 530: m/z matched
                if (result.IsSpectrumMatch) code = 430; // 430: m/z+MS/MS matched
                if (code == 430) {
                    if (result.IsLipidPositionMatch) {
                        code = 400; // 400: MS/MS matched, lipid acyl position resolved
                    }
                    else if (result.IsLipidChainsMatch) {
                        code = 410; //410: MS/MS matched, lipid acyl chain resolved
                    }
                    else if (result.IsLipidClassMatch) {
                        code = 420; //420: MS/MS matched, lipid class resolved
                    }
                }

                if (result.IsRtMatch || result.IsRiMatch) {
                    if (result.IsSpectrumMatch) {
                        switch (code) {
                            case 400: return 300;
                            case 410: return 310;
                            case 420: return 320;
                            case 430: return 330;
                        }
                    }
                    else if (result.IsPrecursorMzMatch) {
                        return 520;
                    }
                }

                return code;
            }
        }

        public static int GetAnnotationCode(MsScanMatchResult result, ParameterBase param) {
            return GetAnnotationCode(result, param.MachineCategory);
        }

        // Alignment result
        public static double GetInterpolatedValueForMissingValue(List<AlignmentChromPeakFeature> features,
            bool replaceZeroToHalf, string exportType) {
            if (exportType == "Height" || exportType == "Area" || exportType == "Normalized height" || exportType == "Normalized area") {
                if (replaceZeroToHalf) {
                    var nonZeroMin = double.MaxValue;
                    foreach (var peak in features) {
                        var variable = GetSpotValue(peak, exportType);
                        if (variable > 0 && nonZeroMin > variable)
                            nonZeroMin = variable;
                    }

                    if (nonZeroMin == double.MaxValue)
                        nonZeroMin = 1;
                    return nonZeroMin;
                }
                else {
                    return -1;
                }
            }
            else {
                return -1;
            }
        }

        public static string GetSpotValueAsString(AlignmentChromPeakFeature spotProperty, string exportType) {
            switch (exportType) {
                case "Height": return Math.Round(spotProperty.PeakHeightTop, 0).ToString();
                case "Normalized height": return spotProperty.NormalizedPeakHeight.ToString();
                case "Normalized area": return spotProperty.NormalizedPeakAreaAboveZero.ToString();
                case "Area": return Math.Round(spotProperty.PeakAreaAboveZero, 0).ToString();
                case "ID": return spotProperty.MasterPeakID.ToString();
                case "RT": return Math.Round(spotProperty.ChromXsTop.RT.Value, 3).ToString();
                case "RI": return Math.Round(spotProperty.ChromXsTop.RI.Value, 2).ToString();
                case "Mobility": return Math.Round(spotProperty.ChromXsTop.Drift.Value, 5).ToString();
                case "CCS": return Math.Round(spotProperty.CollisionCrossSection, 3).ToString();
                case "MZ": return Math.Round(spotProperty.Mass, 5).ToString();
                case "SN": return Math.Round(spotProperty.PeakShape.SignalToNoise, 1).ToString();
                case "MSMS": return spotProperty.MS2RawSpectrumID >= 0 ? "TRUE" : "FALSE";
                default: return string.Empty;
            }
        }

        public static double GetSpotValue(AlignmentChromPeakFeature spotProperty, string exportType) {
            switch (exportType) {
                case "Height": return spotProperty.PeakHeightTop;
                case "Normalized height": return spotProperty.NormalizedPeakHeight;
                case "Normalized area": return spotProperty.NormalizedPeakAreaAboveZero;
                case "Area": return spotProperty.PeakAreaAboveZero;
                case "ID": return spotProperty.MasterPeakID;
                case "RT": return spotProperty.ChromXsTop.RT.Value;
                case "RI": return spotProperty.ChromXsTop.RI.Value;
                case "Mobility": return spotProperty.ChromXsTop.Drift.Value;
                case "CCS": return spotProperty.CollisionCrossSection;
                case "MZ": return spotProperty.Mass;
                case "SN": return spotProperty.PeakShape.SignalToNoise;
                case "MSMS": return spotProperty.MS2RawSpectrumID;
                default: return -1;
            }
        }

        public static List<ChromatogramPeakFeature> GetChromPeakFeatureObjectsIntegratingRtAndDriftData(List<ChromatogramPeakFeature> features) {
            var objects = new List<ChromatogramPeakFeature>();
            foreach (var spot in features) {
                objects.Add(spot);
                foreach (var dSpot in spot.DriftChromFeatures.OrEmptyIfNull()) {
                    objects.Add(dSpot);
                }
            }
            return objects;
        }

        public static List<AlignmentSpotProperty> GetAlignmentSpotPropertiesIntegratingRtAndDriftData(List<AlignmentSpotProperty> features) {
            var objects = new List<AlignmentSpotProperty>();
            foreach (var spot in features) {
                objects.Add(spot);
                foreach (var dSpot in spot.AlignmentDriftSpotFeatures.OrEmptyIfNull()) {
                    objects.Add(dSpot);
                }
            }
            return objects;
        }
    }
}
