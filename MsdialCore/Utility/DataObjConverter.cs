﻿using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;

namespace CompMs.MsdialCore.Utility
{
    public class DataObjConverter
    {

        public static AlignmentChromPeakFeature ConvertToAlignmentChromPeakFeature(IMSScanProperty peakobj, MachineCategory category) {
            var result = new AlignmentChromPeakFeature();
            SetAlignmentChromPeakFeature(result, peakobj, category);
            return result;
        }
            
        public static void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature alignmentPeak, IMSScanProperty peakobj, MachineCategory category) {
            if (category == MachineCategory.GCMS) {
                var peak = (MSDecResult)peakobj;
                alignmentPeak.MasterPeakID = peak.ScanID;
                alignmentPeak.PeakID = peak.ScanID;
                alignmentPeak.SeekPointToDCLFile = peak.SeekPoint;
                alignmentPeak.MS1RawSpectrumID = peak.RawSpectrumID;
                alignmentPeak.MS1RawSpectrumIdTop = peak.RawSpectrumID;
                alignmentPeak.ChromXsTop = peak.ChromXs;
                alignmentPeak.ChromXsLeft = peak.ModelPeakChromatogram[0].ChromXs;
                alignmentPeak.ChromXsRight = peak.ModelPeakChromatogram[peak.ModelPeakChromatogram.Count - 1].ChromXs;
                alignmentPeak.PeakHeightTop = peak.ModelPeakHeight;
                alignmentPeak.PeakAreaAboveZero = peak.ModelPeakArea;
                alignmentPeak.Mass = peak.ModelPeakMz;
                alignmentPeak.IonMode = peak.IonMode;
                alignmentPeak.MSRawID2MspIDs = new Dictionary<int, List<int>>() { { peak.RawSpectrumID, peak.MspIDs } };
                alignmentPeak.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>() { { peak.RawSpectrumID, peak.MspBasedMatchResult } };
                alignmentPeak.PeakShape = new ChromatogramPeakShape()
                {
                    EstimatedNoise = peak.EstimatedNoise, SignalToNoise = peak.SignalNoiseRatio, AmplitudeScoreValue = peak.AmplitudeScore,
                    PeakPureValue = peak.ModelPeakPurity, IdealSlopeValue = peak.ModelPeakQuality
                };
            }
            else {
                var peak = (ChromatogramPeakFeature)peakobj;
                alignmentPeak.MasterPeakID = peak.MasterPeakID;
                alignmentPeak.PeakID = peak.PeakID;
                alignmentPeak.ParentPeakID = peak.ParentPeakID;
                alignmentPeak.SeekPointToDCLFile = peak.SeekPointToDCLFile;
                alignmentPeak.MS1RawSpectrumID = peak.ScanID;
                alignmentPeak.MS1RawSpectrumIDatAccumulatedMS1 = peak.MS1AccumulatedMs1RawSpectrumIdTop;
                alignmentPeak.MS2RawSpectrumID = peak.MS2RawSpectrumID;
                alignmentPeak.MS2RawSpectrumID2CE = peak.MS2RawSpectrumID2CE;
                alignmentPeak.ChromScanIdLeft = peak.ChromScanIdLeft;
                alignmentPeak.ChromScanIdRight = peak.ChromScanIdRight;
                alignmentPeak.ChromScanIdTop = peak.ChromScanIdTop;
                alignmentPeak.MS1RawSpectrumIdTop = peak.MS1RawSpectrumIdTop;
                alignmentPeak.MS1RawSpectrumIdLeft = peak.MS1RawSpectrumIdLeft;
                alignmentPeak.MS1RawSpectrumIdRight = peak.MS1RawSpectrumIdRight;
                alignmentPeak.MS1AccumulatedMs1RawSpectrumIdTop = peak.MS1AccumulatedMs1RawSpectrumIdTop;
                alignmentPeak.MS1AccumulatedMs1RawSpectrumIdLeft = peak.MS1AccumulatedMs1RawSpectrumIdLeft;
                alignmentPeak.MS1AccumulatedMs1RawSpectrumIdRight = peak.MS1AccumulatedMs1RawSpectrumIdRight;
                alignmentPeak.ChromXsLeft = peak.ChromXsLeft;
                alignmentPeak.ChromXsTop = peak.ChromXsTop;
                alignmentPeak.ChromXsRight = peak.ChromXsRight;
                alignmentPeak.PeakHeightLeft = peak.PeakHeightLeft;
                alignmentPeak.PeakHeightTop = peak.PeakHeightTop;
                alignmentPeak.PeakHeightRight = peak.PeakHeightRight;
                alignmentPeak.PeakAreaAboveZero = peak.PeakAreaAboveZero;
                alignmentPeak.PeakAreaAboveBaseline = peak.PeakAreaAboveBaseline;
                alignmentPeak.Mass = peak.Mass;
                alignmentPeak.IonMode = peak.IonMode;
                alignmentPeak.Name = peak.Name;
                alignmentPeak.Formula = peak.Formula;
                alignmentPeak.Ontology = peak.Ontology;
                alignmentPeak.SMILES = peak.SMILES;
                alignmentPeak.InChIKey = peak.InChIKey;
                alignmentPeak.CollisionCrossSection = peak.CollisionCrossSection;
                alignmentPeak.MSRawID2MspIDs = peak.MSRawID2MspIDs;
                alignmentPeak.TextDbIDs = peak.TextDbIDs;
                alignmentPeak.MSRawID2MspBasedMatchResult = peak.MSRawID2MspBasedMatchResult;
                alignmentPeak.TextDbBasedMatchResult = peak.TextDbBasedMatchResult;
                alignmentPeak.PeakCharacter = peak.PeakCharacter;
                alignmentPeak.PeakShape = peak.PeakShape;
            }
        }

        public static AlignmentSpotProperty ConvertFeatureToSpot(List<AlignmentChromPeakFeature> alignment) {
            var alignedPeaks = alignment.Where(align => align.PeakID >= 0).ToArray();
            if (alignedPeaks.Length == 0) {
                return new AlignmentSpotProperty();
            }
            var repId = GetRepresentativeFileID(alignment);
            var representative = repId >= 0 ? alignment[repId] : alignedPeaks.First();
            var chromXType = representative.ChromXsTop.MainType;
            var result = new AlignmentSpotProperty() {
                RepresentativeFileID = repId,

                AlignedPeakProperties = alignment,

                // PeakCharacter = representative.PeakCharacter, // TODO: need to change to deep copy
                PeakCharacter = new IonFeatureCharacter(),
                AdductType = new Common.DataObj.Property.AdductIon(),
                IonMode = representative.IonMode,

                Name = representative.Name,
                // Formula = representative.Formula, // TODO: need to change to deep copy ?
                Formula = new Common.DataObj.Property.Formula(),
                Ontology = representative.Ontology,
                SMILES = representative.SMILES,
                InChIKey = representative.InChIKey,

                CollisionCrossSection = representative.CollisionCrossSection,

                MSRawID2MspIDs = representative.MSRawID2MspIDs, // TODO: need to change to deep copy ?
                TextDbIDs = new List<int>(representative.TextDbIDs),
                MSRawID2MspBasedMatchResult = representative.MSRawID2MspBasedMatchResult, // TODO: need to change to deep copy ?
                TextDbBasedMatchResult = representative.TextDbBasedMatchResult, // TODO: need to change to deep copy ?

                HeightAverage = (float)alignedPeaks.Average(peak => peak.PeakHeightTop),
                HeightMax = (float)alignedPeaks.Max(peak => peak.PeakHeightTop),
                HeightMin = (float)alignedPeaks.Min(peak => peak.PeakHeightTop),
                PeakWidthAverage = (float)alignedPeaks.Average(peak => peak.PeakWidth(chromXType)),

                SignalToNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.SignalToNoise),

                EstimatedNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.EstimatedNoise),

                TimesMin = alignedPeaks.Argmin(peak => peak.ChromXsTop.Value).ChromXsTop, // TODO: need to change to deep copy ?
                TimesMax = alignedPeaks.Argmax(peak => peak.ChromXsTop.Value).ChromXsTop, // TODO: need to change to deep copy ?

                MassMin = (float)alignedPeaks.Min(peak => peak.Mass),
                MassMax = (float)alignedPeaks.Max(peak => peak.Mass),

                FillParcentage = (float)alignedPeaks.Length / alignment.Count,
                MonoIsotopicPercentage = alignedPeaks.Count(peak => peak.PeakCharacter.IsotopeWeightNumber == 0) / (float)alignedPeaks.Length,
            };
            result.TimesCenter = new ChromXs() {
                MainType = chromXType,
                RT = new RetentionTime(alignedPeaks.Average(peak => peak.ChromXsTop.RT.Value), representative.ChromXsTop.RT.Unit),
                RI = new RetentionIndex(alignedPeaks.Average(peak => peak.ChromXsTop.RI.Value), representative.ChromXsTop.RI.Unit),
                Mz = new MzValue(alignedPeaks.Average(peak => peak.ChromXsTop.Mz.Value), representative.ChromXsTop.Mz.Unit),
                Drift = new DriftTime(alignedPeaks.Average(peak => peak.ChromXsTop.Drift.Value), representative.ChromXsTop.Drift.Unit),
            };
            result.MassCenter = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.Mass)).Mass;
            return result;
        }

        public static int GetRepresentativeFileID(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            if (alignment.Count == 0) return -1;
            var alignmentWithMSMS = alignment.Where(align => !align.MS2RawSpectrumID2CE.IsEmptyOrNull()).ToArray();
            if (alignmentWithMSMS.Length != 0) {
                return alignmentWithMSMS.Argmax(align =>
                    // UNDONE: MSRawID2MspBasedMatchResult has no element.
                    (align.MSRawID2MspBasedMatchResult?.Values?.DefaultIfEmpty().Max(val => val?.TotalScore), align.PeakHeightTop)
                    ).FileID;
            }
            return alignment.Argmax(align => (align.TextDbBasedMatchResult?.TotalScore, align.PeakHeightTop)).FileID;
        }
    }
}