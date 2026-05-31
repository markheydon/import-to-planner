namespace ImportToPlanner.Tests;

public sealed class ArchitectureComplianceTests
{
    [Fact]
    public void DomainAndApplication_DoNotReferenceProviderOrUiPackages()
    {
        // This guards clean architecture boundaries in inner layers.
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var files = Directory.EnumerateFiles(Path.Combine(rootPath, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                path.Contains("ImportToPlanner.Domain", StringComparison.Ordinal)
                || path.Contains("ImportToPlanner.Application", StringComparison.Ordinal))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));

        var forbiddenTokens = new[]
        {
            "Microsoft.Graph",
            "Microsoft.Kiota",
            "MudBlazor",
            "CsvHelper",
            "PlannerGraph",
            "Microsoft.AspNetCore.Http",
            "System.Security.Claims",
            "Azure.Data.Tables",
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                Assert.DoesNotContain(token, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Application_ContainsTenantBoundaryAbstractionsForHostedMode()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var currentTenantAccessorPath = Path.Combine(
            rootPath,
            "src",
            "ImportToPlanner.Application",
            "Abstractions",
            "ICurrentTenantContextAccessor.cs");
        var tenantMetadataStorePath = Path.Combine(
            rootPath,
            "src",
            "ImportToPlanner.Application",
            "Abstractions",
            "ITenantOperationalMetadataStore.cs");

        Assert.True(File.Exists(currentTenantAccessorPath));
        Assert.True(File.Exists(tenantMetadataStorePath));
    }

    [Fact]
    public void Application_DoesNotContainCommercialAccountBoundaryContracts()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var applicationRoot = Path.Combine(rootPath, "src", "ImportToPlanner.Application");

        var forbiddenPaths = new[]
        {
            Path.Combine(applicationRoot, "Models", "SessionIdentityContext.cs"),
            Path.Combine(applicationRoot, "Models", "CommercialAccount.cs"),
            Path.Combine(applicationRoot, "Models", "CommercialAccessDecision.cs"),
            Path.Combine(applicationRoot, "Models", "CommercialAccountRestoreResult.cs"),
            Path.Combine(applicationRoot, "Models", "CommercialApiContracts.cs"),
            Path.Combine(applicationRoot, "Models", "AccountAuditEvent.cs"),
            Path.Combine(applicationRoot, "Abstractions", "ICommercialAccountStore.cs"),
            Path.Combine(applicationRoot, "Abstractions", "ICommercialAuditStore.cs"),
            Path.Combine(applicationRoot, "Abstractions", "ICommercialAccessUseCase.cs"),
            Path.Combine(applicationRoot, "Abstractions", "ICommercialProfileUseCase.cs"),
        };

        foreach (var forbiddenPath in forbiddenPaths)
        {
            Assert.False(File.Exists(forbiddenPath));
        }
    }

    [Fact]
    public void CommercialService_ContainsCommercialAccountBoundaryContracts()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var contractsRoot = Path.Combine(rootPath, "src", "ImportToPlanner.CommercialService");

        var requiredPaths = new[]
        {
            Path.Combine(contractsRoot, "Models", "SessionIdentityContext.cs"),
            Path.Combine(contractsRoot, "Models", "CommercialAccount.cs"),
            Path.Combine(contractsRoot, "Models", "CommercialAccessDecision.cs"),
            Path.Combine(contractsRoot, "Models", "CommercialAccountRestoreResult.cs"),
            Path.Combine(contractsRoot, "Models", "CommercialApiContracts.cs"),
            Path.Combine(contractsRoot, "Models", "AccountAuditEvent.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialAccountStore.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialAuditStore.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialAccessUseCase.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialProfileUseCase.cs"),
        };

        foreach (var requiredPath in requiredPaths)
        {
            Assert.True(File.Exists(requiredPath));
        }
    }

    [Fact]
    public void CommercialAbstractionsProject_DoesNotExist()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var contractsRoot = Path.Combine(rootPath, "src", "ImportToPlanner.CommercialAbstractions");
        Assert.False(Directory.Exists(contractsRoot));
    }

    [Fact]
    public void CommercialAccountContracts_AreProviderNeutral()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var contractsRoot = Path.Combine(rootPath, "src", "ImportToPlanner.CommercialService");
        var commercialFiles = new[]
        {
            Path.Combine(contractsRoot, "Models", "SessionIdentityContext.cs"),
            Path.Combine(contractsRoot, "Models", "CommercialAccount.cs"),
            Path.Combine(contractsRoot, "Models", "CommercialAccessDecision.cs"),
            Path.Combine(contractsRoot, "Models", "AccountAuditEvent.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialAccountStore.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialAuditStore.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialAccessUseCase.cs"),
            Path.Combine(contractsRoot, "CommercialAccounts", "ICommercialProfileUseCase.cs"),
        };

        var forbiddenTokens = new[]
        {
            "Azure.Data.Tables",
            "Microsoft.Graph",
            "Microsoft.AspNetCore",
            "System.Security.Claims",
            "ImportToPlanner.Web",
            "ImportToPlanner.Infrastructure",
            "Features:CommercialMode",
        };

        foreach (var commercialFile in commercialFiles)
        {
            var content = File.ReadAllText(commercialFile);
            foreach (var forbiddenToken in forbiddenTokens)
            {
                Assert.DoesNotContain(forbiddenToken, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void MaintainedSource_DoesNotContainRemovedRuntimeModeConcepts()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(rootPath, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains("ImportToPlanner.Application", StringComparison.Ordinal)
                || path.Contains("ImportToPlanner.Domain", StringComparison.Ordinal)
                || path.Contains("ImportToPlanner.Web", StringComparison.Ordinal))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal)
                && !path.Contains("/obj/", StringComparison.Ordinal)
                && !path.EndsWith("StartupConfigurationValidator.cs", StringComparison.Ordinal));

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("DeploymentModeConfiguration", content, StringComparison.Ordinal);
            Assert.DoesNotContain("enum DeploymentMode", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WebWorkflowCoordination_DoesNotReferenceMudBlazorTypes()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var workflowPath = Path.Combine(rootPath, "src", "ImportToPlanner.Web", "Features", "Import", "Workflows");
        var workflowFiles = Directory.EnumerateFiles(workflowPath, "*.cs", SearchOption.TopDirectoryOnly);

        foreach (var file in workflowFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("MudBlazor", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void HomePageGuidanceFlags_DoNotDependOnStatusMessageStringScanning()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var homePagePath = Path.Combine(rootPath, "src", "ImportToPlanner.Web", "Features", "Import", "Pages", "Home", "Home.razor");
        var content = File.ReadAllText(homePagePath);

        Assert.DoesNotContain("statusMessage.Contains(", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HomePagePresentationContracts_AreWebOwnedAndPresentationFocused()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var homePath = Path.Combine(rootPath, "src", "ImportToPlanner.Web", "Features", "Import", "Pages", "Home");
        var statePath = Path.Combine(homePath, "HomeWorkflowStepState.cs");
        var presentationPath = Path.Combine(homePath, "HomeWorkflowStepPresentation.cs");

        Assert.True(File.Exists(statePath));
        Assert.True(File.Exists(presentationPath));

        var stateContent = File.ReadAllText(statePath);
        var presentationContent = File.ReadAllText(presentationPath);

        Assert.Contains("namespace ImportToPlanner.Web.Features.Import.Pages", stateContent, StringComparison.Ordinal);
        Assert.Contains("namespace ImportToPlanner.Web.Features.Import.Pages", presentationContent, StringComparison.Ordinal);
        Assert.Contains("enum HomeWorkflowStepState", stateContent, StringComparison.Ordinal);
        Assert.Contains("record HomeWorkflowStepPresentation", presentationContent, StringComparison.Ordinal);
    }

    [Fact]
    public void HomePage_ContainsConciseManualFollowUpGuidanceWithGoalsExample()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var homePagePath = Path.Combine(rootPath, "src", "ImportToPlanner.Web", "Features", "Import", "Pages", "Home", "Home.razor");
        var content = File.ReadAllText(homePagePath);

        Assert.Contains("manual follow-up", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("confirming goals", content, StringComparison.OrdinalIgnoreCase);
    }
}
