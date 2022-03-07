﻿using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class SpectrumViewModel : ViewModelBase {
        public SpectrumViewModel(SpectrumModel model) {
            Model = model;

            Name = Observable.Return(Model.Name).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            DisplayScans = Model.DisplayScans.ToReadOnlyReactiveCollection().AddTo(Disposables);
            DisplayScan = new ReactivePropertySlim<DisplayScan>().AddTo(Disposables);

            var collectionChanged = new[]
            {
                DisplayScans.ObserveAddChanged().ToUnit(),
                DisplayScans.ObserveRemoveChanged().ToUnit(),
                DisplayScans.ObserveResetChanged().ToUnit(),
            }.Merge();
            HorizontalAxis = collectionChanged
                .Where(_ => DisplayScans.Any())
                .Select(_ => DisplayScans
                    .Select(scan => new Range(scan.Spectrum.DefaultIfEmpty().Min(s => s?.Mass) ?? 0d, scan.Spectrum.DefaultIfEmpty().Max(s => s?.Mass) ?? 0d))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(30), labelType: LabelType.Standard)
                .AddTo(Disposables);
            VerticalAxis = collectionChanged
                .Where(_ => DisplayScans.Any())
                .Select(_ => DisplayScans
                    .Select(scan => new Range(scan.Spectrum.DefaultIfEmpty().Min(s => s?.Intensity) ?? 0d, scan.Spectrum.DefaultIfEmpty().Max(s => s?.Intensity) ?? 0d))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0, 0), labelType: LabelType.Order)
                .AddTo(Disposables);

            ChartBrushes = new[]
            {
                Brushes.White,
                Brushes.LightCoral,
                Brushes.LightSkyBlue,
                Brushes.LightGreen,
                Brushes.LightGray,
                Brushes.LightPink,
                Brushes.LightBlue,
                Brushes.LightYellow,
            };
            BrushMapper = new SequentialBrushMapper(ChartBrushes);

            DropCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposables);
            DropCommand
                .Where(e => !e.Handled && e.Data.GetDataPresent(typeof(DisplayScan)))
                .Do(e => e.Handled = true)
                .Select(e => e.Data.GetData(typeof(DisplayScan)))
                .OfType<DisplayScan>()
                .Where(scan => !DisplayScans.Contains(scan))
                .Subscribe(AddScan)
                .AddTo(Disposables);

            CloseCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public SpectrumModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReadOnlyReactiveCollection<DisplayScan> DisplayScans { get; }

        public ReadOnlyReactiveCollection<DisplayScan> UpperScans { get; }

        public ReadOnlyReactiveCollection<DisplayScan> LowerScans { get; }

        public ReactivePropertySlim<DisplayScan> DisplayScan { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IBrushMapper BrushMapper { get; }

        public Brush[] ChartBrushes { get; }

        public ReactiveCommand<DragEventArgs> DropCommand { get; }

        public ReactiveCommand CloseCommand { get; }
        
        public void AddScan(IMSScanProperty scan) {
            Model.AddScan(scan);
        }
    }
}
