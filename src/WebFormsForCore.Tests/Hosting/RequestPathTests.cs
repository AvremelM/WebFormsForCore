using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WebFormsForCore.Tests.Infrastructure;

namespace WebFormsForCore.Tests.Hosting;

/// <summary>
/// Kestrel hands over the request path already decoded. The worker request has to keep it decoded
/// (as IIS does) and must not escape it again, only RawUrl is the raw form.
/// </summary>
[TestFixture]
public class RequestPathTests
{
	private static WebFormsTestSite Site => WebFormsTestSite.Shared;

	[Test]
	public async Task StaticFileWithSpaceInNameIsServed()
	{
		Site.Write("paths/a b.htm", "with space");

		Assert.That(await Site.GetStringAsync("paths/a%20b.htm"), Is.EqualTo("with space"));
	}

	[Test]
	public async Task StaticFileWithNonAsciiNameIsServed()
	{
		Site.Write("paths/א.htm", "alef");

		Assert.That(await Site.GetStringAsync("paths/%D7%90.htm"), Is.EqualTo("alef"));
	}

	[Test]
	public async Task PageWithSpaceInNameIsServed()
	{
		Site.Write("paths/c d.aspx", "<%@ Page Language=\"C#\" %><%= Request.Path %>");

		Assert.That(await Site.GetStringAsync("paths/c%20d.aspx"), Is.EqualTo("/paths/c d.aspx"));
	}

	[Test]
	public async Task RequestFilePathIsDecoded()
	{
		Site.Write("paths/g h.aspx", "<%@ Page Language=\"C#\" %><%= Request.FilePath %>");

		Assert.That(await Site.GetStringAsync("paths/g%20h.aspx"), Is.EqualTo("/paths/g h.aspx"));
	}

	[Test]
	public async Task RawUrlStaysEscaped()
	{
		Site.Write("paths/e f.aspx", "<%@ Page Language=\"C#\" %><%= Request.RawUrl %>");

		Assert.That(await Site.GetStringAsync("paths/e%20f.aspx?x=a%20b"), Is.EqualTo("/paths/e%20f.aspx?x=a%20b"));
	}

	[Test]
	public async Task QueryStringIsStillDecodedByRequest()
	{
		Site.Write("paths/query.aspx", "<%@ Page Language=\"C#\" %><%= Request.QueryString[\"x\"] %>");

		Assert.That(await Site.GetStringAsync("paths/query.aspx?x=a%20b"), Is.EqualTo("a b"));
	}

	// ASP.NET rejects these in Request.Path by default (httpRuntime requestPathInvalidCharacters).
	[TestCase("paths/percent%25.htm")]
	[TestCase("paths/colon:.htm")]
	[TestCase("paths/back%5Cslash.htm")]
	public async Task PathWithInvalidCharacterIsRejected(string path)
	{
		using var response = await Site.Client.GetAsync(path);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
	}
}
