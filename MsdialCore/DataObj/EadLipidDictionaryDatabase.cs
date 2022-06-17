﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Lipidomics;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    internal sealed class EadLipidDictionaryDatabase : ILipidDatabase
    {
        private readonly string _dbPath;
        private readonly string _id;
        private readonly ILipidSpectrumGenerator _lipidGenerator;
        private readonly IEqualityComparer<ILipid> _comparer;
        private readonly ConcurrentDictionary<ILipid, Lazy<MoleculeMsReference>> _lipidToReference;
        private readonly List<MoleculeMsReference> _references;
        private readonly object syncObject = new object();

        public EadLipidDictionaryDatabase(string dbPath, string id) {
            _dbPath = dbPath;
            _id = id;
            _lipidGenerator = FacadeLipidSpectrumGenerator.Default;
            _comparer = new LipidNameComparer();
            _lipidToReference = new ConcurrentDictionary<ILipid, Lazy<MoleculeMsReference>>(_comparer);
            _references = new List<MoleculeMsReference>();
        }

        public List<MoleculeMsReference> Generates(IEnumerable<ILipid> lipids, ILipid seed, AdductIon adduct, MoleculeMsReference baseReference) {
            var references = new List<MoleculeMsReference>();
            foreach (var lipid in lipids) {
                var lazyReference = _lipidToReference.GetOrAdd(lipid, lipid_ => new Lazy<MoleculeMsReference>(() => GenerateReference(lipid_, adduct, baseReference), isThreadSafe: true));
                if (lazyReference.Value is MoleculeMsReference reference) {
                    references.Add(reference);
                }
            }
            return references;
        }

        private MoleculeMsReference GenerateReference(ILipid lipid, AdductIon adduct, MoleculeMsReference baseReference) {
            if (!_lipidGenerator.CanGenerate(lipid, adduct)) {
                return null;
            }
            if (lipid.GenerateSpectrum(_lipidGenerator, adduct, baseReference) is MoleculeMsReference reference) {
                lock (syncObject) {
                    reference.ScanID = _references.Count;
                    _references.Add(reference);
                }
                return reference;
            }
            return null;
        }

        // ILipidDatabase
        List<MoleculeMsReference> ILipidDatabase.GetReferences() {
            return _references;
        }

        void ILipidDatabase.SetReferences(IEnumerable<MoleculeMsReference> references) {
            var refs = references.ToList();
            var max = refs.DefaultIfEmpty().Max(r => r?.ScanID) ?? 0;
            _references.AddRange(Enumerable.Repeat<MoleculeMsReference>(null, max));
            foreach (var r in refs) {
                _references[r.ScanID] = r;
            }
        }

        // IMatchResultRefer
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => _id;

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            if (result.LibraryID < _references.Count) {
                return _references[result.LibraryID];
            }
            return null;
        }

        // IReferenceDataBase
        string IReferenceDataBase.Id => _id;

        void IReferenceDataBase.Load(Stream stream, string folderpath) {
            var references = MessagePackDefaultHandler.LoadLargerListFromStream<MoleculeMsReference>(stream);
            var pairs = references.Select(reference => (FacadeLipidParser.Default.Parse(reference.Name), reference)).ToList();
            lock (syncObject) {
                _references.Clear();
                _lipidToReference.Clear();

                _references.AddRange(references);
                foreach (var (lipid, reference) in pairs) {
                    _lipidToReference[lipid] = new Lazy<MoleculeMsReference>(() => reference, isThreadSafe: true);
                }
            }
        }

        void IReferenceDataBase.Save(Stream stream) {
            MessagePackDefaultHandler.SaveLargeListToStream(_references, stream);
        }

        // IDisposable
        private bool _disposedValue;

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        ~EadLipidDictionaryDatabase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        class LipidNameComparer : IEqualityComparer<ILipid>
        {
            public bool Equals(ILipid x, ILipid y) {
                return Equals(x?.Name, y?.Name);
            }

            public int GetHashCode(ILipid obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}
