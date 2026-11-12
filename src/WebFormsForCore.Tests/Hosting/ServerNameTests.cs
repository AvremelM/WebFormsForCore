using System.Threading.Tasks;
using WebFormsForCore.Tests.Infrastructure;

namespace WebFormsForCore.Tests.Hosting;

[TestFixture]
public class ServerNameTests
{
	[Test]
	public async Task RequestUrlHostIsTakenFromHostHeader()
	{
		WebFormsTestSite.Shared.Write("servername/url.aspx", "<%@ Page Language=\"C#\" %><%= Request.Url.Host %>");

		var body = await WebFormsTestSite.Shared.GetStringAsync("servername/url.aspx", ("Host", "example.org"));

		Assert.That(body, Is.EqualTo("example.org"));
	}

	[Test]
	public async Task ServerNameVariableIsTakenFromHostHeader()
	{
		WebFormsTestSite.Shared.Write("servername/var.aspx", "<%@ Page Language=\"C#\" %><%= Request.ServerVariables[\"SERVER_NAME\"] %>");

		var body = await WebFormsTestSite.Shared.GetStringAsync("servername/var.aspx", ("Host", "example.org:8080"));

		Assert.That(body, Is.EqualTo("example.org"));
	}
}
