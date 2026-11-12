using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Web.Hosting
{
	public interface IAspNetCoreWorkerRequest
	{
		public AspNetCoreHost Host { get; }
		public Microsoft.AspNetCore.Http.HttpContext Context { get; }

		/// <summary>
		/// The virtual path of the default document that serves the requested directory, or null.
		/// The pipeline applies it when the handler is mapped, like IIS does.
		/// </summary>
		public string DefaultDocumentPath => null;
	}
}
