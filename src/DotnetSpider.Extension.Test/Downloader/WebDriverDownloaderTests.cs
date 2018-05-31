﻿using DotnetSpider.Core;
using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Monitor;
using DotnetSpider.Core.Selector;
using DotnetSpider.Extension.Downloader;
using DotnetSpider.Extension.Infrastructure;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Model.Attribute;
using DotnetSpider.Extension.Model.Formatter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using DotnetSpider.Extension.Pipeline;
#if NETSTANDARD
using System.Runtime.InteropServices;
#endif

namespace DotnetSpider.Extension.Test.Downloader
{
	public class WebDriverDownloaderTests
	{
		public WebDriverDownloaderTests()
		{
			Env.HubService = false;
		}

		[Fact]
		public void DestoryDownloader()
		{
#if NETSTANDARD
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
#endif

			var chromedriverCount1 = Process.GetProcessesByName("chromedriver").Length;

			WebDriverDownloaderSpider spider = new WebDriverDownloaderSpider();
			spider.Run();

			var chromedriverCount2 = Process.GetProcessesByName("chromedriver").Length;

			Assert.Equal(chromedriverCount1, chromedriverCount2);
		}


		[Fact]
		public void ChromeHeadlessDownloader()
		{
#if NETSTANDARD
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
#endif
			HeadlessSpider spider = new HeadlessSpider();
			spider.Run();
		}

		private class HeadlessSpider : EntitySpider
		{
			public HeadlessSpider() : base("HeadlessSpider")
			{
			}

			protected override void MyInit(params string[] arguments)
			{
				Monitor = new LogMonitor();
				Identity = "HeadlessSpider";
				var word = "可乐|雪碧";
				AddStartUrl(string.Format("http://news.baidu.com/ns?word={0}&tn=news&from=news&cl=2&pn=0&rn=20&ct=1", word), new Dictionary<string, dynamic> { { "Keyword", word } });
				Downloader = new WebDriverDownloader(Browser.Chrome, new Option
				{
					Headless = true
				});
				EmptySleepTime = 6000;
				AddPipeline(new ConsoleEntityPipeline());
				AddEntityType<BaiduSearchEntry>();
			}
		}

		private class WebDriverDownloaderSpider : EntitySpider
		{
			public WebDriverDownloaderSpider() : base("WebDriverDownloader")
			{
			}

			protected override void MyInit(params string[] arguments)
			{
				var word = "可乐|雪碧";
				AddStartUrl(string.Format("http://news.baidu.com/ns?word={0}&tn=news&from=news&cl=2&pn=0&rn=20&ct=1", word), new Dictionary<string, dynamic> { { "Keyword", word } });
				Downloader = new WebDriverDownloader(Browser.Chrome);
				AddPipeline(new ConsoleEntityPipeline());
				AddEntityType<BaiduSearchEntry>();
			}
		}

		[EntityTable("baidu", "baidu_search")]
		[EntitySelector(Expression = ".//div[@class='result']", Type = SelectorType.XPath)]
		private class BaiduSearchEntry : SpiderEntity
		{
			[PropertyDefine(Expression = "Keyword", Type = SelectorType.Enviroment)]
			public string Keyword { get; set; }

			[PropertyDefine(Expression = ".//h3[@class='c-title']/a")]
			[ReplaceFormatter(NewValue = "", OldValue = "<em>")]
			[ReplaceFormatter(NewValue = "", OldValue = "</em>")]
			public string Title { get; set; }

			[PropertyDefine(Expression = ".//h3[@class='c-title']/a/@href")]
			public string Url { get; set; }

			[PropertyDefine(Expression = ".//div/p[@class='c-author']/text()")]
			[ReplaceFormatter(NewValue = "-", OldValue = "&nbsp;")]
			public string Website { get; set; }


			[PropertyDefine(Expression = ".//div/span/a[@class='c-cache']/@href")]
			public string Snapshot { get; set; }


			[PropertyDefine(Expression = ".//div[@class='c-summary c-row ']", Option = PropertyDefineOptions.InnerText)]
			[ReplaceFormatter(NewValue = "", OldValue = "<em>")]
			[ReplaceFormatter(NewValue = "", OldValue = "</em>")]
			[ReplaceFormatter(NewValue = " ", OldValue = "&nbsp;")]
			public string Details { get; set; }

			[PropertyDefine(Expression = ".", Option = PropertyDefineOptions.InnerText)]
			[ReplaceFormatter(NewValue = "", OldValue = "<em>")]
			[ReplaceFormatter(NewValue = "", OldValue = "</em>")]
			[ReplaceFormatter(NewValue = " ", OldValue = "&nbsp;")]
			public string PlainText { get; set; }

			[PropertyDefine(Expression = "today", Type = SelectorType.Enviroment)]
			public DateTime RunId { get; set; }
		}
	}
}