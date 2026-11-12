using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebFormsForCore.Tests.Infrastructure;

/// <summary>
/// The real WebForms middleware on Kestrel (loopback, random port) over a temporary site directory.
/// There can only be one HostingEnvironment per process, so there is one shared site for all tests;
/// tests add their own pages with <see cref="Write"/> using unique file names.
/// </summary>
public sealed class WebFormsTestSite : IAsyncDisposable
{
	/// <summary>
	/// Requests carrying this header have Request.Scheme set to its value before they reach WebForms,
	/// which simulates serving over HTTPS (or behind a TLS-terminating proxy).
	/// </summary>
	public const string SchemeHeader = "X-Test-Scheme";

	private static WebFormsTestSite shared;

	public static WebFormsTestSite Shared => shared ?? throw new InvalidOperationException("The shared site is started by TestSetup.");

	private readonly WebApplication app;

	public string RootPath { get; }
	public Uri BaseAddress { get; }
	public HttpClient Client { get; }

	private WebFormsTestSite(WebApplication app, string rootPath, Uri baseAddress)
	{
		this.app = app;
		RootPath = rootPath;
		BaseAddress = baseAddress;
		Client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false })
		{
			BaseAddress = baseAddress,
			Timeout = TimeSpan.FromSeconds(180)
		};
	}

	/// <summary>Writes a file (forward slashes) below the site root.</summary>
	public void Write(string relativePath, string content)
	{
		var full = Path.Combine(RootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(full));
		File.WriteAllText(full, content);
	}

	/// <summary>GETs a path and returns the body; ASP.NET error pages are reduced to their text so that failures are readable.</summary>
	public async Task<string> GetStringAsync(string path, params (string Name, string Value)[] headers)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		foreach (var (name, value) in headers)
		{
			if (name.Equals("Host", StringComparison.OrdinalIgnoreCase)) request.Headers.Host = value;
			else request.Headers.TryAddWithoutValidation(name, value);
		}
		using var response = await Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();
		if (body.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
		{
			body = System.Text.RegularExpressions.Regex.Replace(body, "(?s)<style.*?</style>", "");
			body = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", "");
			body = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(body, @"\s*\n\s*", "\n"));
		}
		return body;
	}

	private const string WebConfig = """
		<?xml version="1.0"?>
		<configuration>
			<system.web>
				<compilation debug="true" />
				<httpRuntime targetFramework="4.8" />
				<httpModules>
					<add name="RequestProbe" type="WebFormsForCore.Tests.Infrastructure.RequestProbeModule, WebFormsForCore.Tests" />
				</httpModules>
			</system.web>
		</configuration>
		""";

	internal static async Task StartSharedAsync()
	{
		var root = Path.Combine(Path.GetTempPath(), "wffc-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(root, "bin"));
		Directory.CreateDirectory(Path.Combine(root, "App_Data"));
		File.WriteAllText(Path.Combine(root, "web.config"), WebConfig);
		File.WriteAllText(Path.Combine(root, "App_Data", "machine.config"), TrimmedMachineConfig());

		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		var app = builder.Build();
		app.Use((context, next) =>
		{
			if (context.Request.Headers.TryGetValue(SchemeHeader, out var scheme))
				context.Request.Scheme = scheme.ToString();
			return next(context);
		});
		app.UseWebForms(o => o.PhysicalPath(root).HandleAllRequestsWithWebForms());
		await app.StartAsync();

		var address = app.Services.GetRequiredService<IServer>()
			.Features.Get<IServerAddressesFeature>().Addresses.First();
		shared = new WebFormsTestSite(app, root, new Uri(address));
	}

	/// <summary>
	/// The default machine.config references System.Web.Extensions, which the tests do not load.
	/// </summary>
	private static string TrimmedMachineConfig()
	{
		var doc = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "machine.config"));
		doc.Descendants()
			.Where(e => e.Attributes().Any(a => a.Value.Contains("System.Web.Extensions", StringComparison.Ordinal)))
			.ToList()
			.ForEach(e => e.Remove());
		return doc.ToString();
	}

	internal static async Task StopSharedAsync()
	{
		var site = shared;
		shared = null;
		if (site == null) return;
		site.Client.Dispose();
		await site.app.StopAsync();
		await site.app.DisposeAsync();
		try { Directory.Delete(site.RootPath, true); } catch { }
	}

	public ValueTask DisposeAsync() => new ValueTask(StopSharedAsync());
}
