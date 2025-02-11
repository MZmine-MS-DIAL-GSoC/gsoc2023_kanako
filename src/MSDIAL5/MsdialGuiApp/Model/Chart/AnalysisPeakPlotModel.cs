﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AnalysisPeakPlotModel : DisposableModelBase
    {
        private readonly PeakLinkModel _peakLinkModel;

        public AnalysisPeakPlotModel(
            ObservableCollection<ChromatogramPeakFeatureModel> spots,
            Func<ChromatogramPeakFeatureModel, double> horizontalSelector,
            Func<ChromatogramPeakFeatureModel, double> verticalSelector,
            IReactiveProperty<ChromatogramPeakFeatureModel> targetSource,
            IObservable<string> labelSource,
            BrushMapData<ChromatogramPeakFeatureModel> selectedBrush,
            IList<BrushMapData<ChromatogramPeakFeatureModel>> brushes,
            PeakLinkModel peakLinkModel,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> verticalAxis = null) {
            if (brushes is null) {
                throw new ArgumentNullException(nameof(brushes));
            }

            Spots = spots ?? throw new ArgumentNullException(nameof(spots));
            LabelSource = labelSource ?? throw new ArgumentNullException(nameof(labelSource));
            SelectedBrush = selectedBrush ?? throw new ArgumentNullException(nameof(selectedBrush));
            Brushes = new ReadOnlyCollection<BrushMapData<ChromatogramPeakFeatureModel>>(brushes);
            TargetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;

            HorizontalAxis = horizontalAxis ?? Spots.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Throttle(TimeSpan.FromSeconds(.01d))
                .Select(_ => Spots.Any() ? new Range(Spots.Min(horizontalSelector), Spots.Max(horizontalSelector)) : new Range(0, 1))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            VerticalAxis = verticalAxis ?? Spots.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Throttle(TimeSpan.FromSeconds(.01d))
                .Select(_ => Spots.Any() ? new Range(Spots.Min(verticalSelector), Spots.Max(verticalSelector)) : new Range(0, 1))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            _peakLinkModel = peakLinkModel;
        }

        public ObservableCollection<ChromatogramPeakFeatureModel> Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IReactiveProperty<ChromatogramPeakFeatureModel> TargetSource { get; }

        public string GraphTitle {
            get => _graphTitle;
            set => SetProperty(ref _graphTitle, value);
        }
        private string _graphTitle;

        public string HorizontalTitle {
            get => _horizontalTitle;
            set => SetProperty(ref _horizontalTitle, value);
        }
        private string _horizontalTitle;

        public string VerticalTitle {
            get => _verticalTitle;
            set => SetProperty(ref _verticalTitle, value);
        }
        private string _verticalTitle;

        public string HorizontalProperty {
            get => _horizontalProperty;
            set => SetProperty(ref _horizontalProperty, value);
        }
        private string _horizontalProperty;

        public string VerticalProperty {
            get => _verticalProperty;
            set => SetProperty(ref _verticalProperty, value);
        }
        private string _verticalProperty;

        public IObservable<string> LabelSource { get; }
        public BrushMapData<ChromatogramPeakFeatureModel> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<ChromatogramPeakFeatureModel> _selectedBrush;

        public ReadOnlyCollection<BrushMapData<ChromatogramPeakFeatureModel>> Brushes { get; }

        public ObservableCollection<SpotLinker> Links => _peakLinkModel.Links;
        public ObservableCollection<SpotAnnotator> Annotations => _peakLinkModel.Annotations;
        public IBrushMapper<ISpotLinker> LinkerBrush => _peakLinkModel.LinkerBrush;
        public IBrushMapper<SpotAnnotator> SpotLabelBrush => _peakLinkModel.SpotLabelBrush;
    }
}
