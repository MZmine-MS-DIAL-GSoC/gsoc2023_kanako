﻿using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAnalysisModel : AnalysisModelBase {
        public ImmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            DataBaseMapper mapper,
            ParameterBase parameter,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotatorContainers)
            : base(analysisFile) {

            this.provider = provider;
            DatBaseMapper = mapper;
            AnnotatorContainers = annotatorContainers;
            this.parameter = parameter as MsdialImmsParameter;

            FileName = analysisFile.AnalysisFileName;

            AmplitudeOrderMin = Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0;
            AmplitudeOrderMax = Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0;

            var labelsource = this.ObserveProperty(m => m.DisplayLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelsource)
            {
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            };
            Target.Select(
                t => t is null
                    ? string.Empty
                    : $"Spot ID: {t.InnerModel.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.InnerModel.Mass:N5}")
                .Subscribe(title => PlotModel.GraphTitle = title);

            EicLoader = new EicLoader(provider, parameter, ChromXType.Drift, ChromXUnit.Msec, this.parameter.DriftTimeBegin, this.parameter.DriftTimeEnd);
            EicModel = new EicModel(Target, EicLoader)
            {
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            };
            Target.CombineLatest(
                EicModel.MaxIntensitySource,
                (t, i) => t is null
                    ? string.Empty
                    : $"EIC chromatogram of {t.Mass:N4} tolerance [Da]: {this.parameter.CentroidMs1Tolerance:F} Max intensity: {i:F0}")
                .Subscribe(title => EicModel.GraphTitle = title);

            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                new MsRawSpectrumLoader(provider, parameter),
                new MsDecSpectrumLoader(decLoader, Ms1Peaks),
                new MsRefSpectrumLoader(mapper),
                peak => peak.Mass,
                peak => peak.Intensity)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative aubndance",
                HorizontaProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            };

            SurveyScanModel = new SurveyScanModel(
                Target.SelectMany(t =>
                    Observable.DeferAsync(async token => {
                        var result = await LoadMs1SpectrumAsync(t, token);
                        return Observable.Return(result);
                    })),
                spec => spec.Mass,
                spec => spec.Intensity
            ).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            PeakTableModel = new ImmsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax, ChromMin, ChromMax);

            CanSearchCompound = new[]
            {
                Target.Select(t => t is null || t.InnerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    Brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181));
                    break;
                case TargetOmics.Metabolomics:
                    Brush = new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true);
                    break;
            }
            Target.Subscribe(t => OnTargetChanged(t));
        }

        public MsdialImmsParameter parameter { get; }
        private readonly IDataProvider provider;

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }

        public RawDecSpectrumsModel Ms2SpectrumModel { get; }

        public SurveyScanModel SurveyScanModel { get; }

        public ImmsAnalysisPeakTableModel PeakTableModel { get; }

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }
        private string rawSplashKey = string.Empty;

        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }

        public EicLoader EicLoader { get; }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusDt {
            get => focusDt;
            set => SetProperty(ref focusDt, value);
        }
        private double focusDt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public double ChromMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.ChromXValue) ?? 0d;
        public double ChromMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.ChromXValue) ?? 0d;
        public double MassMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.Mass) ?? 0d;
        public double MassMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.Mass) ?? 0d;
        public double IntensityMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.Intensity) ?? 0d;
        public double IntensityMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.Intensity) ?? 0d;

        public DataBaseMapper DatBaseMapper { get; }
        public IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> AnnotatorContainers { get; }

        void OnTargetChanged(ChromatogramPeakFeatureModel target) {
            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusDt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }
        }

        async Task<List<SpectrumPeakWrapper>> LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms1Spectrum = new List<SpectrumPeakWrapper>();

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS1RawSpectrumIdTop < 0) {
                        return;
                    }
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMs1Spectrums()[target.MS1RawSpectrumIdTop], parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                    token.ThrowIfCancellationRequested();
                    ms1Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                }, token);
            }
            return ms1Spectrum;
        }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public ImmsCompoundSearchModel<ChromatogramPeakFeature> CreateCompoundSearchModel() {
            if (Target.Value?.InnerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new ImmsCompoundSearchModel<ChromatogramPeakFeature>(
                AnalysisFile,
                Target.Value.InnerModel,
                MsdecResult.Value,
                null,
                AnnotatorContainers);
        }
    }
}
