﻿using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class AnnotationProcess
    {

        public double InitialProgress { get; set; } = 60.0;
        public double ProgressMax { get; set; } = 30.0;

        public AnnotationProcess(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }

        public void MainProcess(
            List<RawSpectrum> spectrumList, 
            List<ChromatogramPeakFeature> chromPeakFeatures,
            List<MSDecResult> msdecResults,
            List<MoleculeMsReference> mspDB,
            List<MoleculeMsReference> textDB,
            MsdialImmsParameter paramter,
            Action<int> reportAction) {

        }

        public void MainProcess(
            List<RawSpectrum> spectrumList, 
            List<ChromatogramPeakFeature> chromPeakFeatures,
            List<MSDecResult> msdecResults,
            List<MoleculeMsReference> mspDB,
            List<MoleculeMsReference> textDB,
            MsdialImmsParameter paramter,
            Action<int> reportAction,
            int numThread, System.Threading.CancellationToken token) {

        }

        public void Run(
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IAnnotator mspAnnotator,
            IAnnotator textDBAnnotator,
            MsdialImmsParameter parameter,
            Action<int> reportAction) {

            if (chromPeakFeatures.Count != msdecResults.Count)
                throw new ArgumentException("Number of ChromatogramPeakFeature and MSDecResult are different.");

            if (mspAnnotator == null && textDBAnnotator == null) {
                reportAction?.Invoke((int)ProgressMax);
                return;
            }

            var spectrumList = provider.LoadMs1Spectrums();
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                var msdecResult = msdecResults[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    ImmsMatchMethod(chromPeakFeature, msdecResult, spectrumList[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum, mspAnnotator, textDBAnnotator, parameter);
                }
                ReportProgress.Show(InitialProgress, ProgressMax, i, chromPeakFeatures.Count, reportAction);
            }
        }

        public void Run(
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IAnnotator mspAnnotator,
            IAnnotator textDBAnnotator,
            MsdialImmsParameter parameter,
            Action<int> reportAction,
            int numThreads, System.Threading.CancellationToken token) {

            if (chromPeakFeatures.Count != msdecResults.Count)
                throw new ArgumentException("Number of ChromatogramPeakFeature and MSDecResult are different.");

            if (mspAnnotator == null && textDBAnnotator == null) {
                reportAction?.Invoke((int)ProgressMax);
                return;
            }

            var spectrumList = provider.LoadMs1Spectrums();
            Enumerable.Range(0, chromPeakFeatures.Count)
                .AsParallel()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .ForAll(i => {
                    var chromPeakFeature = chromPeakFeatures[i];
                    var msdecResult = msdecResults[i];
                    if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                        ImmsMatchMethod(chromPeakFeature, msdecResult, spectrumList[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum, mspAnnotator, textDBAnnotator, parameter);
                    }
                    ReportProgress.Show(InitialProgress, ProgressMax, i, chromPeakFeatures.Count, reportAction);
                });
        }

        private static void ImmsMatchMethod(
            ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult,
            IReadOnlyList<RawPeakElement> spectrum,
            IAnnotator mspAnnotator,
            IAnnotator textDBAnnotator,
            MsdialImmsParameter parameter) {

            var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)chromPeakFeature.Mass, parameter.CentroidMs1Tolerance);

            SetMspAnnotationResult(chromPeakFeature, msdecResult, isotopes, mspAnnotator, parameter.MspSearchParam);
            SetTextDBAnnotationResult(chromPeakFeature, msdecResult, isotopes, textDBAnnotator, parameter.TextDbSearchParam);
        }

        private static void SetMspAnnotationResult(
            ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult, List<IsotopicPeak> isotopes,
            IAnnotator mspAnnotator, MsRefSearchParameterBase mspSearchParameter) {

            if (mspAnnotator == null)
                return;

            var results = mspAnnotator.FindCandidates(msdecResult, chromPeakFeature, isotopes, mspSearchParameter)
                .Where(candidate => candidate.IsPrecursorMzMatch || candidate.IsSpectrumMatch)
                .Where(candidate => !string.IsNullOrEmpty(candidate.Name))
                .ToList();
            chromPeakFeature.MSRawID2MspIDs[msdecResult.RawSpectrumID] = results.Select(result => result.LibraryIDWhenOrdered).ToList();
            if (results.Count > 0) {
                var best = results.Argmax(result => result.TotalScore);
                chromPeakFeature.MSRawID2MspBasedMatchResult[msdecResult.RawSpectrumID] = best;
                DataAccess.SetMoleculeMsProperty(chromPeakFeature, mspAnnotator.Refer(best), best);
            }
        }

        private static void SetTextDBAnnotationResult(
            ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult, List<IsotopicPeak> isotopes,
            IAnnotator textDBAnnotator, MsRefSearchParameterBase textDBSearchParameter) {

            if (textDBAnnotator == null)
                return;

            var results = textDBAnnotator.FindCandidates(msdecResult, chromPeakFeature, isotopes, textDBSearchParameter)
                .Where(candidate => candidate.IsPrecursorMzMatch)
                .Where(candidate => !string.IsNullOrEmpty(candidate.Name))
                .ToList();
            chromPeakFeature.TextDbIDs.AddRange(results.Select(result => result.LibraryIDWhenOrdered));
            if (results.Count > 0) {
                var best = results.Argmax(result => result.TotalScore);
                if (chromPeakFeature.TextDbBasedMatchResult == null || chromPeakFeature.TextDbBasedMatchResult.TotalScore < best.TotalScore) {
                    chromPeakFeature.TextDbBasedMatchResult = best;
                    DataAccess.SetTextDBMoleculeMsProperty(chromPeakFeature, textDBAnnotator.Refer(best), best);
                }
            }
        }
    }
}