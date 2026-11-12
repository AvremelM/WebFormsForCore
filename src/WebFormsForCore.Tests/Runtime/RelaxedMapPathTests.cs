using System;
using System.IO;
using System.Reflection;
using System.Web;

namespace WebFormsForCore.Tests.Runtime;

/// <summary>
/// httpRuntime relaxedUrlToFileSystemMapping="true" replaces every mapped path that is not a valid
/// file system path by a placeholder. It is only readable from the application config, so these tests
/// switch it on through the static field that RelaxMapPathIfRequired sets.
/// </summary>
[TestFixture]
public class RelaxedMapPathTests
{
	private const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

	private static readonly FieldInfo placeholderField =
		typeof(HttpRuntime).GetField("_DefaultPhysicalPathOnMapPathFailure", NonPublicStatic);

	private static readonly MethodInfo getRelaxedMapPathResult =
		typeof(HttpRuntime).GetMethod("GetRelaxedMapPathResult", NonPublicStatic);

	private static readonly string placeholder = Path.Combine(Path.GetTempPath(), "NOT_A_VALID_FILESYSTEM_PATH");

	private string originalPlaceholder;

	[SetUp]
	public void EnableRelaxedMapping()
	{
		originalPlaceholder = (string)placeholderField.GetValue(null);
		placeholderField.SetValue(null, placeholder);
	}

	[TearDown]
	public void RestoreRelaxedMapping() => placeholderField.SetValue(null, originalPlaceholder);

	private static string GetRelaxedMapPathResult(string path) => (string)getRelaxedMapPathResult.Invoke(null, new object[] { path });

	[Test]
	public void ValidPhysicalPathIsKept()
	{
		// On Unix the directory separator is '/', so a valid mapped path contains it.
		var path = Path.Combine(Path.GetTempPath(), "site", "page.aspx");

		Assert.That(GetRelaxedMapPathResult(path), Is.EqualTo(path));
	}

	[TestCase("page?.aspx")]
	[TestCase("page*.aspx")]
	[TestCase("page<.aspx")]
	[TestCase("page>.aspx")]
	[TestCase("page|.aspx")]
	[TestCase("page\".aspx")]
	public void PhysicalPathWithInvalidCharacterIsReplacedByPlaceholder(string fileName)
	{
		var path = Path.Combine(Path.GetTempPath(), "site", fileName);

		Assert.That(GetRelaxedMapPathResult(path), Is.EqualTo(placeholder));
	}

	[Test]
	public void NullIsReplacedByPlaceholder()
	{
		Assert.That(GetRelaxedMapPathResult(null), Is.EqualTo(placeholder));
	}
}
