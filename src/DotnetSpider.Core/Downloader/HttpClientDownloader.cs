﻿using System;
using System.Collections.Generic;
using System.IO;
#if !NET_CORE
using System.Web;
#endif
using System.Text;
using System.Net.Http;
using DotnetSpider.Core.Infrastructure;
using NLog;
using DotnetSpider.Core.Redial;
using System.Runtime.CompilerServices;

namespace DotnetSpider.Core.Downloader
{
	/// <summary>
	/// 纯HTTP下载器
	/// </summary>
	public class HttpClientDownloader : BaseDownloader
	{
		/// <summary>
		/// 定义哪些类型的内容不需要当成文件下载
		/// </summary>
		public static HashSet<string> ExcludeMediaTypes = new HashSet<string>
		{
			"text/html",
			"text/plain",
			"text/richtext",
			"text/xml",
			"text/XML",
			"text/json",
			"text/javascript",
			"application/soap+xml",
			"application/xml",
			"application/json",
			"application/x-javascript",
			"application/javascript",
			"application/x-www-form-urlencoded"
		};

		private readonly string _downloadFolder;
		private readonly bool _decodeHtml;
		private readonly int _timeout = 5;

		/// <summary>
		/// HttpClient池
		/// </summary>
		public IHttpClientPool HttpClientPool { get; set; } = new HttpClientPool();

		/// <summary>
		/// 构造方法
		/// </summary>
		public HttpClientDownloader()
		{
			_downloadFolder = Path.Combine(Env.BaseDirectory, "download");
		}

		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="timeout">下载超时时间</param>
		/// <param name="decodeHtml">下载的内容是否需要HTML解码</param>
		public HttpClientDownloader(int timeout = 5, bool decodeHtml = false) : this()
		{
			_timeout = timeout;
			_decodeHtml = decodeHtml;
		}

		/// <summary>
		/// 重置Cookie
		/// </summary>
		/// <param name="cookies">Cookies</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void ResetCookies(Cookies cookies)
		{
			HttpClientPool.ResetCookies(cookies);
		}

		protected override Page DowloadContent(Request request, ISpider spider)
		{
			HttpResponseMessage response = null;
			try
			{
				var httpMessage = GenerateHttpRequestMessage(request, spider.Site);

				HttpClientItem httpClientItem = null;
				if (spider.Site.HttpProxyPool == null)
				{
					httpClientItem = HttpClientPool.GetHttpClient(request.DownloaderGroup, spider.Cookies);
				}
				else
				{
					// TODO: 代理模式下: request.DownloaderGroup 再考虑
					var proxy = spider.Site.HttpProxyPool.GetProxy();
					request.Proxy = proxy;
					httpClientItem = HttpClientPool.GetHttpClient(proxy?.GetHashCode(), spider.Cookies);
					httpClientItem.Handler.Proxy = httpClientItem.Handler.Proxy ?? proxy;
				}

				response = NetworkCenter.Current.Execute("http", () => httpClientItem.Client.SendAsync(httpMessage).Result);
				request.StatusCode = response.StatusCode;
				response.EnsureSuccessStatusCode();

				Page page;

				if (response.Content.Headers.ContentType != null && !ExcludeMediaTypes.Contains(response.Content.Headers.ContentType.MediaType))
				{
					if (!spider.Site.DownloadFiles)
					{
						Logger.AllLog(spider.Identity, $"Ignore: {request.Url} because media type is not allowed to download.", LogLevel.Warn);
						return new Page(request) { Skip = true };
					}
					else
					{
						page = SaveFile(request, response, spider);
					}
				}
				else
				{
					page = HandleResponse(request, response, spider.Site);

					if (string.IsNullOrEmpty(page.Content))
					{
						Logger.AllLog(spider.Identity, $"Content is empty: {request.Url}.", LogLevel.Warn);
					}
				}

				page.TargetUrl = response.RequestMessage.RequestUri.AbsoluteUri;

				return page;
			}
			catch (DownloadException de)
			{
				Page page = spider.Site.CycleRetryTimes > 0 ? Spider.AddToCycleRetry(request, spider.Site) : new Page(request);

				if (page != null)
				{
					page.Exception = de;
				}
				Logger.AllLog(spider.Identity, $"Download {request.Url} failed: {de.Message}", LogLevel.Warn);

				return page;
			}
			catch (HttpRequestException he)
			{
				Page page = spider.Site.CycleRetryTimes > 0 ? Spider.AddToCycleRetry(request, spider.Site) : new Page(request);
				if (page != null)
				{
					page.Exception = he;
				}

				Logger.AllLog(spider.Identity, $"Download {request.Url} failed: {he.Message}.", LogLevel.Warn);
				return page;
			}
			catch (Exception e)
			{
				Page page = new Page(request)
				{
					Exception = e,
					Skip = true
				};

				Logger.AllLog(spider.Identity, $"Download {request.Url} failed: {e.Message}.", LogLevel.Error, e);
				return page;
			}
			finally
			{
				try
				{
					response?.Dispose();
				}
				catch (Exception e)
				{
					Logger.AllLog(spider.Identity, $"Close response fail: {e}", LogLevel.Error, e);
				}
			}
		}

		private HttpRequestMessage GenerateHttpRequestMessage(Request request, Site site)
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(request.Method, request.Url);

			var userAgentHeader = "User-Agent";
			httpRequestMessage.Headers.Add(userAgentHeader, site.Headers.ContainsKey(userAgentHeader) ? site.Headers[userAgentHeader] : site.UserAgent);

			if (!string.IsNullOrEmpty(request.Referer))
			{
				httpRequestMessage.Headers.Add("Referer", request.Referer);
			}

			if (!string.IsNullOrEmpty(request.Origin))
			{
				httpRequestMessage.Headers.Add("Origin", request.Origin);
			}

			if (!string.IsNullOrEmpty(site.Accept))
			{
				httpRequestMessage.Headers.Add("Accept", site.Accept);
			}

			var contentTypeHeader = "Content-Type";

			foreach (var header in site.Headers)
			{
				if (header.Key.ToLower() == "cookie")
				{
					continue;
				}
				if (!string.IsNullOrEmpty(header.Key) && !string.IsNullOrEmpty(header.Value) && header.Key != contentTypeHeader && header.Key != userAgentHeader)
				{
					httpRequestMessage.Headers.Add(header.Key, header.Value);
				}
			}

			if (httpRequestMessage.Method == HttpMethod.Post)
			{
				var data = string.IsNullOrEmpty(site.EncodingName) ? Encoding.UTF8.GetBytes(request.PostBody) : site.Encoding.GetBytes(request.PostBody);
				httpRequestMessage.Content = new StreamContent(new MemoryStream(data));


				if (site.Headers.ContainsKey(contentTypeHeader))
				{
					httpRequestMessage.Content.Headers.Add(contentTypeHeader, site.Headers[contentTypeHeader]);
				}

				var xRequestedWithHeader = "X-Requested-With";
				if (site.Headers.ContainsKey(xRequestedWithHeader) && site.Headers[xRequestedWithHeader] == "NULL")
				{
					httpRequestMessage.Content.Headers.Remove(xRequestedWithHeader);
				}
				else
				{
					if (!httpRequestMessage.Content.Headers.Contains(xRequestedWithHeader) && !httpRequestMessage.Headers.Contains(xRequestedWithHeader))
					{
						httpRequestMessage.Content.Headers.Add(xRequestedWithHeader, "XMLHttpRequest");
					}
				}
			}
			return httpRequestMessage;
		}

		private Page HandleResponse(Request request, HttpResponseMessage response, Site site)
		{
			string content = ReadContent(site, response);

			if (_decodeHtml)
			{
#if !NET_CORE
				content = HttpUtility.UrlDecode(HttpUtility.HtmlDecode(content), string.IsNullOrEmpty(site.EncodingName) ? Encoding.Default : site.Encoding);
#else
				content = System.Net.WebUtility.UrlDecode(System.Net.WebUtility.HtmlDecode(content));
#endif
			}

			Page page = new Page(request)
			{
				Content = content
			};

			//foreach (var header in response.Headers)
			//{
			//	page.Request.PutExtra(header.Key, header.Value);
			//}

			return page;
		}

		private string ReadContent(Site site, HttpResponseMessage response)
		{
			byte[] contentBytes = response.Content.ReadAsByteArrayAsync().Result;
			contentBytes = PreventCutOff(contentBytes);
			if (string.IsNullOrEmpty(site.EncodingName))
			{
				var charSet = response.Content.Headers.ContentType?.CharSet;
				Encoding htmlCharset = EncodingExtensions.GetEncoding(charSet, contentBytes);
				return htmlCharset.GetString(contentBytes, 0, contentBytes.Length);
			}
			else
			{
				return site.Encoding.GetString(contentBytes, 0, contentBytes.Length);
			}
		}

		private Page SaveFile(Request request, HttpResponseMessage response, ISpider spider)
		{
			var intervalPath = new Uri(request.Url).LocalPath.Replace("//", "/").Replace("/", Env.PathSeperator);
			string filePath = $"{_downloadFolder}{Env.PathSeperator}{spider.Identity}{intervalPath}";
			if (!File.Exists(filePath))
			{
				try
				{
					string folder = Path.GetDirectoryName(filePath);
					if (!string.IsNullOrEmpty(folder))
					{
						if (!Directory.Exists(folder))
						{
							Directory.CreateDirectory(folder);
						}
					}

					File.WriteAllBytes(filePath, response.Content.ReadAsByteArrayAsync().Result);
				}
				catch (Exception e)
				{
					Logger.AllLog(spider.Identity, "Storage file failed.", LogLevel.Error, e);
				}
			}
			Logger.AllLog(spider.Identity, $"Storage file: {request.Url} success.", LogLevel.Info);
			return new Page(request) { Skip = true };
		}

		private byte[] PreventCutOff(byte[] bytes)
		{
			for (int i = 0; i < bytes.Length; i++)
			{
				if (bytes[i] == 0x00)
				{
					bytes[i] = 32;
				}
			}
			return bytes;
		}
	}
}
