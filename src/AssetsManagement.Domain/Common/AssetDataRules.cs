namespace AssetsManagement.Domain;

public static class AssetDataRules
{
    public static void EnsureCategoryParentIsValid(Guid categoryId, Guid? parentId) =>
        _ = parentId == categoryId
            ? throw new DomainRuleException("A category cannot be its own parent.")
            : true;

    public static void EnsureAttributeCodeCanChange(bool hasRecordedValues, string oldCode, string newCode)
    {
        if (hasRecordedValues && !string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("The attribute code is immutable after a value has been recorded.");
    }

    public static void EnsureIdentifierCanBeAssigned(IdentifierStatus status, Guid? assetId)
    {
        if (status != IdentifierStatus.Unassigned || assetId is not null)
            throw new DomainRuleException("Only an unassigned identifier in stock can be assigned.");
    }
}
