using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXml.Serialization;
using Crestron.SimplSharp.CrestronXmlLinq;
using System.Text.RegularExpressions;

namespace PDTDynFusionEPI
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
		/// <param name="nonEscapedXml"></param>
		/// <returns></returns>
		public static XmlDocument CustomEscapeDocument(this XmlDocument doc, string nonEscapedXml)
		{
			try
			{
				var noAmp = Regex.Replace(nonEscapedXml, "&(?!(amp|apos|quot|lt|gt);)", "&amp;");
				var escape = Regex.Replace(noAmp, "<Location>(.*?)</Location>", (match) =>
				{
					var original = match.Value;
					Regex rgx = new Regex("<Location>(.*?)</Location>");
					var split = rgx.Split(original);
					split = split.Where(x => !string.IsNullOrEmpty(x)).ToArray();
					if (split.Count() > 0 && split[0].Contains('>') || split[0].Contains('<'))
					{
						var update = split[0].Replace(">", "&gt;");
						update = update.Replace("<", "&lt;");
						update = "<Location>" + update + "</Location>";
						return update;
					}
					return original;
				});

				XmlDocument scheduleXML = new XmlDocument();
				scheduleXML.LoadXml(escape);
				return scheduleXML;
			}
			catch (Exception ex)
			{
				ErrorLog.Error("Error Escaping XML: {0}. Inner Exception {1}", ex.Message, ex.InnerException);
			}
			return null;
		}
	}
}