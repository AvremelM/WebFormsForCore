using System.Threading.Tasks;

// Outside any namespace so that it applies to every test in the assembly.
[SetUpFixture]
public class TestSetup
{
	[OneTimeSetUp]
	public Task StartSite() => WebFormsForCore.Tests.Infrastructure.WebFormsTestSite.StartSharedAsync();

	[OneTimeTearDown]
	public Task StopSite() => WebFormsForCore.Tests.Infrastructure.WebFormsTestSite.StopSharedAsync();
}
