﻿using CompMs.App.Msdial.Model.Information;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingImageModel : DisposableModelBase
    {
        public ImagingImageModel(AnalysisFileBean file) {
            File = file ?? throw new System.ArgumentNullException(nameof(file));
            ImagingRoiModels = new ObservableCollection<ImagingRoiModel>();
            ImageResult = new WholeImageResultModel(file).AddTo(Disposables);

            Roi = new RoiModel(file, file.GetMaldiFrames());
            ImagingRoiModels.Add(new ImagingRoiModel(Roi, ImageResult.GetTargetElements(), ImageResult.Target, file.GetMaldiFrameLaserInfo()).AddTo(Disposables));

            PeakInformationModel = new PeakInformationAnalysisModel(ImageResult.Target).AddTo(Disposables);
            PeakInformationModel.Add(
                t => new MzPoint(t?.Mass ?? 0d),
                t => new DriftPoint(t?.InnerModel.ChromXsTop.Drift.Value ?? 0d),
                t => new CcsPoint(t?.InnerModel.CollisionCrossSection ?? 0d));
            PeakInformationModel.Add(
                t => new HeightAmount(t?.Intensity ?? 0d),
                t => new AreaAmount(t?.PeakArea ?? 0d));
        }

        public WholeImageResultModel ImageResult { get; }
        public RoiModel Roi { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public AnalysisFileBean File { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
    }
}
