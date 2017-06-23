﻿using DotnetSpider.Runner;
using System;
using System.Text;
#if !NET_CORE
using System.Threading;
#endif

namespace DotnetSpider.Sample
{

	public class Program
	{
		public static void Main(string[] args)
		{
			HuazhuBrandSpider sss = new HuazhuBrandSpider();
			sss.Identity = "asdf";
			sss.Run();

#if NET_CORE
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#else
			ThreadPool.SetMinThreads(200, 200);
#endif

			Startup.Run(new string[] { "-s:BaiduSearch", "-tid:1", "-i:00001" });

			//CustomSpider1 s = new CustomSpider1();
			//s.Run();
			////ConfigurableSpider.Run();

			//// Custmize processor and pipeline 完全自定义页面解析和数据管道
			//BaseUsage.CustmizeProcessorAndPipeline();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//// Crawler pages without traverse 采集指定页面不做遍历
			//BaseUsage.CrawlerPagesWithoutTraverse();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//// Crawler pages traversal 遍历整站
			//BaseUsage.CrawlerPagesTraversal();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//DDengEntitySpider dDengEntitySpider = new DDengEntitySpider();
			//dDengEntitySpider.Run();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//Cnblogs.Run();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//CasSpider casSpider = new CasSpider();
			//casSpider.Run();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//BaiduSearchSpider baiduSearchSpider = new BaiduSearchSpider();
			//baiduSearchSpider.Run();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//JdSkuSampleSpider jdSkuSampleSpider = new JdSkuSampleSpider();
			//jdSkuSampleSpider.Run();
			//Console.WriteLine("Press any key to continue...");
			//Console.Read();

			//Situoli.Run();
		}
	}
}
