﻿using System;
using System.Collections.Generic;
using System.Net;
using DotnetSpider.Common;

namespace DotnetSpider.Downloader
{
	/// <summary>
	/// The Abstraction of a basic downloader.
	/// </summary>
	/// <summary xml:lang="zh-CN">
	/// 基础下载器的抽象
	/// </summary>
	public abstract class BaseDownloader : IDownloader
	{
		private readonly List<IAfterDownloadCompleteHandler> _afterDownloadCompletes = new List<IAfterDownloadCompleteHandler>();
		private readonly List<IBeforeDownloadHandler> _beforeDownloads = new List<IBeforeDownloadHandler>();
		private bool _injectedCookies;

		/// <summary>
		/// Cookie Container
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// Cookie 管理容器
		/// </summary>
		protected readonly CookieContainer CookieContainer = new CookieContainer();

		/// <summary>
		/// 是否自动跳转
		/// </summary>
		public bool AllowAutoRedirect { get; set; } = true;

		/// <summary>
		/// 日志接口
		/// </summary>
		public ILogger Logger { get; set; }

		/// <summary>
		/// Interface to inject cookie.
		/// </summary>
		public ICookieInjector CookieInjector { get; set; }

		/// <summary>
		/// Add cookies to downloader
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 设置 Cookies
		/// </summary>
		/// <param name="cookiesStr">Cookies的键值对字符串, 如: a1=b;a2=c;(Cookie's key-value pairs string, a1=b;a2=c; etc.)</param>
		/// <param name="domain">作用域(<see cref="Cookie.Domain"/>)</param>
		/// <param name="path">作用路径(<see cref="Cookie.Path"/>)</param>
		public void AddCookies(string cookiesStr, string domain, string path = "/")
		{
			var pairs = cookiesStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var pair in pairs)
			{
				var keyValue = pair.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				var name = keyValue[0];
				string value = keyValue.Length > 1 ? keyValue[1] : string.Empty;
				AddCookie(name, value, domain, path);
			}
		}

		/// <summary>
		/// Add cookies to downloader
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 添加Cookies
		/// </summary>
		/// <param name="cookies">Cookies的键值对 (Cookie's key-value pairs)</param>
		/// <param name="domain">作用域(<see cref="Cookie.Domain"/>)</param>
		/// <param name="path">作用路径(<see cref="Cookie.Path"/>)</param>
		public void AddCookies(IDictionary<string, string> cookies, string domain, string path = "/")
		{
			foreach (var pair in cookies)
			{
				var name = pair.Key;
				var value = pair.Value;
				AddCookie(name, value, domain, path);
			}
		}

		/// <summary>
		/// Add one cookie to downloader
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 添加Cookie
		/// </summary>
		/// <param name="name">名称(<see cref="Cookie.Name"/>)</param>
		/// <param name="value">值(<see cref="Cookie.Value"/>)</param>
		/// <param name="domain">作用域(<see cref="Cookie.Domain"/>)</param>
		/// <param name="path">作用路径(<see cref="Cookie.Path"/>)</param>
		public void AddCookie(string name, string value, string domain, string path = "/")
		{
			var cookie = new Cookie(name.Trim(), value.Trim(), path.Trim(), domain.Trim());
			AddCookie(cookie);
		}

		/// <summary>
		/// Gets a <see cref="System.Net.CookieCollection"/> that contains the <see cref="System.Net.Cookie"/> instances that are associated with a specific <see cref="Uri"/>.
		/// </summary>
		/// <param name="uri">The URI of the System.Net.Cookie instances desired.</param>
		/// <returns>A <see cref="System.Net.CookieCollection"/> that contains the <see cref="System.Net.Cookie"/> instances that are associated with a specific <see cref="Uri"/>.</returns>
		public CookieCollection GetCookies(Uri uri)
		{
			return CookieContainer.GetCookies(uri);
		}

		/// <summary>
		/// Add one cookie to downloader
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 设置 Cookie
		/// </summary>
		/// <param name="cookie">Cookie</param>
		public virtual void AddCookie(Cookie cookie)
		{
			if (cookie == null)
			{
				return;
			}
			CookieContainer.Add(cookie);
		}

		/// <summary>
		/// Download webpage content and build a <see cref="Response"/> instance.
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 下载链接内容
		/// </summary>
		/// <param name="request">链接请求 <see cref="Request"/></param>
		/// <returns>下载内容封装好的页面对象 (a <see cref="Response"/> instance that contains requested page infomations, like Html source, headers, etc.)</returns>
		public Response Download(Request request)
		{
			lock (this)
			{
				if (!_injectedCookies && CookieInjector != null)
				{
					CookieInjector.Inject(this, false);
					_injectedCookies = true;
				}
			}
			BeforeDownload(ref request);
			var response = DowloadContent(request);
			AfterDownloadComplete(ref response);
			return response;
		}

		protected virtual void DetectContentType(Response response, string contentType)
		{
			if (response.Request.Site.ContentType == ContentType.Auto)
			{
				if (contentType.Contains("json"))
				{
					response.ContentType = ContentType.Json;
				}
				else
				{
					response.ContentType = ContentType.Html;
				}
			}
			else
			{
				response.ContentType = response.Request.Site.ContentType;
			}
		}

		/// <summary>
		/// Add a <see cref="IAfterDownloadCompleteHandler"/> to <see cref="IDownloader"/>
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 添加处理器
		/// </summary>
		/// <param name="handler"><see cref="IAfterDownloadCompleteHandler"/></param>
		public void AddAfterDownloadCompleteHandler(IAfterDownloadCompleteHandler handler)
		{
			_afterDownloadCompletes.Add(handler);
		}

		/// <summary>
		/// Add a <see cref="IBeforeDownloadHandler"/> to <see cref="IDownloader"/>
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 添加处理器
		/// </summary>
		/// <param name="handler"><see cref="IBeforeDownloadHandler"/></param>
		public void AddBeforeDownloadHandler(IBeforeDownloadHandler handler)
		{
			_beforeDownloads.Add(handler);
		}

		/// <summary>
		/// Clone a Downloader throuth <see cref="object.MemberwiseClone"/>, override if you need a deep clone or others. 
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 克隆一个下载器, 多线程时, 每个线程使用一个下载器, 这样如WebDriver下载器则不再需要管理WebDriver对象的个数了, 每个下载器就只包含一个WebDriver。
		/// </summary>
		/// <returns>下载器</returns>
		public virtual IDownloader Clone()
		{
			return MemberwiseClone() as IDownloader;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{
		}

		protected void EnsureSuccessStatusCode(HttpStatusCode code)
		{
			if ((int)code >= 200 && ((int)code <= 299))
			{
				return;
			}
			throw new DownloaderException($"Response status code does not indicate success: {(int)code} ({code}).");
		}

		/// <summary>
		/// Override this method to download content.
		/// </summary>
		/// <summary xml:lang="zh-CN">
		/// 下载工作的具体实现
		/// </summary>
		/// <param name="request">请求信息 <see cref="Request"/></param>
		/// <returns>页面数据 <see cref="Response"/></returns>
		protected abstract Response DowloadContent(Request request);

		private void BeforeDownload(ref Request request)
		{
			if (_beforeDownloads != null && _beforeDownloads.Count > 0)
			{
				foreach (var handler in _beforeDownloads)
				{
					handler.Handle(ref request, this);
				}
			}
		}

		private void AfterDownloadComplete(ref Response response)
		{
			if (_afterDownloadCompletes != null && _afterDownloadCompletes.Count > 0)
			{
				foreach (var handler in _afterDownloadCompletes)
				{
					handler.Handle(ref response, this);
				}
			}
		}
	}
}
