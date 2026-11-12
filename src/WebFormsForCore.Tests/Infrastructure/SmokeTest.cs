using System.Threading.Tasks;

namespace WebFormsForCore.Tests.Infrastructure;

[TestFixture]
public class SmokeTest
{
	[Test]
	public void WebAssemblyIsOurs()
	{
		var asm = typeof(System.Web.HttpContext).Assembly;
		Assert.That(asm.Location, Does.Not.Contain("shared"));
	}

	[Test]
	public async Task ServesStaticFile()
	{
		WebFormsTestSite.Shared.Write("smoke/hello.htm", "<p>hi</p>");
		var response = await WebFormsTestSite.Shared.Client.GetAsync("smoke/hello.htm");
		Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("<p>hi</p>"));
	}

	[Test]
	public async Task RunsAspxPage()
	{
		WebFormsTestSite.Shared.Write("smoke/sum.aspx", "<%@ Page Language=\"C#\" %><%= 1 + 2 %>");
		Assert.That(await WebFormsTestSite.Shared.GetStringAsync("smoke/sum.aspx"), Is.EqualTo("3"));
	}
}
