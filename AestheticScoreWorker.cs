using PhotoScore.DotNet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaPictureViewer
{
    public sealed class AestheticScoreWorker : IDisposable
    {
        private readonly SemaphoreSlim _scoreLock = new SemaphoreSlim(1, 1);
        private AestheticScorer _scorer;
        private bool _disposed;

        public async Task<double> ScoreAsync(string imagePath, string requestId)
        {
            await _scoreLock.WaitAsync().ConfigureAwait(false);
            try
            {
                return await Task.Run(() =>
                {
                    ThrowIfDisposed();
                    var scorer = EnsureScorer();
                    return scorer.Score(imagePath).Score;
                }).ConfigureAwait(false);
            }
            finally
            {
                _scoreLock.Release();
            }
        }

        private AestheticScorer EnsureScorer()
        {
            if (_scorer != null)
            {
                return _scorer;
            }

            var modelPath = Environment.GetEnvironmentVariable("PHOTOSCORE_ONNX_MODEL") ?? string.Empty;
            var device = Environment.GetEnvironmentVariable("PHOTOSCORE_DEVICE") ?? "auto";
            _scorer = new AestheticScorer(modelPath, device);
            return _scorer;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AestheticScoreWorker));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _scorer?.Dispose();
            _scoreLock.Dispose();
        }
    }
}
