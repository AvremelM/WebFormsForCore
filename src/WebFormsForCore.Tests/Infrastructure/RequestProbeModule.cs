using System.Collections.Concurrent;
using System.Web;

namespace WebFormsForCore.Tests.Infrastructure;

/// <summary>
/// Records what an HttpModule sees at the start of a request. Only requests with a "probe" query string
/// value are recorded; read the result with <see cref="Get"/> using that value.
/// </summary>
public class RequestProbeModule : IHttpModule
{
	public record Snapshot(string Path, string FilePath, string RawUrl);

	private static readonly ConcurrentDictionary<string, Snapshot> beginRequest = new();

	/// <summary>What the module saw in BeginRequest for the request that had this probe value.</summary>
	public static Snapshot Get(string probe) => beginRequest.TryGetValue(probe, out var snapshot) ? snapshot : null;

	public void Init(HttpApplication application) => application.BeginRequest += (sender, e) =>
	{
		var request = ((HttpApplication)sender).Context.Request;
		var probe = request.QueryString["probe"];
		if (probe != null) beginRequest[probe] = new Snapshot(request.Path, request.FilePath, request.RawUrl);
	};

	public void Dispose()
	{
	}
}
