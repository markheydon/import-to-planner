using System.Reflection;
using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Application.TenantContext.Models;

namespace ImportToPlanner.Tests;

public sealed class ConsentResolutionTests
{
    [Fact]
    public void GrantedFactory_ReturnsGrantedStatusAndScopes()
    {
        var resolution = ConsentResolution.Granted(["Tasks.ReadWrite", "Group.Read.All"]);

        Assert.Equal(ConsentResolutionStatus.Granted, resolution.Status);
        Assert.Equal(2, resolution.RequiredScopes.Count);
        Assert.Equal("consent.granted", resolution.MessageKey);
    }

    [Fact]
    public void TenantOperationalMetadata_ModelDoesNotExposeImportHistoryFields()
    {
        var disallowedMembers = new[] { "Csv", "Preview", "Report", "History", "Payload" };
        var publicMembers = typeof(TenantOperationalMetadata)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(member => member.Name)
            .ToArray();

        foreach (var disallowed in disallowedMembers)
        {
            Assert.DoesNotContain(publicMembers, member => member.Contains(disallowed, StringComparison.OrdinalIgnoreCase));
        }
    }
}
