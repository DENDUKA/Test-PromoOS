namespace TaskApi.Domain.Common;

/// <summary>Нарушение бизнес-инварианта домена. Api-слой превращает его в 409/400.</summary>
public abstract class DomainException(string message) : Exception(message);
