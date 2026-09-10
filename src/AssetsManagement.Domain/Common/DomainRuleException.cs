namespace AssetsManagement.Domain;

public sealed class DomainRuleException(string message) : Exception(message);
