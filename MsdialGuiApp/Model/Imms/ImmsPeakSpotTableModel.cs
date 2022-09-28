﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imms
{
    interface IImmsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
        double DriftMin { get; }
        double DriftMax { get; }
    }

    abstract class ImmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, IImmsPeakSpotTableModel where T : class
    {
        public ImmsPeakSpotTableModel(
            ObservableCollection<T> peakSpots,
            IReactiveProperty<T> target,
            double massMin, double massMax,
            double driftMin, double driftMax)
            : base(peakSpots, target) {

            MassMin = massMin;
            MassMax = massMax;

            DriftMin = driftMin;
            DriftMax = driftMax;
        }

        public double MassMin { get; }

        public double MassMax { get; }

        public double DriftMin { get; }

        public double DriftMax { get; }
    }

    sealed class ImmsAlignmentSpotTableModel : ImmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public ImmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target)
            : base(spots, target,
                  spots.Select(peak => peak.MassCenter).DefaultIfEmpty().Min(), spots.Select(peak => peak.MassCenter).DefaultIfEmpty().Max(),
                  spots.Select(peak => peak.TimesCenter).DefaultIfEmpty().Min(), spots.Select(peak => peak.TimesCenter).DefaultIfEmpty().Max()) {

        }
    }

    sealed class ImmsAnalysisPeakTableModel : ImmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public ImmsAnalysisPeakTableModel(ObservableCollection<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target)
            : base(peaks, target,
                  peaks.Select(peak => peak.Mass).DefaultIfEmpty().Min(), peaks.Select(peak => peak.Mass).DefaultIfEmpty().Max(),
                  peaks.DefaultIfEmpty().Min(peak => peak.ChromXValue) ?? 0d, peaks.DefaultIfEmpty().Max(peak => peak.ChromXValue) ?? 0d) {
        }
    }
}
