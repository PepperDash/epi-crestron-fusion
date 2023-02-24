using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using System.Text.RegularExpressions;

namespace DynFusion
{
	public static class XmlExtensions
	{
		//DynFusion fusion { get; set; }

		//public XmlExtensions(DynFusion fus)
		//{
		//    fusion = fus;
		//}

	    /// <summary>
	    /// Custom method that needs to be updated if other properties 
	    /// are found to be invalid. This checks for & and will fix the Location field for lt gt
	    /// </summary>
	    /// <param name="doc"></param>
	    /// <param name="nonEscapedXml"></param>
	    /// <returns></returns>
	    public static XmlDocument CustomEscapeDocument(this XmlDocument doc, string nonEscapedXml)
		{
			try
			{
				var noAmp = Regex.Replace(nonEscapedXml, "&(?!(amp|apos|quot|lt|gt);)", "&amp;");
				var escape = Regex.Replace(noAmp, "<Location>(.*?)</Location>", match =>
				{
					var original = match.Value;
					var rgx = new Regex("<Location>(.*?)</Location>");
					var split = rgx.Split(original);
					split = split.Where(x => !string.IsNullOrEmpty(x)).ToArray();
					if (split.Any() && split[0].Contains('>') || split[0].Contains('<'))
					{
						var update = split[0].Replace(">", "&gt;");
						update = update.Replace("<", "&lt;");
						update = "<Location>" + update + "</Location>";
						return update;
					}
					return original;
				});

				var scheduleXml = new XmlDocument();
				scheduleXml.LoadXml(escape);
				return scheduleXml;
			}
			catch (Exception ex)
			{
				ErrorLog.Error("Error Escaping XML: {0}. Inner Exception {1}", ex.Message, ex.InnerException);
			}
			return null;
		}
	}
}