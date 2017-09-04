﻿using System;
using System.Collections.Generic;
using System.Net;
using DotnetSpider.Core.Selector;
using DotnetSpider.Core.Infrastructure;

namespace DotnetSpider.Core
{
	/// <summary>
	/// Object storing extracted result and urls to fetch. 
	/// </summary>
	public class Page
	{
		public const string Images = "580c9065-0f44-47e9-94ea-b172d5a730c0";

		private Selectable _selectable;
		private string _content;

		public ContentType ContentType { get; internal set; } = ContentType.Html;

		/// <summary>
		/// Url of current page
		/// </summary>
		/// <returns></returns>
		public string Url { get; }

		/// <summary>
		/// Get url of current page
		/// </summary>
		/// <returns></returns>
		public string TargetUrl { get; set; }

		public string Title { get; set; }

		/// <summary>
		/// Get request of current page
		/// </summary>
		/// <returns></returns>
		public Request Request { get; }

		public bool Retry { get; set; }

		public bool SkipExtractedTargetUrls { get; set; }

		public ResultItems ResultItems { get; } = new ResultItems();

		public HttpStatusCode? StatusCode => Request?.StatusCode;

		public string Padding { get; set; }

		public string Content
		{
			get => _content;
			set
			{
				if (!Equals(value, _content))
				{
					_content = value;
					_selectable = null;
				}
			}
		}

		public bool SkipTargetUrls { get; set; }

		public bool Skip
		{
			get => ResultItems.IsSkip;
			set => ResultItems.IsSkip = value;
		}

		public Exception Exception { get; set; }

		public HashSet<Request> TargetRequests { get; } = new HashSet<Request>();

		public bool RemoveOutboundLinks { get; }

		public string[] Domains { get; }

		/// <summary>
		/// Get html content of page
		/// </summary>
		/// <returns></returns>
		public Selectable Selectable
		{
			get
			{
				if (_selectable == null)
				{
					string urlPadding = ContentType == ContentType.Json ? Padding : Request.Url.ToString();
					_selectable = new Selectable(Content, urlPadding, ContentType, RemoveOutboundLinks ? Domains : null);
				}
				return _selectable;
			}
		}

		public Page(Request request, params string[] domains)
		{
			Request = request;
			Url = request.Url.ToString();
			ResultItems.Request = request;
			RemoveOutboundLinks = domains != null && domains.Length > 0;
			Domains = domains;
		}

		/// <summary>
		/// Store extract results
		/// </summary>
		/// <param name="key"></param>
		/// <param name="field"></param>
		public void AddResultItem(string key, dynamic field)
		{
			ResultItems.AddOrUpdateResultItem(key, field);
		}

		/// <summary>
		/// Add urls to fetch
		/// </summary>
		/// <param name="requests"></param>
		public void AddTargetRequests(IList<string> requests)
		{
			if (requests == null || requests.Count == 0)
			{
				return;
			}
			lock (this)
			{
				foreach (string s in requests)
				{
					if (string.IsNullOrEmpty(s) || s.Equals("#") || s.StartsWith("javascript:"))
					{
						continue;
					}
					string s1 = UrlUtils.CanonicalizeUrl(s, Url);
					var request = new Request(s1, Request.Extras) { Depth = Request.NextDepth };
					if (request.IsAvailable)
					{
						TargetRequests.Add(request);
					}
				}
			}
		}

		/// <summary>
		/// Add urls to fetch
		/// </summary>
		/// <param name="requests"></param>
		public void AddTargetRequests(IList<Request> requests)
		{
			if (requests == null || requests.Count == 0)
			{
				return;
			}
			lock (this)
			{
				foreach (var r in requests)
				{
					r.Depth = Request.NextDepth;
					TargetRequests.Add(r);
				}
			}
		}

		/// <summary>
		/// Add urls to fetch
		/// </summary>
		/// <param name="requests"></param>
		/// <param name="priority"></param>
		public void AddTargetRequests(IList<string> requests, int priority)
		{
			if (requests == null || requests.Count == 0)
			{
				return;
			}
			lock (this)
			{
				foreach (string s in requests)
				{
					if (string.IsNullOrEmpty(s) || s.Equals("#") || s.StartsWith("javascript:"))
					{
						continue;
					}
					string s1 = UrlUtils.CanonicalizeUrl(s, Url);
					Request request = new Request(s1, Request.Extras) { Priority = priority, Depth = Request.NextDepth };
					if (request.IsAvailable)
					{
						TargetRequests.Add(request);
					}
				}
			}
		}

		/// <summary>
		/// Add url to fetch
		/// </summary>
		/// <param name="requestString"></param>
		/// <param name="increaseDeep"></param>
		public void AddTargetRequest(string requestString, bool increaseDeep = true)
		{
			lock (this)
			{
				if (string.IsNullOrEmpty(requestString) || requestString.Equals("#"))
				{
					return;
				}

				requestString = UrlUtils.CanonicalizeUrl(requestString, Url);
				var request = new Request(requestString, Request.Extras)
				{
					Depth = Request.NextDepth
				};

				if (increaseDeep)
				{
					request.Depth = Request.NextDepth;
				}
				TargetRequests.Add(request);
			}
		}

		/// <summary>
		/// Add requests to fetch
		/// </summary>		 
		public void AddTargetRequest(Request request, bool increaseDeep = true)
		{
			if (request == null)
			{
				return;
			}
			lock (this)
			{
				if (request.IsAvailable)
				{
					if (increaseDeep)
					{
						request.Depth = Request.NextDepth;
					}
					TargetRequests.Add(request);
				}
			}
		}
	}
}
