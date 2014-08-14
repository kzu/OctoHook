<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="System.IO" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <h1>Build Info</h1>
    OctoHook.Web, Version=<%= AppDomain.CurrentDomain.GetAssemblies().First(x => x.FullName.StartsWith("OctoHook.Web")).GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion %>
    <h1>Available Assemblies</h1>
    <%
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

			foreach (var asm in assemblies.OrderBy(a => a.FullName))
			{
%>
    <%= asm.FullName %>
    <br />
<%
			}
     %>
</body>
</html>
