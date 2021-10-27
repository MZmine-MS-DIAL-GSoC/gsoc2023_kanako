﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public class IdentifySettingModel : BindableBase
    {
        public IdentifySettingModel(ParameterBase parameter, IAnnotatorSettingModelFactory annotatorFactory, DataBaseStorage dataBaseStorage = null) {
            this.parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            this.annotatorFactory = annotatorFactory ?? throw new System.ArgumentNullException(nameof(annotatorFactory));

            if (!(dataBaseStorage is null)) {
                foreach (var dataBase in dataBaseStorage.MetabolomicsDataBases) {
                    var dbModel = new DataBaseSettingModel(parameter, dataBase.DataBase);
                    DataBaseModels.Add(dbModel);
                    foreach (var pair in dataBase.Pairs) {
                        AnnotatorModels.Add(annotatorFactory.Create(dbModel, pair.AnnotatorID, pair.SearchParameter));
                    }
                }
                foreach (var dataBase in dataBaseStorage.ProteomicsDataBases) {
                    var dbModel = new DataBaseSettingModel(parameter, dataBase.DataBase);
                    DataBaseModels.Add(dbModel);
                    foreach (var pair in dataBase.Pairs) {
                        AnnotatorModels.Add(annotatorFactory.Create(dbModel, pair.AnnotatorID, pair.SearchParameter));
                    }
                }
            }
        }

        private readonly ParameterBase parameter;
        private readonly IAnnotatorSettingModelFactory annotatorFactory;
        private int serialNumber = 1;

        public ObservableCollection<DataBaseSettingModel> DataBaseModels { get; } = new ObservableCollection<DataBaseSettingModel>();

        public DataBaseSettingModel DataBaseModel {
            get => dataBaseModel;
            set {
                if (SetProperty(ref dataBaseModel, value)) {
                    if (AnnotatorModel?.DataBaseSettingModel != value) {
                        AnnotatorModel = AnnotatorModels.LastOrDefault(annotator => annotator.DataBaseSettingModel == value);
                    }
                }
            }
        }
        private DataBaseSettingModel dataBaseModel;

        public ObservableCollection<IAnnotatorSettingModel> AnnotatorModels { get; } = new ObservableCollection<IAnnotatorSettingModel>();

        public IAnnotatorSettingModel AnnotatorModel {
            get => annotatorModel;
            set {
                if (SetProperty(ref annotatorModel, value)) {
                    if (!(value is null) && DataBaseModel != value.DataBaseSettingModel) {
                        DataBaseModel = value.DataBaseSettingModel;
                    }
                }
            }
        }
        private IAnnotatorSettingModel annotatorModel;

        private readonly object Lock = new object();
        private readonly object dbLock = new object();
        private readonly object annotatorLock = new object();

        public void AddDataBase() {
            lock (Lock) {
                var db = new DataBaseSettingModel(parameter);
                lock (dbLock) {
                    DataBaseModels.Add(db);
                }
                DataBaseModel = db;
            }
        }

        public DataBaseSettingModel AddDataBaseZZZ() {
            var db = new DataBaseSettingModel(parameter);
            DataBaseModels.Add(db);
            return db;
        }

        public void RemoveDataBase() {
            lock (Lock) {
                var db = DataBaseModel;
                if (!(db is null)) {
                    lock (dbLock) {
                        DataBaseModels.Remove(db);
                    }
                    var removeAnnotators = AnnotatorModels.Where(annotator => annotator.DataBaseSettingModel == db).ToArray();
                    lock (annotatorLock) {
                        foreach (var annotator in removeAnnotators) {
                            AnnotatorModels.Remove(annotator);
                        }
                    }
                    DataBaseModel = DataBaseModels.LastOrDefault();
                }
            }
        }

        public void RemoveDataBaseZZZ(DataBaseSettingModel db) {
            if (!(db is null)) {
                DataBaseModels.Remove(db);
                var removeAnnotators = AnnotatorModels.Where(annotator => annotator.DataBaseSettingModel == db).ToArray();
                foreach (var annotator in removeAnnotators) {
                    AnnotatorModels.Remove(annotator);
                }
            }
        }

        public void AddAnnotator() {
            lock (Lock) {
                var db = DataBaseModel;
                if (!(db is null)) {
                    var annotatorModel = annotatorFactory.Create(db, $"{db.DataBaseID}_{serialNumber++}", null);
                    lock (annotatorLock) {
                        AnnotatorModels.Add(annotatorModel);
                    }
                    AnnotatorModel = annotatorModel;
                }
            }
        }

        public IAnnotatorSettingModel AddAnnotatorZZZ(DataBaseSettingModel db) {
            if (!(db is null)) {
                var annotatorModel = annotatorFactory.Create(db, $"{db.DataBaseID}_{serialNumber++}", null);
                AnnotatorModels.Add(annotatorModel);
                return annotatorModel;
            }
            return null;
        }

        public void RemoveAnnotator() {
            lock (Lock) {
                var annotatorModel = AnnotatorModel;
                if (!(annotatorModel is null)) {
                    lock (annotatorLock) {
                        AnnotatorModels.Remove(annotatorModel);
                    }
                    AnnotatorModel = AnnotatorModels.LastOrDefault();
                }
            }
        }

        public void RemoveAnnotatorZZZ(IAnnotatorSettingModel annotator) {
            if (!(annotator is null)) {
                AnnotatorModels.Remove(annotator);
            }
        }

        public void MoveUpAnnotator() {
            lock (Lock) {
                var annotatorModel = AnnotatorModel;
                if (!(annotatorModel is null)) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    if (index == 0) {
                        return;
                    }
                    lock (annotatorLock) {
                        AnnotatorModels.Move(index, index - 1);
                    }
                }
            }
        }

        public void MoveUpAnnotatorZZZ(IAnnotatorSettingModel annotator) {
            if (!(annotator is null)) {
                var index = AnnotatorModels.IndexOf(annotator);
                if (index <= 0 || index >= AnnotatorModels.Count) {
                    return;
                }
                AnnotatorModels.Move(index, index - 1);
            }
        }

        public void MoveDownAnnotator() {
            lock (Lock) {
                var annotatorModel = AnnotatorModel;
                if (!(annotatorModel is null)) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    if (index == AnnotatorModels.Count - 1) {
                        return;
                    }
                    lock (annotatorLock) {
                        AnnotatorModels.Move(index, index+1);
                    }
                }
            }
        }

        public void MoveDownAnnotatorZZZ(IAnnotatorSettingModel annotator) {
            if (!(annotator is null)) {
                var index = AnnotatorModels.IndexOf(annotator);
                if (index < 0 || index >= AnnotatorModels.Count - 1) {
                    return;
                }
                AnnotatorModels.Move(index, index + 1);
            }
        }

        public DataBaseStorage Create() {
            var result = DataBaseStorage.CreateEmpty();
            SetAnnotatorContainer(result);
            SetProteomicsAnnotatorContainer(result);
            return result;
        }

        private void SetAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<IMetabolomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreateMoleculeDataBase();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>();
                foreach (var annotatorModel in group) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    var annotators = annotatorModel.CreateAnnotator(db, AnnotatorModels.Count - index, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new MetabolomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter)));
                }
                storage.AddMoleculeDataBase(db, results);
            }
        }

        private void SetProteomicsAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<IProteomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreatePorteomicsDB();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>();
                foreach (var annotatorModel in group) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    var annotators = annotatorModel.CreateAnnotator(db, AnnotatorModels.Count - index, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new ProteomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter, db.ProteomicsParameter)));
                }
                storage.AddProteomicsDataBase(db, results);
            }
        }
    }
}