using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WebFormsForCore.Tests.Infrastructure;

namespace WebFormsForCore.Tests.Hosting;

/// <summary>
/// HttpResponse.Headers requires the IIS integrated pipeline on .NET Framework. The ASP.NET Core host runs the
/// classic pipeline, where it threw PlatformNotSupportedException, so it was unusable. It is now a view over
/// the headers of the response that writes through.
/// </summary>
[TestFixture]
public class ResponseHeadersTests
{
	private static WebFormsTestSite Site => WebFormsTestSite.Shared;

	private static async Task<IReadOnlyList<string>> GetHeaderValues(string path, string header)
	{
		using var response = await Site.Client.GetAsync(path);
		var body = await response.Content.ReadAsStringAsync();
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
		return response.Headers.TryGetValues(header, out var values) ? values.ToList() : new List<string>();
	}

	[Test]
	public async Task AddedHeaderIsSent()
	{
		Site.Write("responseheaders/add.aspx", "<%@ Page Language=\"C#\" %><% Response.Headers.Add(\"X-Test-Add\", \"a\"); %>");

		Assert.That(await GetHeaderValues("responseheaders/add.aspx", "X-Test-Add"), Is.EqualTo(new[] { "a" }));
	}

	[Test]
	public async Task IndexerSetReplacesExistingHeader()
	{
		Site.Write("responseheaders/set.aspx", "<%@ Page Language=\"C#\" %><% Response.AppendHeader(\"X-Test-Set\", \"old\"); Response.Headers[\"X-Test-Set\"] = \"new\"; %>");

		Assert.That(await GetHeaderValues("responseheaders/set.aspx", "X-Test-Set"), Is.EqualTo(new[] { "new" }));
	}

	[Test]
	public async Task RemoveDeletesHeader()
	{
		Site.Write("responseheaders/remove.aspx", "<%@ Page Language=\"C#\" %><% Response.AppendHeader(\"X-Test-Remove\", \"x\"); Response.Headers.Remove(\"X-Test-Remove\"); %>");

		Assert.That(await GetHeaderValues("responseheaders/remove.aspx", "X-Test-Remove"), Is.Empty);
	}

	[Test]
	public async Task HeadersAppendedWithAppendHeaderAreReadable()
	{
		Site.Write("responseheaders/read.aspx", "<%@ Page Language=\"C#\" %><% Response.AppendHeader(\"X-Test-Read\", \"value\"); Response.Write(Response.Headers[\"X-Test-Read\"]); %>");

		Assert.That(await Site.GetStringAsync("responseheaders/read.aspx"), Is.EqualTo("value"));
	}

	[Test]
	public async Task AddingHeaderAfterHeadersWereSentThrowsHttpException()
	{
		Site.Write("responseheaders/late.aspx", """
			<%@ Page Language="C#" %><%
			Response.Write("x");
			Response.Flush();
			try { Response.Headers.Add("X-Test-Late", "a"); Response.Write("no exception"); }
			catch (HttpException) { Response.Write("HttpException"); }
			%>
			""");

		Assert.That(await Site.GetStringAsync("responseheaders/late.aspx"), Is.EqualTo("xHttpException"));
	}
}
