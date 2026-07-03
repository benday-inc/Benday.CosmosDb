namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Thrown when a save fails because the entity's ETag no longer matches the stored
/// document, indicating it was modified concurrently by another writer.
/// </summary>
public class OptimisticConcurrencyException : Exception
{
    /// <summary>Initializes a new instance with no message.</summary>
    public OptimisticConcurrencyException()
    {
    }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The error message.</param>
    public OptimisticConcurrencyException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception that caused this error.</param>
    public OptimisticConcurrencyException(string message, Exception? innerException) : base(message, innerException)
    {
    }

}