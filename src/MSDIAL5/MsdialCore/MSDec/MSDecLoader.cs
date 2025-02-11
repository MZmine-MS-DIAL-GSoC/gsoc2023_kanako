﻿using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.MSDec {
    public sealed class MSDecLoader : IDisposable, IMsScanPropertyLoader<IChromatogramPeak> {
        private Stream _deconvolutionStream;
        private readonly int _version;
        private readonly List<long> _seekPointers;
        private readonly bool _isAnnotationInfoIncluded;

        public MSDecLoader(Stream fs) {
            if (fs is null || !fs.CanSeek) {
                throw new ArgumentException(nameof(fs));
            }

            _deconvolutionStream = fs;
            MsdecResultsReader.GetSeekPointers(_deconvolutionStream, out _version, out _seekPointers, out _isAnnotationInfoIncluded);
        }

        public MSDecLoader(string deconvolutionFile) : this(FileOpen(deconvolutionFile)) {

        }

        public bool IsDisposed => _disposedValue;

        private static FileStream FileOpen(string deconvolutionFile) {
            if (!File.Exists(deconvolutionFile)) {
                throw new ArgumentException(nameof(deconvolutionFile));
            }

            return File.Open(deconvolutionFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public MSDecResult LoadMSDecResult(int idx) {
            if (_disposedValue) {
                return null;
            }
            return LoadMSDecResultCore(idx);
        }

        private MSDecResult LoadMSDecResultCore(int idx) {
            return MsdecResultsReader.ReadMSDecResult(_deconvolutionStream, _seekPointers[idx], _version, _isAnnotationInfoIncluded);
        }

        // IDisposable
        private bool _disposedValue;

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    _deconvolutionStream.Close();
                    _deconvolutionStream = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        IMSScanProperty IMsScanPropertyLoader<IChromatogramPeak>.Load(IChromatogramPeak source) {
            return LoadMSDecResult(source.ID);
        }
    }
}
