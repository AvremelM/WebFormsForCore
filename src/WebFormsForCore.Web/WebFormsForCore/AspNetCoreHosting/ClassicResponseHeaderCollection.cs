#if NETCOREAPP

using System.Collections.Specialized;

namespace System.Web.Hosting
{
	/// <summary>
	/// HttpResponse.Headers for the classic pipeline of the ASP.NET Core host. IIS only offers a writable header block
	/// in the integrated pipeline, so this is a snapshot of the custom headers of the response that writes
	/// Add, Set and Remove through to it. A new snapshot is created on every access of HttpResponse.Headers.
	/// </summary>
	internal sealed class ClassicResponseHeaderCollection : NameValueCollection
	{
		private readonly HttpResponse response;

		internal ClassicResponseHeaderCollection(HttpResponse response)
		{
			this.response = response;
			foreach (var header in response.GetCustomHeaders())
			{
				base.Add(header.Key, header.Value);
			}
		}

		public override void Add(string name, string value)
		{
			response.AppendHeader(name, value);
			base.Add(name, value);
		}

		public override void Set(string name, string value)
		{
			response.SetCustomHeader(name, value);
			base.Set(name, value);
		}

		public override void Remove(string name)
		{
			response.RemoveCustomHeader(name);
			base.Remove(name);
		}

		public override void Clear() => throw new NotSupportedException();
	}
}

#endif
