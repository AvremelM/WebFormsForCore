using System;
using System.Linq;
using System.Web;

namespace WebFormsForCore.Tests.TypeForwarding;

/// <summary>
/// Assemblies compiled against .NET Framework reference these types in System.Web.
/// .NET moved them to System.Web.HttpUtility, so System.Web has to forward them.
/// </summary>
[TestFixture]
public class TypeForwarderTests
{
	private static readonly System.Reflection.Assembly systemWeb = typeof(HttpContext).Assembly;

	[Test]
	public void HttpUtilityIsForwarded()
	{
		Assert.That(systemWeb.GetForwardedTypes(), Does.Contain(typeof(HttpUtility)));
	}

	[Test]
	public void IHtmlStringIsForwarded()
	{
		Assert.That(systemWeb.GetForwardedTypes(), Does.Contain(typeof(IHtmlString)));
	}

	[Test]
	public void IHtmlStringResolvesFromSystemWebAssemblyName()
	{
		var type = Type.GetType("System.Web.IHtmlString, " + systemWeb.FullName, throwOnError: false);

		Assert.That(type, Is.EqualTo(typeof(IHtmlString)));
	}
}
