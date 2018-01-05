﻿using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace DotnetSpider.Core.Downloader
{
	/// <summary>
	/// HttpClient信息封装
	/// </summary>
	public class HttpClientItem
	{
		/// <summary>
		/// <see cref="HttpClient"/>
		/// </summary>
		public HttpClient Client { get; set; }

		/// <summary>
		/// <see cref="HttpClientHandler"/>
		/// </summary>
		public HttpClientHandler Handler { get; set; }

		/// <summary>
		/// 上一次使用的时间
		/// </summary>
		public DateTime LastUsedTime { get; set; }
	}

	/// <summary>
	/// HttpClient池
	/// </summary>
	public interface IHttpClientPool
	{
#pragma warning disable CS1570 // XML comment has badly formed XML
		/// <summary>
		/// 通过不同的Hash分组, 返回对应的HttpClient
		/// 设计初衷: 某些网站会对COOKIE某部分做承上启下的检测, 因此必须保证: www.a.com/keyword=xxxx&page=1 www.a.com/keyword=xxxx&page=2 在同一个HttpClient里访问
		/// </summary>
		/// <param name="hashCode">分组的哈希</param>
		/// <param name="cookies">Cookies</param>
		/// <returns>HttpClient对象</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
#pragma warning restore CS1570 // XML comment has badly formed XML
		HttpClientItem GetHttpClient(int? hashCode = null, CookieContainer cookies = null);

		/// <summary>
		/// 设置 Cookie
		/// </summary>
		/// <param name="cookie">Cookie</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		void AddCookie(Cookie cookie);
	}
}
