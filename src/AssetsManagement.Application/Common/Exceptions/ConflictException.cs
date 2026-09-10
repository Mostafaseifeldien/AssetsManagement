namespace AssetsManagement.Application;

public sealed class ConflictException(string message) : Exception(message);
