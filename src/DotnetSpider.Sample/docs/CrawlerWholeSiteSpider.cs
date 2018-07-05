using System;
using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Core.Processor;
using DotnetSpider.Core.Scheduler;
using DotnetSpider.Core.Selector;
using DotnetSpider.Core.Downloader;
using System.Text;

namespace DotnetSpider.Sample.docs
{
	public class CrawlerWholeSiteSpider
	{
		public static void Run()
		{
			// Config encoding, header, cookie, proxy etc... ����ɼ��� Site ����, ���� Header��Cookie�������
			var site = new Site { EncodingName = "UTF-8" };

			// Set start/seed url
			site.AddStartUrl("http://www.cnblogs.com/");

			Spider spider = Spider.Create(site,
				// use memoery queue scheduler
				new QueueDuplicateRemovedScheduler(),
				// default page processor will save whole html, and extract urls to target urls via regex
				new DefaultPageProcessor(new[] { "cnblogs\\.com" }))
				// save crawler result to file in the folder: \{running directory}\data\{crawler identity}\{guid}.dsd
				.AddPipeline(new FilePipeline());

			// dowload html by http client
			spider.Downloader = new HttpClientDownloader();
			spider.Name = "CNBLOGS";
			// 4 threads 4�߳�
			spider.ThreadNum = 4;
			spider.TaskId = "cnblogs";
			// traversal deep �������
			spider.Scheduler.Depth = 3;

			// stop crawler if it can't get url from the scheduler after 30000 ms ����������30���޷��ӵ�������ȡ����Ҫ�ɼ�������ʱ����.
			spider.EmptySleepTime = 30000;

			// start crawler ��������
			spider.Run();
		}
	}
}