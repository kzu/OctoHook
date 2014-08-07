using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace OctoHook
{
	public partial class index : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			var assemblies = new HashSet<Assembly>();
			assemblies.Add(Assembly.GetExecutingAssembly());
			foreach (var file in Directory.EnumerateFiles(Server.MapPath("bin"), "*.dll"))
			{
				Trace.Write(string.Format("Loading {0} for composition.", Path.GetFileName(file)));
				try
				{
					var name = AssemblyName.GetAssemblyName(file);

					try
					{
						var asm = Assembly.Load(name);
						if (!assemblies.Contains(asm))
							assemblies.Add(asm);
					}
					catch (Exception ex)
					{
						Trace.Write(string.Format("Failed to load {0} for composition:\r\n{1}", Path.GetFileName(file), ex));
					}
				}
				catch { } // AssemblyName loading could fail for non-managed assemblies
			}

			var writer = new HtmlTextWriter(Response.Output);

			foreach (var asm in assemblies)
			{
				writer.Write(asm.FullName);
				writer.WriteBreak();
			}

			writer.Flush();
		}
	}
}