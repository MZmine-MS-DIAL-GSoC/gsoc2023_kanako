﻿using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class RawDecSpectrumsViewModel : ViewModelBase
    {
        public RawDecSpectrumsViewModel(
            RawDecSpectrumsModel model,
            IObservable<IAxisManager<double>> horizontalAxisSource = null,
            IObservable<IAxisManager<double>> upperVerticalAxisSource = null,
            IObservable<IAxisManager<double>> lowerVerticalAxisSource = null) {

            this.model = model;

            if (upperVerticalAxisSource is null) {
                var upperVerticalAxis = this.model
                    .DecRefSpectrumModels
                    .UpperVerticalRangeSource
                    .ToReactiveAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent)
                    .AddTo(Disposables);
                var upperLogVerticalAxis = this.model
                    .DecRefSpectrumModels
                    .UpperVerticalRangeSource
                    .Select(range => (range.Minimum.Value, range.Maximum.Value))
                    .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d)
                    .AddTo(Disposables);

                var axis = new AxisItemModel(upperVerticalAxis, "Normal");
                UpperVerticalAxiss = new ObservableCollection<AxisItemModel>(new[]
                {
                    axis,
                    new AxisItemModel(upperLogVerticalAxis, "Log10"),
                });
                UpperVerticalAxis = new ReactivePropertySlim<AxisItemModel>(axis).AddTo(Disposables);
                upperVerticalAxisSource = UpperVerticalAxis.Select(item => item.AxisManager);
            }
            else {
                UpperVerticalAxiss = new ObservableCollection<AxisItemModel>();
                UpperVerticalAxis = new ReactivePropertySlim<AxisItemModel>().AddTo(Disposables);
            }

            RawRefSpectrumViewModels = new MsSpectrumViewModel(
                this.model.RawRefSpectrumModels,
                horizontalAxisSource,
                upperVerticalAxisSource,
                lowerVerticalAxisSource);

            DecRefSpectrumViewModels = new MsSpectrumViewModel(
                this.model.DecRefSpectrumModels,
                horizontalAxisSource,
                upperVerticalAxisSource,
                lowerVerticalAxisSource);
        }

        private readonly RawDecSpectrumsModel model;

        public MsSpectrumViewModel RawRefSpectrumViewModels { get; }

        public MsSpectrumViewModel DecRefSpectrumViewModels { get; }

        public ObservableCollection<AxisItemModel> UpperVerticalAxiss { get; }

        public ReactivePropertySlim<AxisItemModel> UpperVerticalAxis { get; }

    }

    public class AxisItemViewModel : BindableBase
    {
        public AxisItemViewModel(IAxisManager<double> axisManager, string label) {
            AxisManager = axisManager;
            Label = label;
        }

        public IAxisManager<double> AxisManager { get; }

        public string Label { get; }
    }
}
