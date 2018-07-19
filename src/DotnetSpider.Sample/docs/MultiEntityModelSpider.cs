﻿using DotnetSpider.Common;
using DotnetSpider.Core;
using DotnetSpider.Core.Downloader;
using DotnetSpider.Core.Infrastructure.Database;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Core.Processor;
using DotnetSpider.Core.Processor.TargetRequestExtractors;
using DotnetSpider.Core.Scheduler;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.AfterDownloadCompleteHandlers;
using DotnetSpider.Extension;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Pipeline;
using DotnetSpider.Extension.Processor;
using DotnetSpider.Extraction;
using DotnetSpider.Extraction.Model;
using DotnetSpider.Extraction.Model.Attribute;
using DotnetSpider.Extraction.Model.Formatter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotnetSpider.Sample.docs
{
	public class MultiEntityModelSpider
	{
		public static void Run()
		{
			CnblogsSpider spider = new CnblogsSpider();
			spider.Run();
		}

		private class CnblogsSpider : EntitySpider
		{
			protected override void MyInit(params string[] arguments)
			{
				Identity = ("cnblogs_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss"));
				AddStartUrl("http://www.cnblogs.com");
				AddStartUrl("https://www.cnblogs.com/news/");
				AddPipeline(new ConsoleEntityPipeline());
				AddEntityType<News>();
				AddEntityType<BlogSumary>();
			}

			[TargetRequestSelector(Patterns = new[] { "^http://www\\.cnblogs\\.com/news/$", "www\\.cnblogs\\.com/news/\\d+" })]
			[EntitySelector(Expression = "//div[@class='post_item']")]
			class News : BaseEntity
			{
				[Field(Expression = ".//a[@class='titlelnk']")]
				public string Name { get; set; }

				[Field(Expression = ".//div[@class='post_item_foot']/a[1]")]
				public string Author { get; set; }

				[Field(Expression = ".//div[@class='post_item_foot']/text()")]
				public string PublishTime { get; set; }

				[Field(Expression = ".//a[@class='titlelnk']/@href")]
				public string Url { get; set; }
			}

			[TargetRequestSelector(Patterns = new[] { "^http://www\\.cnblogs\\.com/$", "http://www\\.cnblogs\\.com/sitehome/p/\\d+" })]
			[EntitySelector(Expression = "//div[@class='post_item']")]
			class BlogSumary : BaseEntity
			{
				[Field(Expression = ".//a[@class='titlelnk']")]
				public string Name { get; set; }

				[Field(Expression = ".//div[@class='post_item_foot']/a[1]")]
				public string Author { get; set; }

				[Field(Expression = ".//div[@class='post_item_foot']/text()")]
				public string PublishTime { get; set; }

				[Field(Expression = ".//a[@class='titlelnk']/@href")]
				public string Url { get; set; }
			}
		}
	}
}
