﻿using DotnetSpider.Core.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace DotnetSpider.Core.Downloader
{
	public class IncrementTargetUrlsBuilder : TargetUrlsExtractor
	{
		private readonly int _interval;

		public IncrementTargetUrlsBuilder(string pagerString, int interval = 1,
			ITargetUrlsExtractorTermination termination = null) : base(pagerString, termination)
		{
			_interval = interval;
		}

		private string IncreasePageNum(string currentUrl)
		{
			var currentPaggerString = GetCurrentPagger(currentUrl);
			var matches = RegexUtil.NumRegex.Matches(currentPaggerString);
			if (matches.Count == 0)
			{
				return null;
			}

			if (int.TryParse(matches[0].Value, out var currentPagger))
			{
				var nextPagger = currentPagger + _interval;
				var next = RegexUtil.NumRegex.Replace(PagerString, nextPagger.ToString());
				return currentUrl.Replace(currentPaggerString, next);
			}
			return null;
		}

		public override IEnumerable<Request> ExtractRequests(Page page)
		{
			string newUrl = IncreasePageNum(page.Url);
			return string.IsNullOrEmpty(newUrl) ? null : new[] { new Request(newUrl, page.Request.Extras) };
		}
	}

	public class RequestExtraTargetUrlsBuilder : TargetUrlsExtractor
	{
		private readonly string _field;

		public RequestExtraTargetUrlsBuilder(string pagerString, string field,
			ITargetUrlsExtractorTermination termination = null) : base(pagerString, termination)
		{
			_field = field;
		}

		private string GenerateNewPaggerUrl(Page page)
		{
			var currentUrl = page.Url;
			var nextPagger = page.Request.GetExtra(_field)?.ToString();
			if (nextPagger != null)
			{
				var currentPaggerString = GetCurrentPagger(currentUrl);
				var matches = RegexUtil.NumRegex.Matches(currentPaggerString);
				if (matches.Count == 0)
				{
					return null;
				}

				if (int.TryParse(matches[0].Value, out _))
				{
					var next = RegexUtil.NumRegex.Replace(PagerString, nextPagger.ToString());
					return currentUrl.Replace(currentPaggerString, next);
				}
			}
			return null;
		}

		public override IEnumerable<Request> ExtractRequests(Page page)
		{
			string newUrl = GenerateNewPaggerUrl(page);
			return string.IsNullOrEmpty(newUrl) ? null : new[] { new Request(newUrl, page.Request.Extras) };
		}
	}

	public class ContainsTermination : ITargetUrlsExtractorTermination
	{
		private readonly string[] _contents;

		public ContainsTermination(string[] contents)
		{
			_contents = contents;
		}

		public bool IsTermination(Page page, TargetUrlsExtractor builder)
		{
			if (string.IsNullOrEmpty(page?.Content))
			{
				return false;
			}

			return _contents.Any(c => page.Content.Contains(c));
		}
	}

	public class UnContainsTermination : ITargetUrlsExtractorTermination
	{
		private readonly string[] _contents;

		public UnContainsTermination(string[] contents)
		{
			_contents = contents;
		}

		public bool IsTermination(Page page, TargetUrlsExtractor builder)
		{
			if (string.IsNullOrEmpty(page?.Content))
			{
				return false;
			}

			return !_contents.All(c => page.Content.Contains(c));
		}
	}

	public class LimitPageNumTermination : ITargetUrlsExtractorTermination
	{
		private readonly int _limit;

		public LimitPageNumTermination(int limit)
		{
			_limit = limit;
		}

		public bool IsTermination(Page page, TargetUrlsExtractor builder)
		{
			if (string.IsNullOrEmpty(page?.Content))
			{
				return false;
			}
			var current =
				builder.GetCurrentPagger(page.Request.Method == HttpMethod.Get ? page.Url : page.Request.PostBody);
			int currentIndex = int.Parse(RegexUtil.NumRegex.Match(current).Value);

			return currentIndex >= _limit;
		}
	}
}