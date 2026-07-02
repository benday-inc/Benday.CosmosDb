using System;
using System.Runtime.Serialization;

namespace Benday.CosmosDb.Exceptions;

/// <summary>
/// Base exception class for all Cosmos DB related exceptions in the library.
/// </summary>
[Serializable]
public class CosmosDbException : Exception
{
    /// <summary>
    /// Initializes a new instance of the CosmosDbException class.
    /// </summary>
    public CosmosDbException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public CosmosDbException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public CosmosDbException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data</param>
    /// <param name="context">The StreamingContext that contains contextual information</param>
    protected CosmosDbException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

/// <summary>
/// Exception thrown when an item is not found in the Cosmos DB container.
/// </summary>
[Serializable]
public class CosmosDbItemNotFoundException : CosmosDbException
{
    /// <summary>
    /// Gets the ID of the item that was not found.
    /// </summary>
    public string ItemId { get; }

    /// <summary>
    /// Gets the container name where the item was expected to be found.
    /// </summary>
    public string? ContainerName { get; }

    /// <summary>
    /// Initializes a new instance of the CosmosDbItemNotFoundException class.
    /// </summary>
    /// <param name="itemId">The ID of the item that was not found</param>
    public CosmosDbItemNotFoundException(string itemId) 
        : base($"Item with ID '{itemId}' was not found.")
    {
        ItemId = itemId;
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbItemNotFoundException class.
    /// </summary>
    /// <param name="itemId">The ID of the item that was not found</param>
    /// <param name="containerName">The name of the container</param>
    public CosmosDbItemNotFoundException(string itemId, string containerName) 
        : base($"Item with ID '{itemId}' was not found in container '{containerName}'.")
    {
        ItemId = itemId;
        ContainerName = containerName;
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbItemNotFoundException class.
    /// </summary>
    /// <param name="itemId">The ID of the item that was not found</param>
    /// <param name="containerName">The name of the container</param>
    /// <param name="innerException">The inner exception</param>
    public CosmosDbItemNotFoundException(string itemId, string containerName, Exception innerException) 
        : base($"Item with ID '{itemId}' was not found in container '{containerName}'.", innerException)
    {
        ItemId = itemId;
        ContainerName = containerName;
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbItemNotFoundException class with serialized data.
    /// </summary>
    protected CosmosDbItemNotFoundException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        ItemId = info.GetString(nameof(ItemId)) ?? string.Empty;
        ContainerName = info.GetString(nameof(ContainerName));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ItemId), ItemId);
        info.AddValue(nameof(ContainerName), ContainerName);
    }
}

/// <summary>
/// Exception thrown when a configuration error occurs.
/// </summary>
[Serializable]
public class CosmosDbConfigurationException : CosmosDbException
{
    /// <summary>
    /// Gets the name of the configuration setting that caused the error.
    /// </summary>
    public string? ConfigurationKey { get; }

    /// <summary>
    /// Initializes a new instance of the CosmosDbConfigurationException class.
    /// </summary>
    /// <param name="message">The error message</param>
    public CosmosDbConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbConfigurationException class.
    /// </summary>
    /// <param name="configurationKey">The configuration key that caused the error</param>
    /// <param name="message">The error message</param>
    public CosmosDbConfigurationException(string configurationKey, string message) 
        : base($"Configuration error for '{configurationKey}': {message}")
    {
        ConfigurationKey = configurationKey;
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbConfigurationException class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public CosmosDbConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbConfigurationException class with serialized data.
    /// </summary>
    protected CosmosDbConfigurationException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        ConfigurationKey = info.GetString(nameof(ConfigurationKey));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ConfigurationKey), ConfigurationKey);
    }
}

/// <summary>
/// Exception thrown when a batch operation fails in Cosmos DB.
/// </summary>
[Serializable]
public class CosmosDbBatchOperationException : CosmosDbException
{
    /// <summary>
    /// Gets the batch number that failed.
    /// </summary>
    public int BatchNumber { get; }

    /// <summary>
    /// Gets the total number of batches.
    /// </summary>
    public int TotalBatches { get; }

    /// <summary>
    /// Gets the number of items in the failed batch.
    /// </summary>
    public int BatchSize { get; }

    /// <summary>
    /// Initializes a new instance of the CosmosDbBatchOperationException class.
    /// </summary>
    /// <param name="batchNumber">The batch number that failed</param>
    /// <param name="totalBatches">The total number of batches</param>
    /// <param name="batchSize">The size of the failed batch</param>
    /// <param name="message">The error message</param>
    public CosmosDbBatchOperationException(int batchNumber, int totalBatches, int batchSize, string message) 
        : base($"Batch {batchNumber}/{totalBatches} (size: {batchSize}) failed: {message}")
    {
        BatchNumber = batchNumber;
        TotalBatches = totalBatches;
        BatchSize = batchSize;
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbBatchOperationException class.
    /// </summary>
    /// <param name="batchNumber">The batch number that failed</param>
    /// <param name="totalBatches">The total number of batches</param>
    /// <param name="batchSize">The size of the failed batch</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public CosmosDbBatchOperationException(int batchNumber, int totalBatches, int batchSize, string message, Exception innerException) 
        : base($"Batch {batchNumber}/{totalBatches} (size: {batchSize}) failed: {message}", innerException)
    {
        BatchNumber = batchNumber;
        TotalBatches = totalBatches;
        BatchSize = batchSize;
    }

    /// <summary>
    /// Initializes a new instance of the CosmosDbBatchOperationException class with serialized data.
    /// </summary>
    protected CosmosDbBatchOperationException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        BatchNumber = info.GetInt32(nameof(BatchNumber));
        TotalBatches = info.GetInt32(nameof(TotalBatches));
        BatchSize = info.GetInt32(nameof(BatchSize));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(BatchNumber), BatchNumber);
        info.AddValue(nameof(TotalBatches), TotalBatches);
        info.AddValue(nameof(BatchSize), BatchSize);
    }
}