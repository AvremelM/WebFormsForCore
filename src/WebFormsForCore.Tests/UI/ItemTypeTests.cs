using System.Threading.Tasks;
using WebFormsForCore.Tests.Infrastructure;

namespace WebFormsForCore.Tests.UI;

/// <summary>
/// Setting ItemType on a data-bound control enables Dynamic Data, which is an optional assembly in WebFormsForCore
/// (System.Web.DynamicData). ItemType is also used for plain strongly typed binding, so a missing assembly
/// must not break the page.
/// </summary>
[TestFixture]
public class ItemTypeTests
{
	private static WebFormsTestSite Site => WebFormsTestSite.Shared;

	[Test]
	public async Task RepeaterWithItemTypeBindsWithoutDynamicDataAssembly()
	{
		Site.Write("itemtype/repeater.aspx", """
			<%@ Page Language="C#" %>
			<script runat="server">
			protected void Page_Load(object sender, EventArgs e)
			{
				repeater.DataSource = new[] { "a", "b" };
				repeater.DataBind();
			}
			</script>
			<asp:Repeater ID="repeater" runat="server" ItemType="System.String"><ItemTemplate><%# Item %>;</ItemTemplate></asp:Repeater>
			""");

		Assert.That((await Site.GetStringAsync("itemtype/repeater.aspx")).Trim(), Is.EqualTo("a;b;"));
	}

	[Test]
	public async Task GridViewWithItemTypeBindsWithoutDynamicDataAssembly()
	{
		Site.Write("itemtype/grid.aspx", """
			<%@ Page Language="C#" %>
			<script runat="server">
			protected void Page_Load(object sender, EventArgs e)
			{
				grid.DataSource = new[] { "a", "b" };
				grid.DataBind();
			}
			</script>
			<form runat="server"><asp:GridView ID="grid" runat="server" ItemType="System.String" AutoGenerateColumns="false"><Columns><asp:TemplateField><ItemTemplate><%# Item %>;</ItemTemplate></asp:TemplateField></Columns></asp:GridView></form>
			""");

		Assert.That(await Site.GetStringAsync("itemtype/grid.aspx"), Does.Contain("a;").And.Contain("b;"));
	}
}
