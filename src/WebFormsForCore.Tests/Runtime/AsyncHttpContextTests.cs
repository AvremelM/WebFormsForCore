using System.Linq;
using System.Threading.Tasks;
using WebFormsForCore.Tests.Infrastructure;

namespace WebFormsForCore.Tests.Runtime;

[TestFixture]
public class AsyncHttpContextTests
{
	private static WebFormsTestSite Site => WebFormsTestSite.Shared;

	private const string PageStart = "<%@ Page Language=\"C#\" Async=\"true\" %>\n<script runat=\"server\">\n";

	[Test]
	public async Task HttpContextCurrentIsSetAfterTaskDelayInRegisteredAsyncTask()
	{
		Site.Write("asynccontext/delay.aspx", PageStart + @"
protected void Page_Load(object sender, EventArgs e)
{
	RegisterAsyncTask(new PageAsyncTask(async () =>
	{
		await System.Threading.Tasks.Task.Delay(50);
		Response.Write(System.Web.HttpContext.Current == null ? ""null"" : ""set"");
	}));
}
</script>");

		Assert.That(await Site.GetStringAsync("asynccontext/delay.aspx"), Is.EqualTo("set"));
	}

	[Test]
	public async Task HttpContextCurrentIsSetAfterTaskYieldInRegisteredAsyncTask()
	{
		Site.Write("asynccontext/yield.aspx", PageStart + @"
protected void Page_Load(object sender, EventArgs e)
{
	RegisterAsyncTask(new PageAsyncTask(async () =>
	{
		await System.Threading.Tasks.Task.Yield();
		Response.Write(System.Web.HttpContext.Current == null ? ""null"" : ""set"");
	}));
}
</script>");

		Assert.That(await Site.GetStringAsync("asynccontext/yield.aspx"), Is.EqualTo("set"));
	}

	[Test]
	public async Task HttpContextCurrentIsSetAfterTaskDelayInAsyncPageLoad()
	{
		Site.Write("asynccontext/pageload.aspx", PageStart + @"
protected async void Page_Load(object sender, EventArgs e)
{
	await System.Threading.Tasks.Task.Delay(50);
	Response.Write(System.Web.HttpContext.Current == null ? ""null"" : ""set"");
}
</script>");

		Assert.That(await Site.GetStringAsync("asynccontext/pageload.aspx"), Is.EqualTo("set"));
	}

	[Test]
	public async Task ConcurrentRequestsKeepTheirOwnHttpContextAfterAwait()
	{
		Site.Write("asynccontext/own.aspx", PageStart + @"
protected void Page_Load(object sender, EventArgs e)
{
	RegisterAsyncTask(new PageAsyncTask(async () =>
	{
		var id = Request.QueryString[""id""];
		await System.Threading.Tasks.Task.Delay(100 + int.Parse(id) % 5 * 20);
		var current = System.Web.HttpContext.Current;
		Response.Write(current == null ? ""null"" : current.Request.QueryString[""id""]);
	}));
}
</script>");

		var ids = Enumerable.Range(0, 12).ToArray();
		var bodies = await Task.WhenAll(ids.Select(id => Site.GetStringAsync("asynccontext/own.aspx?id=" + id)));

		Assert.That(bodies, Is.EqualTo(ids.Select(id => id.ToString()).ToArray()));
	}
}
