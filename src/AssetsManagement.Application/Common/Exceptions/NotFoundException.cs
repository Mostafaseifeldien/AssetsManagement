namespace AssetsManagement.Application;

public sealed class NotFoundException(string message) : Exception(message);
