using DotnetSpider.Core.Selector;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DotnetSpider.Core.Processor
{
	public class DefaultPageProcessor : BasePageProcessor
	{
		private Regex _regex;

		public DefaultPageProcessor(string partten)
		{
			TargetUrlPatterns = new HashSet<Regex>()
			{
				new Regex(partten)
			};
			TargetUrlRegions = new HashSet<ISelector> { Selectors.XPath(".") };
		}

		protected override void Handle(Page page)
		{
			page.AddResultItem("title", page.Selectable.XPath("//title").GetValue());
			page.AddResultItem("html", page.Content);

			//foreach (var url in page.Selectable.Links().GetValues())
			//{
			//	Uri uri;
			//	if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) && _regex.IsMatch(url))
			//	{
			//		page.AddTargetRequest(new Request(url));
			//	}
			//}
		}
	}
}
