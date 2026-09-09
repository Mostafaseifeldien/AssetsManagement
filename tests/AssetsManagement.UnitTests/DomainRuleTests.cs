using AssetsManagement.Application;
using AssetsManagement.Domain;

namespace AssetsManagement.UnitTests;

public sealed class DomainRuleTests
{
    [Fact]
    public void Category_cannot_be_its_own_parent()
    {
        var id = Guid.NewGuid();
        Assert.Throws<DomainRuleException>(() => AssetDataRules.EnsureCategoryParentIsValid(id, id));
    }

    [Fact]
    public void Attribute_code_is_immutable_after_values_exist()
    {
        Assert.Throws<DomainRuleException>(() =>
            AssetDataRules.EnsureAttributeCodeCanChange(true, "ram_gb", "memory_gb"));
    }

    [Theory]
    [InlineData(IdentifierStatus.Assigned)]
    [InlineData(IdentifierStatus.Retired)]
    [InlineData(IdentifierStatus.Damaged)]
    [InlineData(IdentifierStatus.Replaced)]
    public void Only_in_stock_identifier_can_be_assigned(IdentifierStatus status)
    {
        Assert.Throws<DomainRuleException>(() => AssetDataRules.EnsureIdentifierCanBeAssigned(status, null));
    }

    [Fact]
    public void Unassigned_in_stock_identifier_can_be_assigned()
    {
        AssetDataRules.EnsureIdentifierCanBeAssigned(IdentifierStatus.Unassigned, null);
    }
}
