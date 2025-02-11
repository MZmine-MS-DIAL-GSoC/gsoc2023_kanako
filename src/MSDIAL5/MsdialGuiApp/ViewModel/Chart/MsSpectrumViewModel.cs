﻿using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public sealed class MsSpectrumViewModel : ViewModelBase
    {
        private readonly MsSpectrumModel _model;

        public MsSpectrumViewModel(
            MsSpectrumModel model,
            IObservable<IAxisManager<double>> horizontalAxisSource = null,
            IObservable<IAxisManager<double>> upperVerticalAxisSource = null,
            IObservable<IAxisManager<double>> lowerVerticalAxisSource = null,
            Action focusAction = null,
            IObservable<bool> isFocused = null) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            SpectrumLoaded = model.SpectrumLoaded;
            ReferenceHasSpectrumInfomation = model.ReferenceHasSpectrumInfomation;

            UpperSpectraViewModel = model.UpperSpectraModel.ToReadOnlyReactiveCollection(m => new SingleSpectrumViewModel(m)).AddTo(Disposables);
            LowerSpectrumViewModel = new SingleSpectrumViewModel(model.LowerSpectrumModel).AddTo(Disposables);

            HorizontalAxis = (horizontalAxisSource ?? model.UpperSpectrumModel.HorizontalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerVerticalAxis = (lowerVerticalAxisSource ?? model.LowerSpectrumModel.VerticalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            LowerVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel>(model.LowerVerticalAxisItemCollection);

            UpperVerticalAxis = (upperVerticalAxisSource ?? model.UpperSpectrumModel.VerticalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            UpperVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel>(model.UpperVerticalAxisItemCollection);

            UpperSpectrum = model.UpperSpectrumModel.Spectrum
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrum = model.LowerSpectrumModel.Spectrum
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            GraphTitle = Observable.Return(model.GraphLabels.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = Observable.Return(model.GraphLabels.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = Observable.Return(model.GraphLabels.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = Observable.Return(model.HorizontalPropertySelector.Property)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = Observable.Return(model.VerticalPropertySelector.Property)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = Observable.Return(model.GraphLabels.AnnotationLabelProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            OrderingProperty = Observable.Return(model.GraphLabels.AnnotationOrderProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
           
            UpperSpectrumBrushSource = model.UpperSpectrumModel.Brush
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrumBrushSource = model.LowerSpectrumModel.Brush
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SaveMatchedSpectrumCommand = model.CanSaveMatchedSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveMatchedSpectrum,  filter: "tab separated values(*.txt)|*.txt"))
                .AddTo(Disposables);

            SaveUpperSpectrumCommand = model.CanSaveUpperSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveUpperSpectrum,  filter: "NIST format(*.msp)|*.msp"))
                .AddTo(Disposables);

            SaveLowerSpectrumCommand = model.CanSaveLowerSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveLowerSpectrum, filter:  "NIST format(*.msp)|*.msp"))
                .AddTo(Disposables);

            SwitchAllSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(model.SwitchViewToAllSpectrum)
                .AddTo(Disposables);

            SwitchCompareSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(model.SwitchViewToCompareSpectrum)
                .AddTo(Disposables);
            _model = model;
            FocusAction = focusAction;
            IsFocused = (isFocused ?? Observable.Never<bool>()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<SingleSpectrumViewModel> UpperSpectraViewModel { get; }
        public SingleSpectrumViewModel LowerSpectrumViewModel { get; }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> UpperSpectrum { get; }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> LowerSpectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInfomation { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> HorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> UpperVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel> UpperVerticalAxisItem => _model.UpperVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel> UpperVerticalAxisItemCollection { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LowerVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel> LowerVerticalAxisItem => _model.LowerVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel> LowerVerticalAxisItemCollection { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }

        public ReadOnlyReactivePropertySlim<string> OrderingProperty { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper> UpperSpectrumBrushSource { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper> LowerSpectrumBrushSource { get; }

        public ReactiveCommand SwitchAllSpectrumCommand { get; }

        public ReactiveCommand SwitchCompareSpectrumCommand { get; }

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public ReactiveCommand SaveMatchedSpectrumCommand { get; }

        public ReactiveCommand SaveUpperSpectrumCommand { get; }

        public ReactiveCommand SaveLowerSpectrumCommand { get; }

        private Action SaveSpectrum(Action<Stream> handler, string filter) {
            void result() {
                var request = new SaveFileNameRequest(path =>
                {
                    using (var fs = File.Open(path, FileMode.Create)) {
                        handler(fs);
                    }
                })
                {
                    Title = "Save spectra",
                    Filter = filter,
                    RestoreDirectory = true,
                    AddExtension = true,
                };
                MessageBroker.Default.Publish(request);
            }
            return result;
        }
    }
}
