﻿using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IDataCollectionRangeSetting : INotifyPropertyChanged
    {
        float Begin { get; set; }
        float End { get; set; }

        bool NeedAccumulation { get; }
        float AccumulatedRange { get; set; }

        void Commit();
    }

    public abstract class DataCollectionRangeSetting : BindableBase
    {
        public DataCollectionRangeSetting(bool needAccumulation) {
            NeedAccumulation = needAccumulation;
        }

        public float Begin {
            get => begin;
            set => SetProperty(ref begin, value);
        }
        private float begin;

        public float End {
            get => end;
            set => SetProperty(ref end, value);
        }
        private float end;

        public bool NeedAccumulation { get; }

        public float AccumulatedRange {
            get => accumulatedRange;
            set => SetProperty(ref accumulatedRange, value);
        }
        private float accumulatedRange;
    }

    public class RetentionTimeCollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        private readonly PeakPickBaseParameter parameter;
        private readonly MsdialLcImMsParameter lcImMsParameter;

        public RetentionTimeCollectionRangeSetting(PeakPickBaseParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.RetentionTimeBegin;
            End = parameter.RetentionTimeEnd;
            this.parameter = parameter;
        }

        public RetentionTimeCollectionRangeSetting(MsdialLcImMsParameter parameter, bool needAccmulation) : this(parameter.PeakPickBaseParam, needAccmulation) {
            AccumulatedRange = parameter.AccumulatedRtRange;
            lcImMsParameter = parameter;
        }

        public void Commit() {
            parameter.RetentionTimeBegin = Begin;
            parameter.RetentionTimeEnd = End;
            if (lcImMsParameter != null) {
                lcImMsParameter.AccumulatedRtRange = AccumulatedRange;
            }
        }
    }

    public class DriftTimeCollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        private readonly MsdialLcImMsParameter lcImMsParameter;
        private readonly MsdialImmsParameter immsParameter;

        public DriftTimeCollectionRangeSetting(MsdialLcImMsParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.DriftTimeBegin;
            End = parameter.DriftTimeEnd;
            lcImMsParameter = parameter;
        }

        public DriftTimeCollectionRangeSetting(MsdialImmsParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.DriftTimeBegin;
            End = parameter.DriftTimeEnd;
            immsParameter = parameter;
        }

        public void Commit() {
            if (lcImMsParameter != null) {
                lcImMsParameter.DriftTimeBegin = Begin;
                lcImMsParameter.DriftTimeEnd = End;
            }
            else if (immsParameter != null) {
                immsParameter.DriftTimeBegin = Begin;
                immsParameter.DriftTimeEnd = End;
            }
        }
    }

    public class Ms1CollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        private readonly PeakPickBaseParameter parameter;

        public Ms1CollectionRangeSetting(PeakPickBaseParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.MassRangeBegin;
            End = parameter.MassRangeEnd;
            this.parameter = parameter;
        }

        public void Commit() {
            parameter.MassRangeBegin = Begin;
            parameter.MassRangeEnd = End;
        }
    }

    public class Ms2CollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        private readonly PeakPickBaseParameter parameter;

        public Ms2CollectionRangeSetting(PeakPickBaseParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.Ms2MassRangeBegin;
            End = parameter.Ms2MassRangeEnd;
            this.parameter = parameter;
        }

        public void Commit() {
            parameter.Ms2MassRangeBegin = Begin;
            parameter.Ms2MassRangeEnd = End;
        }
    }
}
