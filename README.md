# DotnetSpider
=================

This is a cross platfrom, ligth spider develop by C#.

### DEVELOP ENVIROMENT
- Visual Studio 2015 or later
- Want to try .NET CORE? Install 2 packages
	1. [Visual studio 2015 update 3](https://go.microsoft.com/fwlink/?LinkId=691129)	
	2. [.NET Core 1.0.0 - VS 2015 Tooling Preview 2](https://go.microsoft.com/fwlink/?LinkId=817245)
	3. [More details](https://www.microsoft.com/net/core#windows)

### DESIGN
 
![demo](http://images2015.cnblogs.com/blog/40347/201605/40347-20160511101118155-1794710718.jpg)

### SAMPLE

	Please see the Projet: DotnetSpider.Sample, I will update follow this spider's upgrade.

### BASE USAGE

Codes: https://github.com/zlzforever/DotnetSpider/blob/master/src/DotnetSpider.Sample/BaseUsage.cs

		public static void Main()
		{
			IocExtension.ServiceCollection.AddSingleton<IMonitorService, NLogMonitor>();
			
			HttpClientDownloader downloader = new HttpClientDownloader();

			Core.Spider spider = Core.Spider.Create(new MyPageProcessor(), new QueueDuplicateRemovedScheduler()).AddPipeline(new MyPipeline()).SetThreadNum(1);
			var site = new Site() { EncodingName = "UTF-8" };
			for (int i = 1; i < 5; ++i)
			{
				site.AddStartUrl("http://www.youku.com/v_olist/c_97_g__a__sg__mt__lg__q__s_1_r_0_u_0_pt_0_av_0_ag_0_sg__pr__h__d_1_p_1.html");
			}
			spider.Site = site;
			spider.Start();
		}

		private class MyPipeline : IPipeline
		{
			public void Process(ResultItems resultItems)
			{
				foreach (YoukuVideo entry in resultItems.Results["VideoResult"])
				{
					Console.WriteLine($"{entry.Name}:{entry.Click}");
				}

				//May be you want to save to database
				// 
			}

			public void Dispose()
			{
			}
		}

		private class MyPageProcessor : IPageProcessor
		{
			public void Process(Page page)
			{
				var totalVideoElements = page.Selectable.SelectList(Selectors.XPath("//div[@class='yk-col3']")).Nodes();
				List<YoukuVideo> results = new List<YoukuVideo>();
				foreach (var videoElement in totalVideoElements)
				{
					var video = new YoukuVideo();
					video.Name = videoElement.Select(Selectors.XPath("/div[4]/div[1]/a")).Value;
					video.Click = int.Parse(videoElement.Select(Selectors.Css("p-num")).Value.ToString());
					results.Add(video);
				}
				page.AddResultItem("VideoResult", results);
			}

			public Site Site => new Site { SleepTime = 0 };
		}

		public class YoukuVideo
		{
			public string Name { get; set; }
			public string Click { get; set; }
		}
	
### ADDITIONAL USAGE

#### Configurable Entity Spider

Codes: https://github.com/zlzforever/DotnetSpider/blob/master/src/DotnetSpider.Sample/JdSkuSampleSpider.cs

		public class JdSkuSampleSpider : EntitySpiderBuilder
		{
			protected override EntitySpider GetEntitySpider()
			{
				EntitySpider context = new EntitySpider(new Site());
				context.SetThreadNum(1);
				context.SetIdentity("JD_sku_store_test_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss"));
				context.AddTargetUrlExtractor(new TargetUrlExtractor
				{
					Region = new Selector { Type = ExtractType.XPath, Expression = "//span[@class=\"p-num\"]" },
					Patterns = new List<string> { @"&page=[0-9]+&" }
				});
				context.AddEntityPipeline(new MySqlEntityPipeline("Database='test';Data Source=MYSQLSERVER;User ID=root;Password=1qazZAQ!;Port=4306"));
				context.AddStartUrl("http://list.jd.com/list.html?cat=9987,653,655&page=2&JL=6_0_0&ms=5#J_main", new Dictionary<string, object> { { "name", "手机" }, { "cat3", "655" } });
				context.AddEntityType(typeof(Product));
	
				return context;
			}
	
			[Schema("test", "sku", TableSuffix.Today)]
			[TypeExtractBy(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]", Multi = true)]
			[Indexes(Index = new[] { "category" }, Unique = new[] { "category,sku", "sku" })]
			public class Product : ISpiderEntity
			{
				[StoredAs("category", DataType.String, 20)]
				[PropertyExtractBy(Expression = "name", Type = ExtractType.Enviroment)]
				public string CategoryName { get; set; }
	
				[StoredAs("cat3", DataType.String, 20)]
				[PropertyExtractBy(Expression = "cat3", Type = ExtractType.Enviroment)]
				public int CategoryId { get; set; }
	
				[StoredAs("url", DataType.Text)]
				[PropertyExtractBy(Expression = "./div[1]/a/@href")]
				public string Url { get; set; }
	
				[StoredAs("sku", DataType.String, 25)]
				[PropertyExtractBy(Expression = "./@data-sku")]
				public string Sku { get; set; }
	
				[StoredAs("commentscount", DataType.String, 32)]
				[PropertyExtractBy(Expression = "./div[5]/strong/a")]
				public long CommentsCount { get; set; }
	
				[StoredAs("shopname", DataType.String, 100)]
				[PropertyExtractBy(Expression = ".//div[@class='p-shop']/@data-shop_name")]
				public string ShopName { get; set; }
	
				[StoredAs("name", DataType.String, 50)]
				[PropertyExtractBy(Expression = ".//div[@class='p-name']/a/em")]
				public string Name { get; set; }
	
				[StoredAs("venderid", DataType.String, 25)]
				[PropertyExtractBy(Expression = "./@venderid")]
				public string VenderId { get; set; }
	
				[StoredAs("jdzy_shop_id", DataType.String, 25)]
				[PropertyExtractBy(Expression = "./@jdzy_shop_id")]
				public string JdzyShopId { get; set; }
	
				[StoredAs("run_id", DataType.Date)]
				[PropertyExtractBy(Expression = "Monday", Type = ExtractType.Enviroment)]
				public DateTime RunId { get; set; }
	
				[PropertyExtractBy(Expression = "Now", Type = ExtractType.Enviroment)]
				[StoredAs("cdate", DataType.Time)]
				public DateTime CDate { get; set; }
			}
		}

		public static void Main()
		{
			IocExtension.ServiceCollection.AddSingleton<IMonitorService, NLogMonitor>();
		
			JdSkuSampleSpider spider = new JdSkuSampleSpider();
			spider.Run();
		}

#### WebDriver Support

When you want to collect a page JS loaded, there is only one thing you need to do is set the downloader to WebDriverDownloader.	

	context.SetDownloader(new WebDriverDownloader(Browser.Chrome));

See the complete sample https://github.com/zlzforever/DotnetSpider/blob/master/src/DotnetSpider.Sample/JdSkuWebDriverSample.cs

NOTE:

1. Make sure there is a  ChromeDriver.exe in bin forlder when you set Browser to Chrome. You can contain it to your project via NUGET manager: Chromium.ChromeDriver
2. Make sure you already add a *.webdriver Firefox profile when you set Browser to Firefox: https://support.mozilla.org/en-US/kb/profile-manager-create-and-remove-firefox-profiles
3. Make sure there is a PhantomJS.exe in bin folder when you set Browser to PhantomJS. You can contain it to your project via NUGET manager: PhantomJS

### NOTICE

#### when you use redis scheduler, please update your redis config: 
	timeout 0 
	tcp-keepalive 60


### UPDATES

1.0.0.0-PRE

### AREAS FOR IMPROVEMENTS

QQ: 477731655
