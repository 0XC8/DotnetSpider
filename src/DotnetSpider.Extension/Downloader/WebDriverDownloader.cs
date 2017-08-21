﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Core.Downloader;
using DotnetSpider.Core.Infrastructure;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium;
using System.Net.Http;
using DotnetSpider.Core.Redial;
using System.Runtime.InteropServices;
using DotnetSpider.Extension.Infrastructure;
#if NET_CORE
using System.Net;
#else
using System.Web;
#endif

namespace DotnetSpider.Extension.Downloader
{
	public class WebDriverDownloader : BaseDownloader
	{
		private IWebDriver _webDriver;
		private readonly int _webDriverWaitTime;
		private bool _isLogined;
		private readonly Browser _browser;
		private readonly Option _option;
		public LoginHandler Login { get; set; }
		public Func<string, string> UrlHandler;
		public IWebDriverHandler NavigateCompeleted;
		private bool _isDisposed;

		public WebDriverDownloader(Browser browser, int webDriverWaitTime = 200, Option option = null)
		{
			_webDriverWaitTime = webDriverWaitTime;
			_browser = browser;
			_option = option ?? new Option();

			if (browser == Browser.Firefox && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Task.Factory.StartNew(() =>
				{
					while (!_isDisposed)
					{
						IntPtr maindHwnd = WindowsFormUtil.FindWindow(null, "plugin-container.exe - 应用程序错误");
						if (maindHwnd != IntPtr.Zero)
						{
							WindowsFormUtil.SendMessage(maindHwnd, WindowsFormUtil.WmClose, 0, 0);
						}
						Thread.Sleep(500);
					}
				});
			}
		}

		public WebDriverDownloader(Browser browser) : this(browser, 300)
		{
		}

		public WebDriverDownloader(Browser browser, LoginHandler loginHandler) : this(browser, 200)
		{
			Login = loginHandler;
		}

		protected override Page DowloadContent(Request request, ISpider spider)
		{
			Site site = spider.Site;
			try
			{
				lock (this)
				{
					_webDriver = _webDriver ?? WebDriverExtensions.Open(_browser, _option);

					if (!_isLogined && Login != null)
					{
						_isLogined = Login.Handle(_webDriver as RemoteWebDriver);
						if (!_isLogined)
						{
							throw new DownloadException("Login failed. Please check your login codes.");
						}
					}
				}

				//中文乱码URL
				Uri uri = request.Url;
#if NET_CORE
				string query = string.IsNullOrEmpty(uri.Query) ? "" : $"?{WebUtility.UrlEncode(uri.Query.Substring(1, uri.Query.Length - 1))}";
#else
				string query = string.IsNullOrEmpty(uri.Query) ? "" : $"?{HttpUtility.UrlPathEncode(uri.Query.Substring(1, uri.Query.Length - 1))}";
#endif
				string realUrl = $"{uri.Scheme}://{uri.DnsSafeHost}{(uri.Port == 80 ? "" : ":" + uri.Port)}{uri.AbsolutePath}{query}";

				var domainUrl = $"{uri.Scheme}://{uri.DnsSafeHost}{(uri.Port == 80 ? "" : ":" + uri.Port)}";
				var options = _webDriver.Manage();
				if (options.Cookies.AllCookies.Count == 0 && spider.Site.Cookies.PairPart.Count > 0)
				{
					_webDriver.Url = domainUrl;
					options.Cookies.DeleteAllCookies();
					foreach (var c in spider.Site.Cookies.PairPart)
					{
						options.Cookies.AddCookie(new OpenQA.Selenium.Cookie(c.Key, c.Value));
					}
				}

				if (UrlHandler != null)
				{
					realUrl = UrlHandler(realUrl);
				}

				NetworkCenter.Current.Execute("wd-d", () =>
				{
					_webDriver.Navigate().GoToUrl(realUrl);

					NavigateCompeleted?.Handle((RemoteWebDriver)_webDriver);
				});

				Thread.Sleep(_webDriverWaitTime);

				Page page = new Page(request, site.RemoveOutboundLinks ? site.Domains : null)
				{
					Content = _webDriver.PageSource,
					TargetUrl = _webDriver.Url,
					Title = _webDriver.Title
				};

				// 结束后要置空, 这个值存到Redis会导置无限循环跑单个任务
				request.PutExtra(Request.CycleTriedTimes, null);
				return page;
			}
			catch (DownloadException de)
			{
				Page page = new Page(request, null) { Exception = de };
				if (site.CycleRetryTimes > 0)
				{
					page = Spider.AddToCycleRetry(request, site);
				}
				Logger.MyLog(spider.Identity, $"下载 {request.Url} 失败: {de.Message}.", NLog.LogLevel.Warn);
				return page;
			}
			catch (HttpRequestException he)
			{
				Page page = new Page(request, null) { Exception = he };
				if (site.CycleRetryTimes > 0)
				{
					page = Spider.AddToCycleRetry(request, site);
				}
				Logger.MyLog(spider.Identity, $"下载 {request.Url} 失败: {he.Message}.", NLog.LogLevel.Warn);
				return page;
			}
			catch (Exception e)
			{
				Page page = new Page(request, null) { Exception = e };
				return page;
			}
		}

		public override void Dispose()
		{
			_isDisposed = true;
			_webDriver?.Quit();
		}
	}
}
