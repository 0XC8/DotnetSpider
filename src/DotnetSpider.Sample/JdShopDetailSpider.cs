﻿using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Downloader;
using DotnetSpider.Core.Selector;
using DotnetSpider.Extension;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Model.Attribute;
using DotnetSpider.Extension.ORM;
using DotnetSpider.Extension.Pipeline;
using DotnetSpider.Extension.Scheduler;
using DotnetSpider.Core;
using DotnetSpider.Core.Infrastructure.Database;

namespace DotnetSpider.Sample
{
	/// <summary>
	/// 使用 PrepareStartUrls 模块 以及 RedisScheduler
	/// </summary>
	public class JdShopDetailSpider : EntitySpider
	{
		public JdShopDetailSpider() : base("JdShopDetailSpider", new Site())
		{
		}

		protected override void MyInit(params string[] arguments)
		{
			ThreadNum = 1;
			Scheduler = new RedisScheduler("127.0.0.1:6379,serviceName=Scheduler.NET,keepAlive=8,allowAdmin=True,connectTimeout=10000,password=,abortConnect=True,connectRetry=20");
			Downloader = new HttpClientDownloader();
			//Downloader.AddAfterDownloadCompleteHandler(new SubContentHandler
			//{
			//	Start = "json(",
			//	End = ");",
			//	StartOffset = 5,
			//	EndOffset = 0
			//});

			AddStartUrlBuilder(
				new DbStartUrlBuilder(Database.MySql, "Database='mysql';Data Source=localhost;User ID=root;Password=1qazZAQ!;Port=3306;SslMode=None;",
				$"SELECT * FROM jd.sku_v2_{DateTimeUtils.RunIdOfMonday} WHERE shopname is null or shopid is null order by sku",
				new[] { "sku" }, "http://chat1.jd.com/api/checkChat?my=list&pidList={0}&callback=json"));

			AddPipeline(new MySqlEntityPipeline("Database='mysql';Data Source=localhost;User ID=root;Password=1qazZAQ!;Port=3306;SslMode=None;"));
			AddEntityType(typeof(ProductUpdater));
		}

		[Table("jd", "sku_v2", TableSuffix.Monday, Primary = "Sku", UpdateColumns = new[] { "ShopId" })]
		[EntitySelector(Expression = "$.[*]", Type = SelectorType.JsonPath)]
		class ProductUpdater : SpiderEntity
		{
			[PropertyDefine(Expression = "$.pid", Type = SelectorType.JsonPath, Length = 25)]
			public string Sku { get; set; }

			[PropertyDefine(Expression = "$.seller", Type = SelectorType.JsonPath, Length = 100)]
			public string ShopName { get; set; }

			[PropertyDefine(Expression = "$.shopId", Type = SelectorType.JsonPath, Length = 25)]
			public string ShopId { get; set; }
		}
	}
}
