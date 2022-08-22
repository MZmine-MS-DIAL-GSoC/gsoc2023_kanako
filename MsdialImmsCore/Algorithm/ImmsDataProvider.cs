﻿using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class ImmsRepresentativeDataProvider : BaseDataProvider
    {
        public ImmsRepresentativeDataProvider(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd)
            : base(SelectRepresentative(FilterByScanTime(spectrums, timeBegin, timeEnd))) {
            
        }

        public ImmsRepresentativeDataProvider(IEnumerable<RawSpectrum> spectrums)
            : base(SelectRepresentative(spectrums)) {

        }

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, timeBegin, timeEnd) {

        }

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj)
            : this(rawObj.SpectrumList) {

        }

        public ImmsRepresentativeDataProvider(AnalysisFileBean file, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, isGuiProcess, retry).SpectrumList, timeBegin, timeEnd) {

        }

        public ImmsRepresentativeDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, isGuiProcess, retry).SpectrumList) {

        }

        private static List<RawSpectrum> SelectRepresentative(IEnumerable<RawSpectrum> rawSpectrums) {
            var ms1Spectrums = rawSpectrums
                .Where(spectrum => spectrum.MsLevel == 1)
                .GroupBy(spectrum => spectrum.ScanNumber)
                .Argmax(spectrums => spectrums.Sum(spectrum => spectrum.TotalIonCurrent));
            var result = ms1Spectrums.Concat(rawSpectrums.Where(spec => spec.MsLevel != 1))
                .Select(spec => spec.ShallowCopy())
                .OrderBy(spectrum => spectrum.DriftTime).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }
            return result;
        }
    }

    public class ImmsAverageDataProvider : BaseDataProvider
    {
        public ImmsAverageDataProvider(IEnumerable<RawSpectrum> spectrums, double massTolerance, double driftTolerance, double timeBegin, double timeEnd)
            : base(AccumulateSpectrum(FilterByScanTime(spectrums, timeBegin, timeEnd).ToList(), massTolerance, driftTolerance)) {

        }
        
        public ImmsAverageDataProvider(IEnumerable<RawSpectrum> spectrums, double massTolerance, double driftTolerance)
            : base(AccumulateSpectrum(spectrums.ToList(), massTolerance, driftTolerance)) {

        }

        public ImmsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double driftTolerance, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, massTolerance, driftTolerance, timeBegin, timeEnd) {

        }

        public ImmsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double driftTolerance)
            : this(rawObj.SpectrumList, massTolerance, driftTolerance) {

        }

        public ImmsAverageDataProvider(RawMeasurement rawObj)
            :this(rawObj, 0.001, 0.002) { }

        public ImmsAverageDataProvider(AnalysisFileBean file, double massTolerance, double driftTolerance, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, isGuiProcess, retry), massTolerance, driftTolerance, timeBegin, timeEnd) {

        }

        public ImmsAverageDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, isGuiProcess, retry)) {

        }

        public ImmsAverageDataProvider(AnalysisFileBean file, double massTolerance, double driftTolerance, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, isGuiProcess, retry), massTolerance, driftTolerance) {

        }

        private static List<RawSpectrum> AccumulateSpectrum(List<RawSpectrum> rawSpectrums, double massTolerance, double driftTolerance) {
            var ms1Spectrum = rawSpectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var numOfMeasurement = ms1Spectrum.Select(spectrum => spectrum.ScanStartTime).Distinct().Count();

            var groups = rawSpectrums.GroupBy(spectrum => Math.Ceiling(spectrum.DriftTime / driftTolerance));
            var result = groups
                .OrderBy(kvp => kvp.Key)
                .SelectMany(group => AccumulateRawSpectrums(group.ToList(), massTolerance, numOfMeasurement))
                .ToList();
            for (var i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }

            return result;
        }

        private static IEnumerable<RawSpectrum> AccumulateRawSpectrums(IReadOnlyCollection<RawSpectrum> spectrums, double massTolerance, int numOfMeasurement) {
            var ms1Spectrums = spectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var groups = ms1Spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => (int)(peak.Mz / massTolerance));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                //var accIntensity = peaks.Sum(peak => peak.Intensity) / numOfMeasurement;
                var accIntensity = peaks.Sum(peak => peak.Intensity);
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = new double[] { basepeak.Mz, accIntensity, basepeak.Intensity };
            }
            var result = ms1Spectrums.First().ShallowCopy();
            SpectrumParser.setSpectrumProperties(result, massBins);
            return new[] { result }.Concat(
                spectrums.Where(spectrum => spectrum.MsLevel != 1)
                .Select(spec => spec.ShallowCopy())
                .OrderBy(spectrum => spectrum.Index));
        }
    }

    public class ImmsRepresentativeDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public ImmsRepresentativeDataProviderFactory(
            double timeBegin, double timeEnd,
            int retry = 5, bool isGuiProcess = false) {

            this.timeBegin = timeBegin;
            this.timeEnd = timeEnd;
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        private readonly bool isGuiProcess;
        private readonly int retry;
        private readonly double timeBegin;
        private readonly double timeEnd;

        public IDataProvider Create(AnalysisFileBean file) {
            return new ImmsRepresentativeDataProvider(file, timeBegin, timeEnd, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new ImmsRepresentativeDataProvider(rawMeasurement, timeBegin, timeEnd);
        }
    }

    public class ImmsAverageDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public ImmsAverageDataProviderFactory(
            double massTolerance, double driftTolerance,
            int retry = 5, bool isGuiProcess = false) {

            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
            this.massTolerance = massTolerance;
            this.driftTolerance = driftTolerance;
            this.timeBegin = double.MinValue;
            this.timeEnd = double.MaxValue;
        }

        public ImmsAverageDataProviderFactory(
            double massTolerance, double driftTolerance,
            double timeBegin, double timeEnd,
            int retry = 5, bool isGuiProcess = false) {

            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
            this.massTolerance = massTolerance;
            this.driftTolerance = driftTolerance;
            this.timeBegin = timeBegin;
            this.timeEnd = timeEnd;
        }

        private readonly bool isGuiProcess;
        private readonly int retry;
        private readonly double massTolerance, driftTolerance;
        private readonly double timeBegin;
        private readonly double timeEnd;

        public IDataProvider Create(AnalysisFileBean file) {
            return new ImmsAverageDataProvider(file, massTolerance, driftTolerance, timeBegin, timeEnd, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new ImmsAverageDataProvider(rawMeasurement, massTolerance, driftTolerance, timeBegin, timeEnd);
        }
    }
}
