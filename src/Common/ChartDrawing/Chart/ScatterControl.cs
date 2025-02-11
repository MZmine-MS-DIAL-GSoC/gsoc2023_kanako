﻿using CompMs.Common.Extension;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public class ScatterControl : ChartBaseControl
    {
        public ScatterControl() {
            ClipToBounds = true;
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(ScatterControl),
                new PropertyMetadata(
                    default(System.Collections.IEnumerable),
                    OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as ScatterControl;
            if (chart == null) return;

            chart.dataType = null;
            chart.hPropertyReflection = null;
            chart.vPropertyReflection = null;
            chart.pPropertyReflection = null;

            if (chart.cv != null) {
                chart.cv.CurrentChanged -= chart.OnCurrentChanged;
                if (chart.cv is INotifyCollectionChanged collectionOld) {
                    collectionOld.CollectionChanged -= chart.ItemsSourceCollectionChanged;
                }
            }

            chart.cv = CollectionViewSource.GetDefaultView(e.NewValue);
            if (chart.cv == null) {
                return;
            }
            chart.cv.CurrentChanged += chart.OnCurrentChanged;
            if (chart.cv is INotifyCollectionChanged collectionNew) {
                collectionNew.CollectionChanged += chart.ItemsSourceCollectionChanged;
            }
            if (chart.cv.IsEmpty) {
                chart.cv.Refresh();
                if (chart.cv.IsEmpty) {
                    chart.SetDrawingVisuals();
                    chart.Update();
                    return;
                }
            }

            var cv = chart.cv as CollectionView;
            if (cv is null) {
                return;
            }

            chart.ItemsSourceFirstItemAdded(cv.GetItemAt(0));

            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);

            chart.SetDrawingVisuals();
            chart.Update();
        }

        private void ItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (dataType is null && cv is CollectionView cv_ && !cv_.IsEmpty) {
                ItemsSourceFirstItemAdded(cv_.GetItemAt(0));
            }
            SetDrawingVisuals();
            Update();
        }

        private void ItemsSourceFirstItemAdded(object newItem) {
            if ((dataType = newItem.GetType()) is null) {
                return;
            }
            if (HorizontalPropertyName != null)
                hPropertyReflection = dataType.GetProperty(HorizontalPropertyName);
            if (VerticalPropertyName != null)
                vPropertyReflection = dataType.GetProperty(VerticalPropertyName);
            if (EachPlotBrushName != null)
                pPropertyReflection = dataType.GetProperty(EachPlotBrushName);
        }

        void OnCurrentChanged(object obj, EventArgs e) {
            if (cv == null) return;
            var item = cv.CurrentItem;
            SelectedItem = item;

            if (item == null) {
                SelectedPoint = null;
                return;
            }
            var x = hPropertyReflection.GetValue(item);
            var y = vPropertyReflection.GetValue(item);
            double xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX, ActualWidth);
            double yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY, ActualHeight);
            var pos = new Point(xx, yy);
            select = visualChildren.OfType<AnnotatedDrawingVisual>().Argmin(dv => (dv.Center - pos).Length);
            SelectedPoint = select.Center;
        }

        public static readonly DependencyProperty HorizontalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(HorizontalPropertyName), typeof(string), typeof(ScatterControl),
                new PropertyMetadata(
                    default(string),
                    OnHorizontalPropertyNameChanged));

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as ScatterControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        private PropertyInfo hPropertyReflection;

        public static readonly DependencyProperty VerticalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(VerticalPropertyName), typeof(string), typeof(ScatterControl),
                new PropertyMetadata(
                    default(string),
                    OnVerticalPropertyNameChanged));

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as ScatterControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        private PropertyInfo vPropertyReflection;

        public static readonly DependencyProperty PointGeometryProperty =
            DependencyProperty.Register(
                nameof(PointGeometry), typeof(Geometry), typeof(ScatterControl),
                new PropertyMetadata(
                    new EllipseGeometry(new Rect(0, 0, 1, 1)),
                    OnPointGeometryChanged));

        public Geometry PointGeometry {
            get => (Geometry)GetValue(PointGeometryProperty);
            set => SetValue(PointGeometryProperty, value);
        }

        static void OnPointGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ScatterControl scatter) {
                scatter.Generator.UpdateBrush(scatter);
            }
        }

        public static readonly DependencyProperty PointBrushProperty =
            DependencyProperty.Register(
                nameof(PointBrush), typeof(Brush), typeof(ScatterControl),
                new PropertyMetadata(
                    Brushes.Black,
                    OnPointBrushChanged));

        public Brush PointBrush {
            get => (Brush)GetValue(PointBrushProperty);
            set => SetValue(PointBrushProperty, value);
        }

        static void OnPointBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ScatterControl scatter) {
                scatter.Generator.UpdateBrush(scatter);
            }
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(
                nameof(Radius), typeof(double), typeof(ScatterControl),
                new PropertyMetadata(3d));

        // test
        public static readonly DependencyProperty EachPlotBrushNameProperty =
            DependencyProperty.Register(
               nameof(EachPlotBrushName), typeof(string), typeof(ScatterControl),
               new PropertyMetadata(
                   default(string),
                   OnEachPlotBrushNameChanged));

        public string EachPlotBrushName {
            get => (string)GetValue(EachPlotBrushNameProperty);
            set => SetValue(EachPlotBrushNameProperty, value);
        }

        static void OnEachPlotBrushNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as ScatterControl;
            if (chart == null) return;

            if (chart.dataType != null) {
                chart.pPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);
                chart.Generator.UpdateBrush(chart);
            }

            chart.Update();
        }

        private PropertyInfo pPropertyReflection;

        public static readonly DependencyProperty BrushMapperProperty =
            DependencyProperty.Register(
                nameof(BrushMapper), typeof(IBrushMapper), typeof(ScatterControl),
                new FrameworkPropertyMetadata(
                    default,
                    OnBrushMapperChanged));

        public IBrushMapper BrushMapper {
            get => (IBrushMapper)GetValue(BrushMapperProperty);
            set => SetValue(BrushMapperProperty, value);
        }

        static void OnBrushMapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ScatterControl scatter) {
                scatter.Generator.UpdateBrush(scatter);
            }
        }

        public double Radius {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem), typeof(object), typeof(ScatterControl),
                new PropertyMetadata(
                    null,
                    OnSelectedItemChanged));

        public object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedPointProperty =
            DependencyProperty.Register(
                nameof(SelectedPoint), typeof(Point?), typeof(ScatterControl),
                new PropertyMetadata(default));

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as ScatterControl;
            if (chart == null) return;

            if (chart.cv != null)
                chart.cv.MoveCurrentTo(e.NewValue);
        }

        public Point? SelectedPoint {
            get => (Point?)GetValue(SelectedPointProperty);
            set => SetValue(SelectedPointProperty, value);
        }

        public static RoutedEvent SelectChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SelectChanged), RoutingStrategy.Bubble, typeof(RoutedEvent), typeof(ScatterControl));
        private static RoutedEventArgs SelectChangedEventArgs = new RoutedEventArgs(SelectChangedEvent);

        public event RoutedEventHandler SelectChanged {
            add => AddHandler(SelectChangedEvent, value);
            remove => RemoveHandler(SelectChangedEvent, value);
        }

        HitTestFilterBehavior VisualHitTestFilter(DependencyObject d) {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            if (e.ClickCount == 1) {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(VisualHitTestFilter),
                    new HitTestResultCallback(VisualSelectHitTest),
                    new PointHitTestParameters(pt)
                    );
            }
        }

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result) {
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            if (dv != select) {
                select = dv;
                RaiseEvent(SelectChangedEventArgs);
            }
            if (select.Annotation != SelectedItem) {
                SelectedItem = select.Annotation;
            }
            if (select.Center != SelectedPoint) {
                SelectedPoint = select.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        public static readonly DependencyProperty FocusedItemProperty =
            DependencyProperty.Register(
                nameof(FocusedItem), typeof(object), typeof(ScatterControl),
                new PropertyMetadata(null));

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public static readonly DependencyProperty FocusedPointProperty =
            DependencyProperty.Register(
                nameof(FocusedPoint), typeof(Point?), typeof(ScatterControl),
                new PropertyMetadata(default));

        public Point? FocusedPoint {
            get => (Point?)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        public static RoutedEvent FocusChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(FocusChanged), RoutingStrategy.Bubble, typeof(RoutedEvent), typeof(ScatterControl));
        private static RoutedEventArgs FocusChangedEventArgs = new RoutedEventArgs(FocusChangedEvent);

        public event RoutedEventHandler FocusChanged {
            add => AddHandler(FocusChangedEvent, value);
            remove => RemoveHandler(FocusChangedEvent, value);
        }

        //protected override void OnMouseMove(MouseEventArgs e) {
        //    base.OnMouseMove(e);
        //    var pt = e.GetPosition(this);

        //    VisualTreeHelper.HitTest(this,
        //        new HitTestFilterCallback(VisualHitTestFilter),
        //        new HitTestResultCallback(VisualFocusHitTest),
        //        new PointHitTestParameters(pt));
        //}

        HitTestResultBehavior VisualFocusHitTest(HitTestResult result) {
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            if (dv != focus) {
                focus = dv;
                RaiseEvent(FocusChangedEventArgs);
            }
            if (FocusedItem != focus.Annotation) {
                FocusedItem = focus.Annotation;
            }
            if (FocusedPoint != focus.Center) {
                FocusedPoint = focus.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        private BrushGenerator Generator {
            get {
                if (generator != null) {
                    return generator;
                }
                generator = new BrushGenerator();
                generator.UpdateBrush(this);
                return generator;
            }
        }
        private BrushGenerator generator;

        private ICollectionView cv;
        private Type dataType;
        private AnnotatedDrawingVisual focus, select;

        protected override void Update() {
            base.Update();
            if (hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || PointBrush == null
               )
                return;

            double radius = Radius, actualWidth = ActualWidth, actualHeight = ActualHeight;

            foreach (var visual in visualChildren) {
                if (!(visual is AnnotatedDrawingVisual dv)) continue;
                var o = dv.Annotation;
                var x = hPropertyReflection.GetValue(o);
                var y = vPropertyReflection.GetValue(o);

                double xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX, actualWidth);
                double yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY, actualHeight);
                dv.Center = new Point(xx, yy);

                using (var dc = dv.RenderOpen()) {
                    dc.DrawRectangle(Generator.GetBrush(o), null, new Rect(xx - radius, yy - radius, radius * 2, radius * 2));
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            FocusedPoint = focus?.Center;
            SelectedPoint = select?.Center;
        }

        private void SetDrawingVisuals() {
            visualChildren.Clear();
            if (cv == null) return;

            foreach (var o in cv)
                visualChildren.Add(new AnnotatedDrawingVisual(o));
        }

        class BrushGenerator
        {
            // 1. pPropertyReflection, BrushMapper
            // 2. pPropertyReflection
            // 3. BrushMapper
            // 4. PointBrush
            private PropertyInfo pPropertyInfo;
            private IBrushMapper mapper;
            private Brush pointBrush;
            private Geometry geometry;
            private readonly Dictionary<object, Brush> cache = new Dictionary<object, Brush>();

            internal Brush GetBrush(object o) {
                if (cache.ContainsKey(o)) {
                    return cache[o];
                }
                Brush brush;
                if (pPropertyInfo != null && mapper != null) {
                    var v = pPropertyInfo.GetValue(o);
                    brush = mapper.Map(v);
                }
                else if (pPropertyInfo != null) {
                    brush = (Brush)pPropertyInfo.GetValue(o);
                }
                else if (mapper != null) {
                    brush = mapper.Map(o);
                }
                else {
                    brush = pointBrush;
                }
                if (geometry != null) {
                    brush = new DrawingBrush(new GeometryDrawing(brush, null, geometry));
                }
                brush?.Freeze();
                return cache[o] = brush;
            }

            internal void UpdateBrush(ScatterControl scatter) {
                pPropertyInfo = scatter.pPropertyReflection;
                mapper = scatter.BrushMapper;
                pointBrush = scatter.PointBrush;
                geometry = scatter.PointGeometry;
                cache.Clear();
            }
        }
    }
}
