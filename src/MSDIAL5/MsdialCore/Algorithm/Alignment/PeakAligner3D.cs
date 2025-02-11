﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class PeakAligner3D : PeakAligner
    {
        protected GapFiller3D Filler3d { get; }
        protected IDataProviderFactory<AnalysisFileBean> AccumulateDataProviderFactory { get; }

        public PeakAligner3D(AlignmentProcessFactory factory, IDataProviderFactory<AnalysisFileBean> rawDataProviderFactory, IDataProviderFactory<AnalysisFileBean> accumulatedDataProviderFactory) : base(factory, null) {
            Filler3d = factory.CreateGapFiller() as GapFiller3D;
            ProviderFactory = rawDataProviderFactory;
            AccumulateDataProviderFactory = accumulatedDataProviderFactory;
        }

        protected override string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks, List<AlignmentSpotProperty> spots,
            string tempFile, ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            DataAccess.GetAllSpectraWithAccumulatedMS1(analysisFile.AnalysisFilePath, out var spectra, out var accumulated);
            var rawProvider = ProviderFactory.Create(analysisFile);
            var accProvider = AccumulateDataProviderFactory.Create(analysisFile);
            var rawSpectras = new Dictionary<IonMode, Lazy<RawSpectra>>
            {
                { IonMode.Positive, new Lazy<RawSpectra>(() => new RawSpectra(accProvider, IonMode.Positive, analysisFile.AcquisitionType)) },
                { IonMode.Negative, new Lazy<RawSpectra>(() => new RawSpectra(accProvider, IonMode.Negative, analysisFile.AcquisitionType)) },
            };
            var rtRange = new ChromatogramRange(double.MinValue, double.MaxValue, ChromXType.RT, ChromXUnit.Min);
            var dRawSpectras = new Dictionary<IonMode, Lazy<RawSpectra>>
            {
                { IonMode.Positive, new Lazy<RawSpectra>(() => new RawSpectra(rawProvider, IonMode.Positive, analysisFile.AcquisitionType)) },
                { IonMode.Negative, new Lazy<RawSpectra>(() => new RawSpectra(rawProvider, IonMode.Negative, analysisFile.AcquisitionType)) },
            };
            var dtRange = new ChromatogramRange(double.MinValue, double.MaxValue, ChromXType.Drift, ChromXUnit.Msec);

            var peakInfos = new List<ChromatogramPeakInfo>();
            foreach ((var peak, var spot) in peaks.Zip(spots)) {
                var rawSpectra = rawSpectras[peak.IonMode].Value;
                if (spot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                    Filler3d.GapFillFirst(rawSpectra, spot, analysisFile.AnalysisFileId);
                }
                if (DataObjConverter.GetRepresentativeFileID(spot.AlignedPeakProperties.Where(p => p.PeakID >= 0).ToArray()) == analysisFile.AnalysisFileId) {
                    var index = accumulated.LowerBound(peak.MS1AccumulatedMs1RawSpectrumIdTop, (s, id) => s.Index.CompareTo(id));
                    if (index < 0 || accumulated == null || index >= accumulated.Count) {
                        spot.IsotopicPeaks = new List<IsotopicPeak>(0);
                    }
                    else {
                        spot.IsotopicPeaks = DataAccess.GetFineIsotopicPeaks(peak, accumulated[index], Param.CentroidMs1Tolerance);
                    }
                }

                // UNDONE: retrieve spectrum data
                var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                var chromatogram = rawSpectra.GetMs1ExtractedChromatogram(peak.Mass, (detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f, rtRange);
                var peakInfo = new ChromatogramPeakInfo(
                    peak.FileID, chromatogram.Peaks,
                    (float)peak.ChromXsTop.Value,
                    (float)peak.ChromXsLeft.Value,
                    (float)peak.ChromXsRight.Value
                    );
                peakInfos.Add(peakInfo);

                foreach (var dspot in spot.AlignmentDriftSpotFeatures) {
                    var dpeak = dspot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId);
                    if (dpeak.MasterPeakID < 0) {
                        Filler3d.GapFillSecond(spectra, dspot, spot, analysisFile.AnalysisFileId);
                    }

                    // UNDONE: retrieve spectrum data
                    var ddetected = dspot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                    var dRawSpectra = dRawSpectras[peak.IonMode].Value;
                    var dChromatogram = dRawSpectra.GetDriftChromatogramByScanRtMz(dpeak.MS1RawSpectrumIdTop, (float)peak.ChromXsTop.RT.Value, (float)Filler3d.AxTolFirst, (float)peak.Mass, (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f);
                    var dpeakInfo = new ChromatogramPeakInfo(
                        dpeak.FileID, dChromatogram.Peaks,
                        (float)dpeak.ChromXsTop.Value,
                        (float)dpeak.ChromXsLeft.Value,
                        (float)dpeak.ChromXsRight.Value
                    );
                    peakInfos.Add(dpeakInfo);
                }
            }
            serializer?.SerializeAllToFile(tempFile, peakInfos);
            return tempFile;
        }
    }
}
