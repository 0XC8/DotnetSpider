﻿using System;
using DotnetSpider.Core.Downloader;
using DotnetSpider.Extension;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Model.Attribute;
using DotnetSpider.Extension.ORM;
using DotnetSpider.Core.Selector;
using DotnetSpider.Extension.Pipeline;
using DotnetSpider.Core;

namespace DotnetSpider.Sample
{
	public class Hao360EntitySpiderInfoBuble : EntitySpider
	{
		public Hao360EntitySpiderInfoBuble() : base("Hao360", new Site())
		{
		}

		protected override void MyInit(params string[] arguments)
		{
			Identity = "HaoBrowser Hao360Spider Buble " + DateTime.Now.ToString("yyyy-MM-dd HHmmss");
			CachedSize = 1;
			ThreadNum = 1;
			SkipWhenResultIsEmpty = true;
			var downloader = new HttpClientDownloader();
			downloader.AddAfterDownloadCompleteHandler(new SubContentHandler
			{
				Start = "sales[\"hotsite_yixing\"] = [",
				End = "}}",
				StartOffset = 27,
				EndOffset = 0
			});
			downloader.AddAfterDownloadCompleteHandler(new ReplaceContentHandler
			{
				NewValue = "/",
				OldValue = "\\/",
			});
			Scheduler = new Extension.Scheduler.RedisScheduler("127.0.0.1:6379,serviceName=Scheduler.NET,keepAlive=8,allowAdmin=True,connectTimeout=10000,password=6GS9F2QTkP36GggE0c3XwVwI,abortConnect=True,connectRetry=20");
			AddPipeline(new MySqlEntityPipeline("Database='testhao';Data Source= localhost;User ID=root;Password=root@123456;Port=3306"));
			AddStartUrl("https://hao.360.cn/");
			AddEntityType(typeof(UpdateHao360Info));
		}

		[Table("testhao", "hao360buble")]
		[EntitySelector(Expression = "$.data", Type = SelectorType.JsonPath)]
		class UpdateHao360Info : SpiderEntity
		{
			[PropertyDefine(Expression = "$.title", Type = SelectorType.JsonPath, Length = 100)]
			public string Title { get; set; }

			[PropertyDefine(Expression = "$.link", Type = SelectorType.JsonPath)]
			public string Url { get; set; }

			[PropertyDefine(Expression = "Now", Type = SelectorType.Enviroment)]
			public DateTime RunId { get; set; }

			public string Id { get; set; }
		}

		public class Hao360
		{
			public string HId { get; set; }
			public string IsBuble { get; set; }
			public string Name { get; set; }
		}
	}
}
