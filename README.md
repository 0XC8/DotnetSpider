# DotnetSpider
[![Travis branch](https://travis-ci.org/dotnetcore/DotnetSpider.svg?branch=master)](https://travis-ci.org/dotnetcore/DotnetSpider)
[![NuGet](https://img.shields.io/nuget/v/DotnetSpider2.Extension.svg)](https://www.nuget.org/packages/DotnetSpider2.Extension)
[![NuGet Preview](https://img.shields.io/nuget/vpre/DotnetSpider2.Extension.svg?label=nuget-pre)](https://www.nuget.org/packages/DotnetSpider2.Extension/)
[![Member project of .NET China Foundation](https://img.shields.io/badge/member_project_of-.NET_CHINA-red.svg?style=flat&colorB=9E20C8)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/aur/license/yaourt.svg)](https://raw.githubusercontent.com/dotnetcore/DotnetSpider/master/LICENSE)

DotnetSpider, a .NET Standard web crawling library similar to WebMagic and Scrapy. It is a lightweight ,efficient and fast high-level web crawling & scraping framework for .NET

### DESIGN

![DESIGN](https://github.com/dotnetcore/DotnetSpider/blob/master/images/DESIGN.jpg)

### DEVELOP ENVIROMENT
- Visual Studio 2017(15.3 or later)
- [.NET Core 2.0](https://download.microsoft.com/download/0/F/D/0FD852A4-7EA1-4E2A-983A-0484AC19B92C/dotnet-sdk-2.0.0-win-x64.exe)

### OPTIONAL ENVIROMENT

- Storage data to mysql. [Download MySql](http://dev.mysql.com/get/Downloads/MySQLInstaller/mysql-installer-community-5.7.14.0.msi) 
	
		grant all on *.* to 'root'@'localhost' IDENTIFIED BY '' with grant option;
	
		flush privileges;

- Run distributed crawler. [Download Redis for windows](https://github.com/MSOpenTech/redis/releases)
- SqlServer.
- PostgreSQL.

### SAMPLES

	Please see the Projet DotnetSpider.Sample in the solution.

### BASE USAGE

[Base usage Codes](https://github.com/zlzforever/DotnetSpider/blob/master/src/DotnetSpider.Sample/BaseUsage.cs)

##### Crawler pages traversal

		public static void CrawlerPagesTraversal()
		{
			// Config encoding, header, cookie, proxy etc... 定义采集的 Site 对象, 设置 Header、Cookie、代理等
			var site = new Site { EncodingName = "UTF-8", RemoveOutboundLinks = true };

			// Set start/seed url
			site.AddStartUrl("http://www.cnblogs.com/");

			Spider spider = Spider.Create(site,
				// crawler identity
				"cnblogs_" + DateTime.Now.ToString("yyyyMMddhhmmss"),
				// use memoery queue scheduler
				new QueueDuplicateRemovedScheduler(),
				// default page processor will save whole html, and extract urls to target urls via regex
				new DefaultPageProcessor(new[] { "cnblogs\\.com" }))
				// save crawler result to file in the folder: \{running directory}\data\{crawler identity}\{guid}.dsd
				.AddPipeline(new FilePipeline());

			// dowload html by http client
			spider.Downloader = new HttpClientDownloader();
			// 4 threads 4线程
			spider.ThreadNum = 4;
			// traversal deep 遍历深度
			spider.Deep = 3;

			// stop crawler if it can't get url from the scheduler after 30000 ms 当爬虫连续30秒无法从调度中心取得需要采集的链接时结束.
			spider.EmptySleepTime = 30000;

			// start crawler 启动爬虫
			spider.Run();
		}

##### Custmize processor and pipeline

		public static void CustmizeProcessorAndPipeline()
		{
			// Config encoding, header, cookie, proxy etc... 定义采集的 Site 对象, 设置 Header、Cookie、代理等
			var site = new Site { EncodingName = "GB2312", RemoveOutboundLinks = true };
			//for (int i = 1; i < 5; ++i)
			//{
			//	// Add start/feed urls. 添加初始采集链接
			//	site.AddStartUrl("http://" + $"www.youku.com/v_olist/c_97_g__a__sg__mt__lg__q__s_1_r_0_u_0_pt_0_av_0_ag_0_sg__pr__h__d_1_p_{i}.html");
			//}
			site.AddStartUrl("http://www.unistrong.com/");
			Spider spider = Spider.Create(site,
				// use memoery queue scheduler. 使用内存调度
				new QueueDuplicateRemovedScheduler(),
				// use custmize processor for youku 为优酷自定义的 Processor
				new YoukuPageProcessor())
				// use custmize pipeline for youku 为优酷自定义的 Pipeline
				.AddPipeline(new YoukuPipeline());
			spider.Downloader = new HttpClientDownloader();
			spider.ThreadNum = 1;
			spider.EmptySleepTime = 3000;

			// Start crawler 启动爬虫
			spider.Run();

		}

		public class YoukuPipeline : BasePipeline
		{
			private static long count = 0;

			public override void Process(params ResultItems[] resultItems)
			{
				foreach (var resultItem in resultItems)
				{
					StringBuilder builder = new StringBuilder();
					foreach (YoukuVideo entry in resultItem.Results["VideoResult"])
					{
						count++;
						builder.Append($" [YoukuVideo {count}] {entry.Name}");
					}
					Console.WriteLine(builder);
				}

				// Other actions like save data to DB. 可以自由实现插入数据库或保存到文件
			}
		}

		public class YoukuPageProcessor : BasePageProcessor
		{
			protected override void Handle(Page page)
			{
				// 利用 Selectable 查询并构造自己想要的数据对象
				var totalVideoElements = page.Selectable.SelectList(Selectors.XPath("//div[@class='yk-pack pack-film']")).Nodes();
				List<YoukuVideo> results = new List<YoukuVideo>();
				foreach (var videoElement in totalVideoElements)
				{
					var video = new YoukuVideo();
					video.Name = videoElement.Select(Selectors.XPath(".//img[@class='quic']/@alt")).GetValue();
					results.Add(video);
				}

				// Save data object by key. 以自定义KEY存入page对象中供Pipeline调用
				page.AddResultItem("VideoResult", results);

				// Add target requests to scheduler. 解析需要采集的URL
				//foreach (var url in page.Selectable.SelectList(Selectors.XPath("//ul[@class='yk-pages']")).Links().Nodes())
				//{
				//	page.AddTargetRequest(new Request(url.GetValue(), null));
				//}
			}
		}

		public class YoukuVideo
		{
			public string Name { get; set; }
		}
	
### ADDITIONAL USAGE

#### Configurable Entity Spider

[View compelte Codes](https://github.com/zlzforever/DotnetSpider/blob/master/src/DotnetSpider.Sample/JdSkuSampleSpider.cs)

	public class JdSkuSampleSpider : EntitySpider
	{
		public JdSkuSampleSpider() : base("JdSkuSample", new Site
		{
			//HttpProxyPool = new HttpProxyPool(new KuaidailiProxySupplier("快代理API"))
		})
		{
		}

		protected override void MyInit(params string[] arguments)
		{
			ThreadNum = 1;
			// dowload html by http client
			Downloader = new HttpClientDownloader();

			// storage data to mysql, default is mysql entity pipeline, so you can comment this line. Don't miss sslmode.
			AddPipeline(new MySqlEntityPipeline("Database='mysql';Data Source=localhost;User ID=root;Password=;Port=3306;SslMode=None;"));
			AddStartUrl("http://list.jd.com/list.html?cat=9987,653,655&page=2&JL=6_0_0&ms=5#J_main", new Dictionary<string, object> { { "name", "手机" }, { "cat3", "655" } });
			AddEntityType(typeof(Product));
		}

		[Table("test", "jd_sku", TableSuffix.Monday, Indexs = new[] { "Category" }, Uniques = new[] { "Category,Sku", "Sku" })]
		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
		[TargetUrlsSelector(XPaths = new[] { "//span[@class=\"p-num\"]" }, Patterns = new[] { @"&page=[0-9]+&" })]
		public class Product : SpiderEntity
		{
			[PropertyDefine(Expression = "./@data-sku", Length = 100)]
			public string Sku { get; set; }

			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
			public string Category { get; set; }

			[PropertyDefine(Expression = "cat3", Type = SelectorType.Enviroment)]
			public int CategoryId { get; set; }

			[PropertyDefine(Expression = "./div[1]/a/@href")]
			public string Url { get; set; }

			[PropertyDefine(Expression = "./div[5]/strong/a")]
			public long CommentsCount { get; set; }

			[PropertyDefine(Expression = ".//div[@class='p-shop']/@data-shop_name", Length = 100)]
			public string ShopName { get; set; }

			[PropertyDefine(Expression = ".//div[@class='p-name']/a/em", Length = 100)]
			public string Name { get; set; }

			[PropertyDefine(Expression = "./@venderid", Length = 100)]
			public string VenderId { get; set; }

			[PropertyDefine(Expression = "./@jdzy_shop_id", Length = 100)]
			public string JdzyShopId { get; set; }

			[PropertyDefine(Expression = "Monday", Type = SelectorType.Enviroment)]
			public DateTime RunId { get; set; }
		}
	}

	public static void Main()
	{
		Startup.Run(new string[] { "-s:JdSkuSample", "-tid:JdSkuSample", "-i:guid" });
	}

#### Startup configuration

	Command: -s:[spider type name] -i:[identity] -a:[arg1,arg2...] -tid:[taskId] -n:[name] -e:[en1=value1,en2=value2,...]

1. -s: Type name of spider for example: DotnetSpider.Sample.BaiduSearchSpiderl
2. -i: Set identity.
3. -a: Pass arguments to spider's Run method.
4. -tid: Set task id.
5. -n: Set name.
6. -e: Set enviroment, for example you want to run with a customize config: -e:CONFIG=app.my.config.

#### WebDriver Support

When you want to collect a page JS loaded, there is only one thing to do, set the downloader to WebDriverDownloader.

	Downloader=new WebDriverDownloader(Browser.Chrome);

[See a complete sample](https://github.com/zlzforever/DotnetSpider/blob/master/src/DotnetSpider.Sample/JdSkuWebDriverSample.cs)

NOTE:

1. Make sure there is a  ChromeDriver.exe in bin forlder when you try to use Chrome. You can contain it to your project via NUGET manager: Chromium.ChromeDriver
2. Make sure you already add a *.webdriver Firefox profile when you try to use Firefox: https://support.mozilla.org/en-US/kb/profile-manager-create-and-remove-firefox-profiles
3. Make sure there is a PhantomJS.exe in bin folder when you try to use PhantomJS. You can contain it to your project via NUGET manager: PhantomJS

### Monitor and Database log

1. Set SystemConnection in app.config, only support mysql so far.
2. Run a spider then check data in database: dotnetspider, there are 3 tables: log, status, task_running


### Web Manager

1. Beta

### NOTICE

#### when you use redis scheduler, please update your redis config: 
	timeout 0 
	tcp-keepalive 60
### Upgrade

##### 20170829
+ A lot of restruct
+ Use DataObject instead of JObject for performace reason
+ Move entity validation to EntitySpider class, then we can detected issues when try to add a Entity class before run a spider.
+ Use App.config instead of config.ini
+ Framework will detect a pipeline from App.config(MySql or SqlServer) if user did not add.

##### 20170817
+ Upgrade to .NET CORE 2.0
+ Use multi target framework instead of two solution
+ Downgrade to .NET 45(Before is .NET451)
+ Fix some issues.

##### 20170524

+ Make cdate as a default column, and it's the time when insert one row.
+ The type of property will map to database colum
MySql: int->int(11), long->bigint(20), float->float, double->double, datetime->timestamp, string without length->text, string->varchar(n)

SqlServer: int->int(4),long->bigint(8),float->float,double->float,datetime->datetime,string without length->nvarchar(8000),string->nvarchar(n)

### AREAS FOR IMPROVEMENTS

QQ Group: 477731655
Email: zlzforever@163.com