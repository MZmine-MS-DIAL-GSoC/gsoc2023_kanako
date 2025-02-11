﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.Chromatogram.ManualPeakModification;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.PeakCuration
{
    public sealed class AlignedChromatogramModificationViewModelLegacy : ViewModelBase {
        private readonly AlignedChromatogramModificationModelLegacy _model;

        public PeakModUCLegacy OriginalChromUC {
            get => _originalChromUC;
            set => SetProperty(ref _originalChromUC, value);
        }
        private PeakModUCLegacy _originalChromUC;
        public PeakModUCLegacy AlignedChromUC {
            get => _alignedChromUC;
            set => SetProperty(ref _alignedChromUC, value);
        }
        private PeakModUCLegacy _alignedChromUC;
        public PeakModUCLegacy PickingUC {
            get => _pickingUC;
            set => SetProperty(ref _pickingUC, value);
        }
        private PeakModUCLegacy _pickingUC;

        public PeakPropertyLegacy[] PeakPropertyList => _model.ObservablePeakProperties.Value.Properties;
        public bool IsRI => _model.IsRI.Value;

        public AlignedChromatogramModificationViewModelLegacy(AlignedChromatogramModificationModelLegacy model) {
            _model = model;
            model.ObservablePeakProperties.ObserveOnDispatcher().Subscribe(peakProperties =>
            {
                var dv_ = UtilityLegacy.GetDrawingVisualUC(peakProperties.Properties, PeakModType.Original);
                var dv2_ = UtilityLegacy.GetDrawingVisualUC(peakProperties.Properties, PeakModType.Aligned);
                var dv3_ = UtilityLegacy.GetDrawingVisualUC(peakProperties.Properties, PeakModType.Picking);
                var originalChromUC = new PeakModUCLegacy(this, dv_, new MouseActionSetting() { FixMinY = true }, PeakModType.Original, peakProperties.Properties.ToList());
                originalChromUC.RefreshUI();
                OriginalChromUC = originalChromUC;
                var alignedChromUC = new PeakModUCLegacy(this, dv2_, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, peakProperties.Properties.ToList());
                alignedChromUC.RefreshUI();
                AlignedChromUC = alignedChromUC;
                var pickingUC = new PeakModUCLegacy(this, dv3_, new MouseActionSetting() { CanMouseAction = false }, PeakModType.Picking);
                pickingUC.RefreshUI();
                PickingUC = pickingUC;
            });
        }

        public void UpdateAlignedChromUC() {
            var dv2 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            var alignedChromUC = new PeakModUCLegacy(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList.ToList());
            alignedChromUC.RefreshUI();
            AlignedChromUC = alignedChromUC;
        }
        public void UpdatePickingChromUC() {
            var dv = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking, IsRI);
            var pickingUC = new PeakModUCLegacy(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Picking, PeakPropertyList.ToList());
            pickingUC.RefreshUI();
            PickingUC = pickingUC;
        }

        public void UpdatePeakInfo() {
            _model.UpdatePeakInfo();
        }

        public void ClearRtAlignment() {
            _model.ClearRtAlignment();
            var dv2 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            AlignedChromUC = new PeakModUCLegacy(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList.ToList());
        }
    }

    public sealed class PeakPropertiesLegacy {
        private readonly AlignmentSpotPropertyModel _spot;

        public PeakPropertiesLegacy(AlignmentSpotPropertyModel spot, PeakPropertyLegacy[] properties) {
            _spot = spot ?? throw new ArgumentNullException(nameof(spot));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public PeakPropertyLegacy[] Properties { get; }

        public void UpdatePeakInfo() {
            if (Properties.IsEmptyOrNull()) {
                return;
            }

            var rtList = new List<double>();
            foreach (var p in Properties) {
                if (p.Accessory is null) {
                    return;
                }

                p.Model.ChromXsLeft = new ChromXs(p.Accessory.Chromatogram.RtLeft, _spot.ChromXType, _spot.ChromXUnit);
                p.Model.ChromXsTop = new ChromXs(p.Accessory.Chromatogram.RtTop, _spot.ChromXType, _spot.ChromXUnit);
                p.Model.ChromXsRight = new ChromXs(p.Accessory.Chromatogram.RtRight, _spot.ChromXType, _spot.ChromXUnit);

                rtList.Add(p.Accessory.Chromatogram.RtTop);

                //p.PeakBean.PeakID = -3;
                p.Model.IsManuallyModifiedForQuant = true;
                p.Model.PeakAreaAboveZero = p.PeakAreaAboveZero;
                p.Model.PeakHeightTop = p.PeakHeight;
                p.Model.SignalToNoise = p.Accessory.Chromatogram.SignalToNoise;
            }

            if (rtList.Max() > 0) {
                Properties[0].AverageRt = (float)rtList.Average();
            }
            _spot.TimesCenter = Properties[0].AverageRt;
            _spot.IsManuallyModifiedForQuant = true;
        }

        public void ClearRtAlignment() {
            foreach (var p in Properties) {
                p.ClearAlignedPeakList();
            }
        }
    }

    public class PeakPropertyLegacy {
        public AlignmentChromPeakFeatureModel Model { get; set; }
        //public ChromatogramPeakInfo PeakSpotInfo { get; set; }
        public Brush Brush { get; set; }
        public List<ChromatogramPeak> SmoothedPeakList { get; set; }
        public List<ChromatogramPeak> AlignedPeakList { get; set; }
        public float AlignOffset { get; set; }
        public float AverageRt { get; set; }
        public float PeakAreaAboveZero { get; set; }
        public float PeakHeight { get; set; }
        public Accessory Accessory { get; set; }

        //public PeakPropertyLegacy(AlignmentChromPeakFeature bean, ChromatogramPeakInfo info, Brush brush, List<ChromatogramPeak> speaks) {
        //    Model = bean;
        //    PeakSpotInfo = info;
        //    Brush = brush;
        //    SmoothedPeakList = speaks;
        //}

        public PeakPropertyLegacy(AlignmentChromPeakFeatureModel bean, Brush brush, List<ChromatogramPeak> speaks) {
            Model = bean;
            Brush = brush;
            SmoothedPeakList = speaks;
        }

        public void SetAlignOffSet(float val) {
            AlignOffset = val;
            AlignedPeakList = new List<ChromatogramPeak>();
            foreach (var p in SmoothedPeakList) {
                var nChromXs = new ChromXs(p.ChromXs.Value - AlignOffset, p.ChromXs.Type, p.ChromXs.Unit);
                AlignedPeakList.Add(new ChromatogramPeak(p.ID, 0d, p.Intensity, nChromXs));
            }
        }

        public void ClearAlignedPeakList() {
            AlignedPeakList = new List<ChromatogramPeak>();
            foreach (var p in SmoothedPeakList) {
                AlignedPeakList.Add(new ChromatogramPeak(p.ID, 0d, p.Intensity, p.ChromXs));
            }
        }

    }

    public class UtilityLegacy {
        public static void ChangeAlignedRtProperty(List<PeakPropertyLegacy> peakProperties, double minX, double maxX) {
            var rtList = new List<double>();
            if (maxX - minX < 0.01) return;
            foreach (var sample in peakProperties) {
                var maxInt = 0.0;
                var maxRt = 0.0;
                foreach (var p in sample.SmoothedPeakList) {
                    if (p.ChromXs.Value < minX) continue;
                    if (p.ChromXs.Value > maxX) break;
                    if (maxInt < p.Intensity) {
                        maxInt = p.Intensity;
                        maxRt = p.ChromXs.Value;
                    }
                }
                rtList.Add(maxRt);
            }
            if (rtList.Max() <= 0) return;
            var aveRt = rtList.Where(x => x > 0).Average();
            peakProperties[0].AverageRt = (float)aveRt;
            for (var i = 0; i < peakProperties.Count; i++) {
                if (rtList[i] > 0)
                    peakProperties[i].SetAlignOffSet((float)(rtList[i] - aveRt));
                else
                    peakProperties[i].SetAlignOffSet(0f);
            }
        }

        public static void ModifyPeakEdge(List<PeakPropertyLegacy> peakProperties, double minX, double maxX) {
            foreach (var sample in peakProperties) {
                var maxInt = 0.0;
                var maxRt = 0.0;
                var sPeaklist = sample.AlignedPeakList;
                var peakAreaAboveZero = 0.0;
                for (var i = 0; i < sPeaklist.Count; i++) {
                    if (sPeaklist[i].ChromXs.Value < minX) continue;
                    if (sPeaklist[i].ChromXs.Value > maxX) break;
                    if (maxInt < sPeaklist[i].Intensity) {
                        maxInt = sPeaklist[i].Intensity;
                        maxRt = sPeaklist[i].ChromXs.Value;
                    }
                    if (i + 1 < sPeaklist.Count)
                        peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].ChromXs.Value - sPeaklist[i].ChromXs.Value) * 0.5;
                }
                if (maxRt == 0.0) {
                    maxRt = (minX + maxX) * 0.5;
                }
                var peakHeightFromBaseline = maxInt - Math.Min(sPeaklist[0].Intensity, sPeaklist[sPeaklist.Count - 1].Intensity);
                var noise = sample.Model.EstimatedNoise;
                var sn = peakHeightFromBaseline / noise;

                sample.PeakAreaAboveZero = (float)peakAreaAboveZero * 60.0f;
                sample.PeakHeight = (float)maxInt;
                sample.Accessory = new Accessory();
                sample.Accessory.SetChromatogram((float)(maxRt + sample.AlignOffset), (float)(minX + sample.AlignOffset), (float)(maxX + sample.AlignOffset),
                    noise, (float)sn);
            }
        }

        public static DrawVisualManualPeakModification GetDrawingVisualUC(IReadOnlyList<PeakPropertyLegacy> peakProperties, PeakModType type, bool isRI = false, bool isDrift = false) {
            if (peakProperties == null || peakProperties.Count == 0) return null;
            var xtitle = "Retention time (min)";
            if (isRI)
                xtitle = "Retention index";
            else if (isDrift)
                xtitle = "Mobility";


            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xtitle },
                AxisY = new AxisY() { AxisLabel = "Intensity" },
                LabelSpace = new LabelSpace(0, 10, 0, 0)
            };
            var title = new Title();
            if (type == PeakModType.Original)
                title.Label = "Original extracted ion chromatograms";
            else if (type == PeakModType.Aligned)
                title.Label = "Aligned chromatograms";
            else
                title.Label = "Manually modified chromatograms";
            var slist = GetChromatogramFromAlignedData(peakProperties, type, isRI);
            return new DrawVisualManualPeakModification(area, title, slist);
        }

        private static SeriesList GetChromatogramFromAlignedData(IReadOnlyList<PeakPropertyLegacy> peakProperties, PeakModType type, bool isRI = false) {
            var slist = new SeriesList();
            for (var i = 0; i < peakProperties.Count; i++) {
                var prop = peakProperties[i];
                var point = new Series() {
                    ChartType = ChartType.Chromatogram,
                    MarkerType = MarkerType.None,
                    Pen = new Pen(prop.Brush, 1.0),
                    Brush = prop.Brush
                };

                if (type == PeakModType.Original) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    point.Accessory.SetChromatogram(
                        (float)prop.Model.ChromXsTop.Value, 
                        (float)prop.Model.ChromXsLeft.Value, 
                        (float)prop.Model.ChromXsRight.Value,
                        prop.Model.EstimatedNoise, 
                        prop.Model.SignalToNoise);
                }
                else if (type == PeakModType.Aligned) {
                    foreach (var p in prop.AlignedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        point.Accessory.SetChromatogram(
                            (float)prop.Model.ChromXsTop.Value - prop.AlignOffset,
                            (float)prop.Model.ChromXsLeft.Value - prop.AlignOffset,
                            (float)prop.Model.ChromXsRight.Value - prop.AlignOffset,
                            prop.Model.EstimatedNoise,
                            prop.Model.SignalToNoise);
                    }
                    else {
                        point.Accessory.SetChromatogram(prop.Accessory.Chromatogram.RtTop - prop.AlignOffset,
                            prop.Accessory.Chromatogram.RtLeft - prop.AlignOffset,
                            prop.Accessory.Chromatogram.RtRight - prop.AlignOffset,
                            prop.Model.EstimatedNoise,
                            prop.Model.SignalToNoise);
                    }
                }
                else if (type == PeakModType.Picking) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        point.Accessory.SetChromatogram(
                            (float)prop.Model.ChromXsTop.Value,
                            (float)prop.Model.ChromXsLeft.Value,
                            (float)prop.Model.ChromXsRight.Value,
                            prop.Model.EstimatedNoise,
                            prop.Model.SignalToNoise);
                    }
                    else
                        point.Accessory = prop.Accessory;
                }

                if (point.Points.Count > 0)
                    slist.Series.Add(point);
            }
            return slist;
        }
    }
}
