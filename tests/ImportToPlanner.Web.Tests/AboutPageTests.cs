using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.About.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class AboutPageTests
{
    private const string DocsBaseUrl = "https://docs.test.importplanner.app";

    [Fact]
    public async Task AboutPage_WhenUnsigned_ShowsSignInPromptAndNoReleaseLabel()
    {
        var releaseStub = new ReleaseInformationQueryStub();

        await using var ctx = new HomePageTestContext(
            isAuthenticated: false,
            releaseInformationQueryStub: releaseStub,
            docsBaseUrl: DocsBaseUrl);

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sign in to view release information for this deployment.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Sign in", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(releaseStub.ReleaseInformation.ReleaseLabel.DisplayValue, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Documentation and support", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task AboutPage_WhenSignedIn_WithoutBuildMetadata_OmitsOptionalMetadataRows()
    {
        var releaseStub = new ReleaseInformationQueryStub(buildMetadata: null);

        await using var ctx = new HomePageTestContext(
            isAuthenticated: true,
            releaseInformationQueryStub: releaseStub,
            docsBaseUrl: DocsBaseUrl);

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(releaseStub.ReleaseInformation.ReleaseLabel.DisplayValue, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Built (UTC)", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Source revision", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task AboutPage_WhenSignedIn_WithBuildMetadata_ShowsOptionalMetadataRows()
    {
        var buildMetadata = new BuildMetadata(
            new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero),
            "abc1234",
            SourceRevisionUrl: null);
        var releaseStub = new ReleaseInformationQueryStub(buildMetadata);

        await using var ctx = new HomePageTestContext(
            isAuthenticated: true,
            releaseInformationQueryStub: releaseStub,
            docsBaseUrl: DocsBaseUrl);

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Built (UTC)", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("2026-09-30 12:00:00 UTC", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Source revision", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("abc1234", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task AboutPage_WhenSignedIn_ShowsReleaseLabel()
    {
        var releaseStub = new ReleaseInformationQueryStub();

        await using var ctx = new HomePageTestContext(
            isAuthenticated: true,
            releaseInformationQueryStub: releaseStub,
            docsBaseUrl: DocsBaseUrl);

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(releaseStub.ReleaseInformation.ReleaseLabel.DisplayValue, cut.Markup, StringComparison.Ordinal);
            Assert.Contains(releaseStub.ReleaseInformation.ProductName, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Sign in to view release information for this deployment.", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task AboutPage_WhenSignedIn_ExternalLinksMatchDocsExternalLinksOptionsPaths()
    {
        await using var ctx = new HomePageTestContext(
            isAuthenticated: true,
            docsBaseUrl: DocsBaseUrl);

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            var anchors = cut.FindAll("a");
            Assert.Contains(anchors, anchor => anchor.GetAttribute("href") == $"{DocsBaseUrl}/");
            Assert.Contains(anchors, anchor => anchor.GetAttribute("href") == $"{DocsBaseUrl}/terms");
            Assert.Contains(anchors, anchor => anchor.GetAttribute("href") == $"{DocsBaseUrl}/privacy-and-security");
            Assert.Contains(anchors, anchor => anchor.GetAttribute("href") == $"{DocsBaseUrl}/support");
        });
    }

    [Fact]
    public async Task AboutPage_ExposesProductTitleHeadingAndFocusableDocumentationAnchors()
    {
        await using var ctx = new HomePageTestContext(
            isAuthenticated: true,
            docsBaseUrl: DocsBaseUrl);

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            var titleHeadings = cut.FindAll("h1")
                .Concat(cut.FindAll("h4"))
                .Where(element => element.TextContent.Contains("About Import To Planner", StringComparison.Ordinal))
                .ToList();

            Assert.NotEmpty(titleHeadings);

            var documentationAnchors = cut.FindAll("a")
                .Where(anchor =>
                    string.Equals(anchor.TextContent.Trim(), "Documentation", StringComparison.Ordinal)
                    || string.Equals(anchor.TextContent.Trim(), "Terms of use", StringComparison.Ordinal)
                    || string.Equals(anchor.TextContent.Trim(), "Privacy and security", StringComparison.Ordinal)
                    || string.Equals(anchor.TextContent.Trim(), "Support", StringComparison.Ordinal))
                .ToList();

            Assert.Equal(4, documentationAnchors.Count);
            Assert.All(documentationAnchors, anchor =>
            {
                Assert.False(string.IsNullOrWhiteSpace(anchor.GetAttribute("href")));
                Assert.NotEqual("-1", anchor.GetAttribute("tabindex"));
            });
        });
    }
}
