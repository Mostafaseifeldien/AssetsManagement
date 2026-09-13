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

    public static void EnsureExpenseCanBeEdited(string state)
    {
        if (state == ExpenseStates.Reversed)
            throw new DomainRuleException("A reversed expense cannot be edited.");
    }

    public static void EnsureExpenseCanBeReversed(string state)
    {
        if (state != ExpenseStates.Recorded)
            throw new DomainRuleException("Only a recorded expense can be reversed.");
    }

    public static void EnsureScheduleIsActive(string state)
    {
        if (state is DepreciationStates.Completed or DepreciationStates.Superseded)
            throw new DomainRuleException("A completed or superseded depreciation schedule cannot be changed.");
    }

    public static void EnsureWarrantyIsClaimable(string state)
    {
        if (state is WarrantyStates.Expired or WarrantyStates.Void)
            throw new DomainRuleException("A claim cannot be raised against an expired or void warranty.");
    }

    public static void EnsureRequestCanBeRejected(string state)
    {
        if (state is MaintenanceRequestStates.Converted or MaintenanceRequestStates.Rejected
            or MaintenanceRequestStates.Withdrawn)
            throw new DomainRuleException("This maintenance request can no longer be rejected.");
    }

    public static void EnsureRequestCanBeConverted(string state)
    {
        if (state is MaintenanceRequestStates.Converted or MaintenanceRequestStates.Rejected
            or MaintenanceRequestStates.Withdrawn)
            throw new DomainRuleException("This maintenance request can no longer be converted.");
    }

    public static void EnsureWorkOrderIsOpen(string state)
    {
        if (state is WorkOrderStates.Completed or WorkOrderStates.Verified or WorkOrderStates.Canceled)
            throw new DomainRuleException("A closed work order cannot accept further changes.");
    }

    public static void EnsureExitCanBeDecided(string status)
    {
        if (status is ExitAuthorizationStates.Approved or ExitAuthorizationStates.Active
            or ExitAuthorizationStates.Used or ExitAuthorizationStates.Expired
            or ExitAuthorizationStates.Rejected or ExitAuthorizationStates.Revoked)
            throw new DomainRuleException("This exit authorization is no longer awaiting approval.");
    }
}
