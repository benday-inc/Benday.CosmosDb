namespace Benday.CosmosDb.MigrationTool;

/// <summary>
/// Dynamically adjusts write concurrency based on 429 (throttle) responses from Cosmos DB.
///
/// Strategy:
///   - After each batch, report how many 429s occurred.
///   - If zero 429s for N consecutive batches, increase concurrency.
///   - If any 429s, decrease concurrency immediately.
///   - Concurrency is clamped between a floor and ceiling.
/// </summary>
public class AdaptiveConcurrencyController
{
    private readonly int _floor;
    private readonly int _ceiling;
    private readonly int _rampUpAfterBatches;
    private readonly int _rampUpStep;
    private readonly double _rampDownFactor;
    private readonly Action<string> _writeLine;

    private int _currentConcurrency;
    private int _consecutiveCleanBatches;
    private SemaphoreSlim _semaphore;

    public int CurrentConcurrency => _currentConcurrency;

    public AdaptiveConcurrencyController(
        int initialConcurrency,
        Action<string> writeLine,
        int floor = 5,
        int ceiling = 200,
        int rampUpAfterBatches = 3,
        int rampUpStep = 10,
        double rampDownFactor = 0.5)
    {
        _floor = floor;
        _ceiling = ceiling;
        _rampUpAfterBatches = rampUpAfterBatches;
        _rampUpStep = rampUpStep;
        _rampDownFactor = rampDownFactor;
        _writeLine = writeLine;

        _currentConcurrency = Math.Clamp(initialConcurrency, _floor, _ceiling);
        _consecutiveCleanBatches = 0;
        _semaphore = new SemaphoreSlim(_currentConcurrency, _currentConcurrency);
    }

    public async Task WaitAsync() => await _semaphore.WaitAsync();

    public void Release() => _semaphore.Release();

    /// <summary>
    /// Call after each batch completes. Pass the number of 429 errors observed.
    /// Returns true if concurrency was changed.
    /// </summary>
    public bool ReportBatchResult(int throttleCount)
    {
        if (throttleCount > 0)
        {
            _consecutiveCleanBatches = 0;
            var newConcurrency = Math.Max(_floor, (int)(_currentConcurrency * _rampDownFactor));

            if (newConcurrency != _currentConcurrency)
            {
                _writeLine($"  [THROTTLE] {throttleCount} x 429 detected — concurrency {_currentConcurrency} -> {newConcurrency}");
                AdjustSemaphore(newConcurrency);
                return true;
            }
        }
        else
        {
            _consecutiveCleanBatches++;

            if (_consecutiveCleanBatches >= _rampUpAfterBatches && _currentConcurrency < _ceiling)
            {
                var newConcurrency = Math.Min(_ceiling, _currentConcurrency + _rampUpStep);
                _writeLine($"  [RAMP UP] {_consecutiveCleanBatches} clean batches — concurrency {_currentConcurrency} -> {newConcurrency}");
                _consecutiveCleanBatches = 0;
                AdjustSemaphore(newConcurrency);
                return true;
            }
        }

        return false;
    }

    private void AdjustSemaphore(int newConcurrency)
    {
        // Replace the semaphore. Outstanding WaitAsync calls on the old semaphore
        // will still complete normally — we just stop issuing new waits against it.
        // This is safe because we only call this between batches, not mid-batch.
        _currentConcurrency = newConcurrency;
        _semaphore = new SemaphoreSlim(newConcurrency, newConcurrency);
    }
}
