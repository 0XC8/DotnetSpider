﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Java2Dotnet.Spider.Core;
using Java2Dotnet.Spider.Extension.Utils;

namespace Java2Dotnet.Spider.Extension.Configuration
{
	public abstract class TargetUrlsHandler
	{
		[Flags]
		public enum Types
		{
			IncreasePageNumber
		}

		public abstract Types Type { get; internal set; }

		public abstract IList<string> Handle(Page page);
	}

	public class IncreasePageNumberTargetUrlsHandler : TargetUrlsHandler
	{
		public override Types Type { get; internal set; } = Types.IncreasePageNumber;

		/// <summary>
		/// Like &s=44 或者 &page=1 或者 o1
		/// </summary>
		public string PageIndexString { get; set; }

		public int Interval { get; set; }

		public Selector TotalPageSelector { get; set; }

		public Selector CurrenctPageSelector { get; set; }

		public override IList<string> Handle(Page page)
		{
			string pattern = $"{RegexUtil.NumRegex.Replace(PageIndexString, @"\d+")}";
			Regex regex = new Regex(pattern);
			string current = regex.Match(page.Url).Value;
			int currentIndex = int.Parse(RegexUtil.NumRegex.Match(current).Value);
			int nextIndex = currentIndex + Interval;
			string next = RegexUtil.NumRegex.Replace(PageIndexString, nextIndex.ToString());

			int totalPage = -2;
			if (TotalPageSelector != null)
			{
				totalPage = int.Parse(page.Selectable.Select(SelectorUtil.GetSelector(TotalPageSelector)).Value);
			}
			int currentPage = -1;
			if (CurrenctPageSelector != null)
			{
				currentPage = int.Parse(page.Selectable.Select(SelectorUtil.GetSelector(CurrenctPageSelector)).Value);
			}
			if (currentPage == totalPage)
			{
				return new List<string>();
			}
			return new List<string> { page.Url.Replace(current, next) };
		}
	}
}
