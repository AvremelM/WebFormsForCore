using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace WebFormsForCore.Tests.Runtime;

/// <summary>
/// On .NET Framework the logical call context is copied when an async flow is captured, so changes made in a
/// child flow are not visible to its parent.
/// </summary>
[TestFixture]
public class CallContextTests
{
	private const string Key = "CallContextTests.Key";

	[TearDown]
	public void ClearKey() => CallContext.FreeNamedDataSlot(Key);

	[Test]
	public async Task ChangeInChildFlowIsNotVisibleToParent()
	{
		CallContext.SetData(Key, "parent");

		await Task.Run(() => CallContext.SetData(Key, "child"));

		Assert.That(CallContext.GetData(Key), Is.EqualTo("parent"));
	}

	[Test]
	public async Task ChildFlowInheritsValueOfParent()
	{
		CallContext.SetData(Key, "parent");

		var seen = await Task.Run(() => CallContext.GetData(Key));

		Assert.That(seen, Is.EqualTo("parent"));
	}

	[Test]
	public async Task FreeingDataSlotInChildFlowIsNotVisibleToParent()
	{
		CallContext.SetData(Key, "parent");

		await Task.Run(() => CallContext.FreeNamedDataSlot(Key));

		Assert.That(CallContext.GetData(Key), Is.EqualTo("parent"));
	}

	[Test]
	public async Task SiblingFlowsDoNotSeeEachOthersChanges()
	{
		CallContext.SetData(Key, "parent");
		var firstHasSet = new TaskCompletionSource();
		var secondMayRead = new TaskCompletionSource();

		var first = Task.Run(async () =>
		{
			CallContext.SetData(Key, "first");
			firstHasSet.SetResult();
			await secondMayRead.Task;
			return CallContext.GetData(Key);
		});
		var second = Task.Run(async () =>
		{
			await firstHasSet.Task;
			var seen = CallContext.GetData(Key);
			secondMayRead.SetResult();
			return seen;
		});

		Assert.That(await second, Is.EqualTo("parent"));
		Assert.That(await first, Is.EqualTo("first"));
	}

	[Test]
	public async Task ValueSetBeforeAwaitSurvivesAwait()
	{
		CallContext.SetData(Key, "value");

		await Task.Delay(20);

		Assert.That(CallContext.GetData(Key), Is.EqualTo("value"));
	}
}
