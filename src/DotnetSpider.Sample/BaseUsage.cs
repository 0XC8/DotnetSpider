using System;
using System.Collections.Generic;
using System.IO;
using DotnetSpider.Core;
using DotnetSpider.Core.Monitor;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Core.Processor;
using DotnetSpider.Core.Scheduler;
using DotnetSpider.Core.Selector;

namespace DotnetSpider.Sample
{
	public class BaseUsage
	{
		#region Custmize processor and pipeline ��ȫ�Զ���ҳ����������ݹܵ�

		public static void CustmizeProcessorAndPipeline()
		{
			// ����Ҫ�ɼ��� Site ����, �������� Header��Cookie�������
			var site = new Site { EncodingName = "UTF-8", RemoveOutboundLinks = true };
			for (int i = 1; i < 5; ++i)
			{
				// ��ӳ�ʼ�ɼ�����
				site.AddStartUrl("http://" + $"www.youku.com/v_olist/c_97_g__a__sg__mt__lg__q__s_1_r_0_u_0_pt_0_av_0_ag_0_sg__pr__h__d_1_p_{i}.html");
			}

			// ʹ���ڴ�Scheduler���Զ���PageProcessor���Զ���Pipeline��������
			Spider spider = Spider.Create(site,
				new QueueDuplicateRemovedScheduler(),
				new YoukuPageProcessor()).AddPipeline(new YoukuPipeline()).SetThreadNum(1);
			spider.EmptySleepTime = 3000;
			spider.Deep = 2;

			// ��������
			spider.Run();
		}

		public class YoukuPipeline : BasePipeline
		{
			private static long count = 0;

			public override void Process(ResultItems resultItems)
			{
				foreach (YoukuVideo entry in resultItems.Results["VideoResult"])
				{
					count++;
					Console.WriteLine($"[YoukuVideo {count}] {entry.Name}");
				}

				// ��������ʵ�ֲ������ݿ�򱣴浽�ļ�
			}
		}

		public class YoukuPageProcessor : BasePageProcessor
		{
			protected override void Handle(Page page)
			{
				// ���� Selectable ��ѯ�������Լ���Ҫ�����ݶ���
				var totalVideoElements = page.Selectable.SelectList(Selectors.XPath("//div[@class='yk-pack pack-film']")).Nodes();
				List<YoukuVideo> results = new List<YoukuVideo>();
				foreach (var videoElement in totalVideoElements)
				{
					var video = new YoukuVideo();
					video.Name = videoElement.Select(Selectors.XPath(".//img[@class='quic']/@alt")).GetValue();
					results.Add(video);
				}
				// ���Զ���KEY����page�����й�Pipeline����
				page.AddResultItem("VideoResult", results);

				foreach (var url in page.Selectable.SelectList(Selectors.XPath("//ul[@class='yk-pages']")).Links().Nodes())
				{
					page.AddTargetRequest(new Request(url.GetValue(), null));
				}
			}
		}

		public class YoukuVideo
		{
			public string Name { get; set; }
		}

		#endregion

		#region Crawler pages without traverse �ɼ�ָ��ҳ�治������

		public static void CrawlerPagesWithoutTraverse()
		{
			var site = new Site { EncodingName = "UTF-8", RemoveOutboundLinks = true };
			for (int i = 1; i < 5; ++i)
			{
				site.AddStartUrl("http://" + $"www.youku.com/v_olist/c_97_g__a__sg__mt__lg__q__s_1_r_0_u_0_pt_0_av_0_ag_0_sg__pr__h__d_1_p_{i}.html");
			}

			Spider spider = Spider.Create(site,
				"YOUKU_" + DateTime.Now.ToString("yyyyMMddhhmmss"),
				new QueueDuplicateRemovedScheduler(),
				new SimplePageProcessor())
				.AddPipeline(new FilePipeline())
				.SetThreadNum(2);

			spider.EmptySleepTime = 3000;

			// ��������
			spider.Run();
		}

		#endregion

		#region Crawler pages traversal ������վ

		public static void CrawlerPagesTraversal()
		{
			var site = new Site { EncodingName = "UTF-8", RemoveOutboundLinks = true };
			site.AddStartUrl("http://www.cnblogs.com/");
			Spider spider = Spider.Create(site,
				"cnblogs_" + DateTime.Now.ToString("yyyyMMddhhmmss"),
				new QueueDuplicateRemovedScheduler(),
				new DefaultPageProcessor("cnblogs\\.com"))
				.AddPipeline(new FilePipeline())
				// 4�߳�
				.SetThreadNum(4);

			// �������
			spider.Deep = 3;

			// ��������
			spider.Run();
		}

		#endregion
	}
}