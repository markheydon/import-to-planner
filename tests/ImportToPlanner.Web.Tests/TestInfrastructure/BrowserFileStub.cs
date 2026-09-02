using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class BrowserFileStub : IBrowserFile
{
    private readonly byte[] contentBytes;

    public BrowserFileStub(string name, string content)
    {
        Name = name;
        contentBytes = Encoding.UTF8.GetBytes(content);
    }

    public string Name { get; }

    public DateTimeOffset LastModified => DateTimeOffset.UtcNow;

    public long Size => contentBytes.Length;

    public string ContentType => "text/csv";

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (contentBytes.Length > maxAllowedSize)
        {
            throw new IOException("The selected file exceeds the maximum allowed size.");
        }

        return new MemoryStream(contentBytes);
    }
}
