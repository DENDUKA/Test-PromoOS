namespace TaskApi.Common;

public class ConcurrencyConflictException(Exception innerException)
    : Exception("The record was modified by another request.", innerException);
