using ImportToPlanner.Application;
using ImportToPlanner.Application.Services;

namespace ImportToPlanner.Tests;

public sealed class ReleaseLabelFormatterTests
{
    private readonly ReleaseLabelFormatter formatter = new();
    private static readonly ReleaseLabelPolicy DefaultPolicy = new();

    [Fact]
    public void Format_OfficialStableVersion_ReturnsVPrefixedDisplayAndOfficialFlag()
    {
        var label = formatter.Format("1.0.0", DefaultPolicy, builtFromOfficialReleaseTag: true);

        Assert.Equal("v1.0.0", label.DisplayValue);
        Assert.True(label.IsOfficialReleaseTag);
        Assert.Equal("1.0.0", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_VPrefixedOfficialTag_StripsPrefixForNormalisedSemVer()
    {
        var label = formatter.Format("v1.2.3", DefaultPolicy, builtFromOfficialReleaseTag: true);

        Assert.Equal("v1.2.3", label.DisplayValue);
        Assert.True(label.IsOfficialReleaseTag);
        Assert.Equal("1.2.3", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_OfficialTagWithBuildMetadata_KeepsMetadataOnNormalisedSemVer()
    {
        var label = formatter.Format("1.0.0+abc1234", DefaultPolicy, builtFromOfficialReleaseTag: true);

        Assert.Equal("v1.0.0", label.DisplayValue);
        Assert.True(label.IsOfficialReleaseTag);
        Assert.Equal("1.0.0+abc1234", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_PreReleaseVersion_ShowsFullSemVerWithoutOfficialFlag()
    {
        var label = formatter.Format("1.0.0-preview.3+abc", DefaultPolicy);

        Assert.Equal("1.0.0-preview.3+abc", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
        Assert.Equal("1.0.0-preview.3+abc", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_MainBranchStylePreRelease_PreservesGitHeightIdentifier()
    {
        var label = formatter.Format("1.0.0-preview.5", DefaultPolicy);

        Assert.Equal("1.0.0-preview.5", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
        Assert.Equal("1.0.0-preview.5", label.NormalisedSemVer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_MissingOrEmptyInformationalVersion_ReturnsLocalFallback(string? informationalVersion)
    {
        var label = formatter.Format(informationalVersion, DefaultPolicy);

        Assert.Equal("0.0.0-local", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
        Assert.Equal("0.0.0-local", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_UnparseableVersion_ReturnsHonestLocalFallback()
    {
        var label = formatter.Format("not-semver", DefaultPolicy);

        Assert.Equal("0.0.0-local", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
        Assert.Equal("0.0.0-local", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_IncompleteVersion_ReturnsHonestLocalFallback()
    {
        var label = formatter.Format("1.0", DefaultPolicy);

        Assert.Equal("0.0.0-local", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
    }

    [Fact]
    public void Format_AllowUnqualifiedShippingLabelTrue_StillTreatsStableTagAsOfficial()
    {
        var policy = new ReleaseLabelPolicy { AllowUnqualifiedShippingLabel = true };

        var label = formatter.Format("2.4.1", policy);

        Assert.Equal("v2.4.1", label.DisplayValue);
        Assert.True(label.IsOfficialReleaseTag);
    }

    [Fact]
    public void Format_AllowUnqualifiedShippingLabelFalse_PreReleaseInputUnchanged()
    {
        var policy = new ReleaseLabelPolicy { AllowUnqualifiedShippingLabel = false };

        var label = formatter.Format("1.0.0-ci.42", policy);

        Assert.Equal("1.0.0-ci.42", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
    }

    [Fact]
    public void Format_StableWithoutPreRelease_DefaultPolicy_AppendsLocalPreRelease()
    {
        var label = formatter.Format("1.0.0", DefaultPolicy);

        Assert.Equal("1.0.0-local", label.DisplayValue);
        Assert.False(label.IsOfficialReleaseTag);
        Assert.Equal("1.0.0-local", label.NormalisedSemVer);
    }

    [Fact]
    public void Format_NullPolicy_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => formatter.Format("1.0.0", null!));
    }
}
