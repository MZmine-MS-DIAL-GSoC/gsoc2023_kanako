﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Dims
{
    internal interface IDimsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
    }

    internal sealed class DimsAnalysisPeakTableModel : AnalysisPeakSpotTableModelBase, IDimsPeakSpotTableModel
    {
        public DimsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peaks, target, peakSpotNavigatorModel) {
            MassMin = peaks.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peaks.Select(s => s.Mass).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
    }

    internal sealed class DimsAlignmentSpotTableModel : AlignmentSpotTableModelBase, IDimsPeakSpotTableModel
    {
        public DimsAlignmentSpotTableModel(
            IReadOnlyList<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(spots, target, classBrush, classProperties, barItemsLoader, peakSpotNavigatorModel) {
            MassMin = spots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = spots.Select(s => s.Mass).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
    }
}
