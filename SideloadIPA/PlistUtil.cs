using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace SideloadIPA
{
	public class PList : Dictionary<string, object>
	{
		private PList()
		{
			
		}
		
		public static JObject ParsePList(PList plist)
		{
			JObject jObject = new JObject();
			foreach (KeyValuePair<string, object> item in plist)
			{
				if (item.Value is PList)
				{
					try
					{
						jObject.Add(item.Key, ParsePList((dynamic) item.Value));
					}
					catch
					{
					}
				}

				if (item.Value is List<object>)
				{
					bool flag = false;
					foreach (dynamic item2 in (dynamic) item.Value)
					{
						if (!(item2 is PList))
						{
							flag = true;
							break;
						}

						try
						{
							jObject.Add(item.Key, ParsePList(item2));
						}
						catch
						{
						}
					}

					if (flag)
					{
						List<string> list = new List<string>();
						foreach (dynamic item3 in (dynamic) item.Value)
						{
							list.Add(item3.ToString());
						}

						try
						{
						}
						catch
						{
							jObject.Add(item.Key, (dynamic) item.Value);
						}
					}
				}
				else
				{
					try
					{
						jObject.Add(item.Key, (dynamic) item.Value);
					}
					catch
					{
					}
				}
			}

			return jObject;
		}

		public PList(string file)
		{
			Load(file);
		}

		public void Load(string file)
		{
			Clear();
			XDocument xDocument = XDocument.Parse(file);
			XElement xElement = xDocument.Element("plist");
			XElement xElement2 = xElement.Element("dict");
			IEnumerable<XElement> elements = xElement2.Elements();
			Parse(this, elements);
		}

		private void Parse(PList dict, IEnumerable<XElement> elements)
		{
			for (int i = 0; i < elements.Count(); i += 2)
			{
				XElement xElement = elements.ElementAt(i);
				XElement val = elements.ElementAt(i + 1);
				dict[xElement.Value] = (object) ParseValue(val);
			}
		}

		private List<dynamic> ParseArray(IEnumerable<XElement> elements)
		{
			List<object> list = new List<object>();
			foreach (XElement element in elements)
			{
				dynamic val = ParseValue(element);
				list.Add(val);
			}

			return list;
		}

		private dynamic ParseValue(XElement val)
		{
			switch (val.Name.ToString())
			{
				case "dict":
				{
					PList pList = new PList();
					Parse(pList, val.Elements());
					return pList;
				}
				case "array":
					return ParseArray(val.Elements());
				default:
					return val.Value;
			}
		}
	}
}