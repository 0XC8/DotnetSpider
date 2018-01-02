﻿using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
#if !NET_CORE
using System.Web;
#endif

namespace DotnetSpider.Core.Processor
{
	public class RegionAndPatternTargetUrlsExtractor : TargetUrlsExtractor
	{
		private readonly Dictionary<ISelector, List<Regex>> _regionSelectorMapPatterns = new Dictionary<ISelector, List<Regex>>();

		private static readonly ISelector ImageSelector = Selectors.XPath(".//img/@src");

		public RegionAndPatternTargetUrlsExtractor()
		{
		}

		public RegionAndPatternTargetUrlsExtractor(string regionXpath, params string[] patterns)
		{
			AddTargetUrlExtractor(regionXpath, patterns);
		}

		protected override IEnumerable<Request> Extract(Page page, Site site)
		{
			if (_regionSelectorMapPatterns == null || _regionSelectorMapPatterns.Count == 0)
			{
				return new Request[0];
			}

			List<string> resultUrls = new List<string>();
			foreach (var targetUrlExtractor in _regionSelectorMapPatterns)
			{
				if (Equals(targetUrlExtractor.Key, Selectors.Default()))
				{
					continue;
				}
				List<string> links = null;
				if (page.ContentType == ContentType.Html)
				{
					links = page.Selectable.SelectList(targetUrlExtractor.Key).Links().GetValues();
				}
				else if (page.ContentType == ContentType.Json)
				{
					links = page.Selectable.SelectList(Selectors.Regex(RegexUtil.Url)).Links().GetValues();
				}
				else
				{
				}

				if (links == null)
				{
					continue;
				}

				// check: 仔细考虑是放在前面, 还是在后面做 formatter, 我倾向于在前面. 对targetUrl做formatter则表示Start Url也应该是要符合这个规则的。
				List<string> tmp = new List<string>();
				foreach (string link in links)
				{
					var newUrl = FormateUrl(link);
#if !NET_CORE
					tmp.Add(HttpUtility.HtmlDecode(HttpUtility.UrlDecode(newUrl)));
#else
					tmp.Add(WebUtility.HtmlDecode(WebUtility.UrlDecode(newUrl)));
#endif
				}
				links = tmp;

				if (targetUrlExtractor.Value == null || targetUrlExtractor.Value.Count == 0)
				{
					resultUrls.AddRange(links);
					continue;
				}

				foreach (var regex in targetUrlExtractor.Value)
				{
					foreach (string link in links)
					{
						if (regex.IsMatch(link))
						{
							bool isRequired = true;
							if (ExcludeTargetUrlPatterns != null)
							{
								foreach (var excludeRegex in ExcludeTargetUrlPatterns)
								{
									if (excludeRegex.IsMatch(link))
									{
										isRequired = false;
										break;
									}
								}
							}
							if (isRequired)
							{
								resultUrls.Add(link);
							}
						}
					}
				}
			}

			if (site.DownloadFiles)
			{
				var links = (page.Selectable.SelectList(ImageSelector)).GetValues();

				if (links != null && links.Count > 0)
				{
					foreach (string link in links)
					{
						bool isRequired = true;
						if (ExcludeTargetUrlPatterns != null)
						{
							foreach (var excludeRegex in ExcludeTargetUrlPatterns)
							{
								if (excludeRegex.IsMatch(link))
								{
									isRequired = false;
									break;
								}
							}
						}
						if (isRequired)
						{
							resultUrls.Add(link);
						}
					}
				}

			}

			return resultUrls.Select(t => new Request(t, page.Request.Extras) { Site = site });
		}

		public void AddTargetUrlExtractor(string regionXpath, params string[] patterns)
		{
			if (patterns == null || patterns.Length == 0)
			{
				throw new ArgumentNullException("Patterns should not be null or empty.");
			}

			var validPatterns = patterns.Where(p => p != null && !string.IsNullOrEmpty(p.Trim())).Select(p => p.Trim()).ToList();

			if (validPatterns.Count != patterns.Length)
			{
				throw new ArgumentNullException("Pattern value should not be null or empty.");
			}

			ISelector selector = Selectors.Regex(RegexUtil.Url);
			if (!string.IsNullOrEmpty(regionXpath))
			{
				string xpath = string.IsNullOrWhiteSpace(regionXpath.Trim()) ? "." : regionXpath.Trim();
				selector = Selectors.XPath(xpath);
			}

			if (!_regionSelectorMapPatterns.ContainsKey(selector))
			{
				_regionSelectorMapPatterns.Add(selector, new List<Regex>());
			}
			var oldPatterns = _regionSelectorMapPatterns[selector];
			// 如果已经有正则为空, 即表示当前区域内所有的URL都是目标链接, 则无需再校验其它正则了
			if (oldPatterns.Contains(null))
			{
				return;
			}
			// 如果不提供正则表达式, 表示当前区域内所有的URL都是目标链接
			if (validPatterns.Count == 0)
			{
				oldPatterns.Add(null);
			}
			foreach (var pattern in validPatterns)
			{
				if (oldPatterns.All(p => p.ToString() != pattern))
				{
					oldPatterns.Add(new Regex(pattern));
					AddTargetUrlPatterns(pattern);
				}
			}
		}

		/// <summary>
		/// Only used for test
		/// </summary>
		/// <param name="region"></param>
		/// <returns></returns>
		internal virtual bool ContainsTargetUrlRegion(string region)
		{
			ISelector selector = Selectors.Default();
			if (!string.IsNullOrWhiteSpace(region))
			{
				selector = Selectors.XPath(region);
			}
			return _regionSelectorMapPatterns.ContainsKey(selector);
		}

		/// <summary>
		/// Only used for test
		/// </summary>
		/// <param name="regionXpath"></param>
		/// <returns></returns>
		internal virtual List<Regex> GetTargetUrlPatterns(string regionXpath)
		{
			ISelector selector = Selectors.Regex(RegexUtil.Url);
			if (!string.IsNullOrWhiteSpace(regionXpath))
			{
				selector = Selectors.XPath(regionXpath);
			}

			return _regionSelectorMapPatterns.ContainsKey(selector) ? _regionSelectorMapPatterns[selector] : null;
		}

		protected virtual string FormateUrl(string url)
		{
			return url;
		}
	}

	public abstract class PaginationTargetUrlsExtractor : TargetUrlsExtractor
	{
		public readonly Regex PaginationPattern;

		/// <summary>
		/// http://a.com?p=40  PaginationStr: p=40 => Pattern: p=\d+
		/// </summary>
		public readonly string PaginationStr;

		protected PaginationTargetUrlsExtractor(string paginationStr, ITargetUrlsExtractorTermination termination = null)
		{
			if (string.IsNullOrEmpty(paginationStr) || string.IsNullOrWhiteSpace(paginationStr))
			{
				throw new SpiderException("paginationStr should not be null or empty.");
			}

			PaginationStr = paginationStr;
			PaginationPattern = new Regex($"{RegexUtil.Number.Replace(PaginationStr, @"\d+")}");
			TargetUrlsExtractorTermination = termination;
		}

		protected string GetCurrentPagination(string currentUrlOrContent)
		{
			return PaginationPattern.Match(currentUrlOrContent).Value;
		}
	}

	public class AutoIncrementTargetUrlsExtractor : PaginationTargetUrlsExtractor
	{
		private readonly int _interval;

		public AutoIncrementTargetUrlsExtractor(string paginationStr, int interval = 1, ITargetUrlsExtractorTermination termination = null) : base(paginationStr, termination)
		{
			_interval = interval;
		}

		protected override IEnumerable<Request> Extract(Page page, Site site)
		{
			var currentPageStr = GetCurrentPagination(page.Request.Url);
			var matches = RegexUtil.Number.Matches(currentPageStr);
			if (matches.Count > 0 && int.TryParse(matches[0].Value, out var currentPage))
			{
				var next = RegexUtil.Number.Replace(PaginationStr, (currentPage + _interval).ToString());
				string newUrl = page.Request.Url.Replace(currentPageStr, next);
				return new Request[] { new Request(newUrl, page.Request.Extras) { Site = site } };
			}

			return new Request[0];
		}

	}
}