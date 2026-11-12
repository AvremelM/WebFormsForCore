using System.Threading.Tasks;
using WebFormsForCore.Tests.Infrastructure;

namespace WebFormsForCore.Tests.Hosting;

/// <summary>
/// IIS applies the default document when the handler is mapped, so modules see the requested path
/// (for example "/") in BeginRequest and the page sees the document it was served from.
/// </summary>
[TestFixture]
public class DefaultDocumentTests
{
	private static WebFormsTestSite Site => WebFormsTestSite.Shared;

	[Test]
	public async Task ModuleSeesRequestedDirectoryInBeginRequest()
	{
		Site.Write("defaultdoc/aspx/Default.aspx", "<%@ Page Language=\"C#\" %>page");

		await Site.GetStringAsync("defaultdoc/aspx/?probe=begin-aspx");

		var seen = RequestProbeModule.Get("begin-aspx");
		Assert.That(seen, Is.Not.Null, "The request probe module did not run");
		Assert.That(seen.Path, Is.EqualTo("/defaultdoc/aspx/"));
		Assert.That(seen.FilePath, Is.EqualTo("/defaultdoc/aspx/"));
	}

	[Test]
	public async Task PageIsServedFromDefaultDocument()
	{
		Site.Write("defaultdoc/page/Default.aspx", "<%@ Page Language=\"C#\" %><%= Request.Path %>|<%= Request.FilePath %>");

		var body = await Site.GetStringAsync("defaultdoc/page/");

		Assert.That(body, Is.EqualTo("/defaultdoc/page/Default.aspx|/defaultdoc/page/Default.aspx"));
	}

	[Test]
	public async Task StaticDefaultDocumentIsServed()
	{
		Site.Write("defaultdoc/static/index.htm", "static default");

		Assert.That(await Site.GetStringAsync("defaultdoc/static/"), Is.EqualTo("static default"));
	}

	[Test]
	public async Task ModuleSeesRequestedDirectoryForStaticDefaultDocument()
	{
		Site.Write("defaultdoc/staticprobe/index.htm", "static default");

		await Site.GetStringAsync("defaultdoc/staticprobe/?probe=begin-static");

		Assert.That(RequestProbeModule.Get("begin-static")?.Path, Is.EqualTo("/defaultdoc/staticprobe/"));
	}

	[Test]
	public async Task QueryStringIsKeptForDefaultDocument()
	{
		Site.Write("defaultdoc/query/Default.aspx", "<%@ Page Language=\"C#\" %><%= Request.QueryString[\"x\"] %>");

		Assert.That(await Site.GetStringAsync("defaultdoc/query/?x=1"), Is.EqualTo("1"));
	}
}
