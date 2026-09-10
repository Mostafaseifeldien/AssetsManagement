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

    public static void EnsureAssetCanChangeStatus(bool isTerminal, bool hasReinstatementReason)
    {
        if (isTerminal && !hasReinstatementReason)
            throw new DomainRuleException(
                "An asset in a terminal status cannot accept operational transitions without a reinstatement reason.");
    }

    public static void EnsureSingleActiveCustody(bool alreadyHasActive) =>
        _ = alreadyHasActive
            ? throw new DomainRuleException("An asset shall have at most one active custody assignment.")
            : true;

    public static void EnsureRelationshipIsNotSelf(Guid sourceAssetId, Guid targetAssetId)
    {
        if (sourceAssetId == targetAssetId)
            throw new DomainRuleException("An asset cannot be related to itself.");
    }

    public static void EnsureContainsIsAcyclic(bool wouldCycle)
    {
        if (wouldCycle)
            throw new DomainRuleException("A Contains relationship cannot create a cycle.");
    }

    public static void EnsureCustodyIsMutable(string status)
    {
        if (status is CustodyStatuses.Closed or CustodyStatuses.Disputed)
            throw new DomainRuleException("The custody chain is evidentiary and cannot be edited after it is closed.");
    }
}
