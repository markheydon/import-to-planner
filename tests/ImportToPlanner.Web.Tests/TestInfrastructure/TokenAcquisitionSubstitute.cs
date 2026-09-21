using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class TokenAcquisitionSubstitute
{
    public TokenAcquisitionSubstitute(Exception? exceptionToThrow = null)
    {
        Instance = Substitute.For<ITokenAcquisition>();
        Instance.GetAccessTokenForUserAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<TokenAcquisitionOptions?>())
            .Returns(callInfo =>
            {
                CapturedAuthenticationScheme = callInfo.ArgAt<string?>(1);
                CapturedUser = callInfo.ArgAt<ClaimsPrincipal?>(4);

                if (exceptionToThrow is not null)
                {
                    return Task.FromException<string>(exceptionToThrow);
                }

                return Task.FromResult("token");
            });

        Instance.GetAuthenticationResultForUserAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<TokenAcquisitionOptions?>())
            .Returns(_ => Task.FromException<AuthenticationResult>(new NotSupportedException()));

        Instance.GetAccessTokenForAppAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<TokenAcquisitionOptions?>())
            .Returns(_ => Task.FromResult("app-token"));

        Instance.GetAuthenticationResultForAppAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<TokenAcquisitionOptions?>())
            .Returns(_ => Task.FromException<AuthenticationResult>(new NotSupportedException()));

        Instance.GetEffectiveAuthenticationScheme(Arg.Any<string?>())
            .Returns(callInfo => callInfo.Arg<string?>() ?? string.Empty);
    }

    public ITokenAcquisition Instance { get; }

    public ClaimsPrincipal? CapturedUser { get; private set; }

    public string? CapturedAuthenticationScheme { get; private set; }
}
