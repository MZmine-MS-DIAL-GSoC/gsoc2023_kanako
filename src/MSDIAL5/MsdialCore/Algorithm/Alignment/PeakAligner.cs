﻿using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class PeakAligner {
        protected DataAccessor Accessor { get; }
        protected IPeakJoiner Joiner { get; }
        protected GapFiller Filler { get; }
        protected IAlignmentRefiner Refiner { get; }
        protected ParameterBase Param { get; }
        protected List<MoleculeMsReference> MspDB { get; } = new List<MoleculeMsReference>();
        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; set; }
        private Action<int> reportAction { get; set; }

        public PeakAligner(AlignmentProcessFactory factory, Action<int> report) {
            Accessor = factory.CreateDataAccessor();
            Joiner = factory.CreatePeakJoiner();
            Filler = factory.CreateGapFiller();
            Refiner = factory.CreateAlignmentRefiner();
            Param = factory.Parameter;
            reportAction = report;
                
        }

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var spots = Joiner.Join(analysisFiles, Param.AlignmentReferenceFileID, Accessor);
            spots = FilterAlignments(spots, Param);

            var chromPeakInfoSerializer = spotSerializer == null ? null : ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
            var files = analysisFiles.Select(_ => Path.GetTempFileName()).ToArray();
            try {
                var id2idx = CollectPeakSpots(analysisFiles, spots, chromPeakInfoSerializer, files);
                (var refined, var ids) = Refiner.Refine(spots);

                var container = PackingSpots(refined);

                if (Param.TrackingIsotopeLabels) {
                    IsotopeTracking.SetIsotopeTrackingID(container, Param, MspDB, null);
                }

                if (chromPeakInfoSerializer != null)
                    SerializeSpotInfo(
                        FlattenSpots(refined).ToList(),
                        ids.Select(id => id2idx[id]).ToArray(),
                        files,
                        alignmentFile,
                        spotSerializer,
                        chromPeakInfoSerializer);

                return container;
            }
            finally {
                foreach (var f in files) {
                    if (File.Exists(f)) {
                        File.Delete(f);
                    }
                }
            }
        }

        private static List<AlignmentSpotProperty> FilterAlignments(List<AlignmentSpotProperty> spots, ParameterBase param) {
            var result = spots.Where(spot => spot.AlignedPeakProperties.Any(peak => peak.MasterPeakID >= 0));

            var filter = new CompositeFilter();

            filter.Filters.Add(new PeakCountFilter(param.PeakCountFilter / 100 * param.FileID_AnalysisFileType.Count));

            if (param.QcAtLeastFilter)
                filter.Filters.Add(new QcFilter(param.FileID_AnalysisFileType));

            filter.Filters.Add(new DetectedNumberFilter(param.FileID_ClassName, param.NPercentDetectedInOneGroup / 100));

            return filter.Filter(result).ToList();
        }

        private Dictionary<int, int> CollectPeakSpots(
            IReadOnlyList<AnalysisFileBean> analysisFiles,
            List<AlignmentSpotProperty> spots,
            ChromatogramSerializer<ChromatogramPeakInfo> chromPeakInfoSerializer,
            string[] tempFiles) {

            // from 40 to 80
            var counter = 0;
            foreach (var (analysisFile, file_) in analysisFiles.Zip(tempFiles)) {
                var peaks = new List<AlignmentChromPeakFeature>(spots.Count);
                foreach (var spot in spots)
                    peaks.Add(spot.AlignedPeakProperties.FirstOrDefault(peak => peak.FileID == analysisFile.AnalysisFileId));
                var file = CollectAlignmentPeaks(analysisFile, peaks, spots, file_, chromPeakInfoSerializer);

                counter++;
                ReportProgress.Show(40.0, 40.0, counter, analysisFiles.Count - 1, reportAction);
            }
            foreach (var spot in spots) {
                SetRepresentativeProperties(spot);
            }
            var id2idx = FlattenSpots(spots)
                .Select((spot, idx) => Tuple.Create(spot, idx))
                .ToDictionary(pair => pair.Item1.MasterAlignmentID, pair => pair.Item2);

            return id2idx;
        }

        protected virtual string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<AlignmentSpotProperty> spots,
            string tempFile,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var provider = ProviderFactory?.Create(analysisFile);
            IReadOnlyList<RawSpectrum> spectra = provider?.LoadMs1Spectrums();
            if (spectra == null) {
                using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, false, false, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                    spectra = rawDataAccess.GetMeasurement()?.SpectrumList;
                }
            }
            var ms1Spectra = new Ms1Spectra(spectra, Param.IonMode);
            var rawSpectra = new RawSpectra(spectra, Param.IonMode, analysisFile.AcquisitionType);
            var peakInfos = peaks.Zip(spots)
                .AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(Param.NumThreads)
                .Select(peakAndSpot => {
                    (var peak, var spot) = peakAndSpot;

                    if (spot.AlignedPeakProperties.First(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                        Filler.GapFill(ms1Spectra, rawSpectra, spectra, spot, analysisFile.AnalysisFileId);
                    }
                    if (DataObjConverter.GetRepresentativeFileID(spot.AlignedPeakProperties.Where(p => p.PeakID >= 0).ToArray()) == analysisFile.AnalysisFileId) {
                        var index = spectra.LowerBound(peak.MS1RawSpectrumIdTop, (s, id) => s.Index.CompareTo(id));
                        if (index < 0 || spectra == null || index >= spectra.Count) {
                            spot.IsotopicPeaks = new List<IsotopicPeak>(0);
                        }
                        else {
                            spot.IsotopicPeaks = DataAccess.GetFineIsotopicPeaks(peak, spectra[index], Param.CentroidMs1Tolerance);
                        }
                    }

                    // UNDONE: retrieve spectrum data
                    return Accessor.AccumulateChromatogram(peak, spot, ms1Spectra, spectra, Param.CentroidMs1Tolerance);
                }).ToList();

            serializer?.SerializeAllToFile(tempFile, peakInfos);
            return tempFile;
        }

        private AlignmentResultContainer PackingSpots(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.IsEmptyOrNull()) {
                return new AlignmentResultContainer
                {
                    Ionization = Param.Ionization,
                    AlignmentResultFileID = -1,
                    TotalAlignmentSpotCount = 0,
                    AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots),
                };
            }

            var minInt = (double)alignmentSpots.Min(spot => spot.HeightMin);
            var maxInt = (double)alignmentSpots.Max(spot => spot.HeightMax);

            maxInt = maxInt > 1 ? Math.Log(maxInt, 2) : 1;
            minInt = minInt > 1 ? Math.Log(minInt, 2) : 0;

            foreach (var spot in FlattenSpots(alignmentSpots)) {
                var relativeValue = (float)((Math.Log(spot.HeightMax, 2) - minInt) / (maxInt - minInt));
                spot.RelativeAmplitudeValue = Math.Min(1, Math.Max(0, relativeValue));
            }

            var spots = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = Param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = spots.Count,
                AlignmentSpotProperties = spots,
            };
        }

        private void SetRepresentativeProperties(AlignmentSpotProperty spot) {
            foreach (var child in spot.AlignmentDriftSpotFeatures)
                SetRepresentativeProperties(child);
            DataObjConverter.SetRepresentativeProperty(spot);
        }

        private void SerializeSpotInfo(
            IReadOnlyCollection<AlignmentSpotProperty> spots,
            IReadOnlyList<int> ids,
            IEnumerable<string> files,
            AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
            ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
            var pss = files.Select(file => peakSerializer.DeserializeEachFromFile(file, ids)).ToList();
            var qss = pss.Sequence();

            Debug.WriteLine("Serialize start.");
            using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
                spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
            }
            Debug.WriteLine("Serialize finish.");

            pss.ForEach(ps => ((IDisposable)ps).Dispose());
        }

        private IEnumerable<AlignmentSpotProperty> FlattenSpots(IEnumerable<AlignmentSpotProperty> spots) {
            return spots.SelectMany(spot => FlattenSpots(spot.AlignmentDriftSpotFeatures.OrEmptyIfNull()).Prepend(spot));
        }
    }
}
