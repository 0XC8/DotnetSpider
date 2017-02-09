using System;
using System.Collections.Generic;
using System.IO;
using DotnetSpider.Core;
using DotnetSpider.Core.Monitor;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Core.Processor;
using DotnetSpider.Core.Scheduler;
using DotnetSpider.Core.Selector;
using DotnetSpider.Core.Downloader;

namespace DotnetSpider.Demo
{
	public class BaseUsage
	{
		#region Custmize processor and pipeline ��ȫ�Զ���ҳ����������ݹܵ�

		public static void CustmizeProcessorAndPipeline()
		{
			// Config encoding, header, cookie, proxy etc... ����ɼ��� Site ����, ���� Header��Cookie�������
			var site = new Site { EncodingName = "UTF-8", RemoveOutboundLinks = true };
			for (int i = 1; i < 5; ++i)
			{
				// Add start/feed urls. ��ӳ�ʼ�ɼ�����
				site.AddStartUrl("http://" + $"www.youku.com/v_olist/c_97_g__a__sg__mt__lg__q__s_1_r_0_u_0_pt_0_av_0_ag_0_sg__pr__h__d_1_p_{i}.html");
			}

			Spider spider = Spider.Create(site,
				// use memoery queue scheduler. ʹ���ڴ����
				new QueueDuplicateRemovedScheduler(),
				// use custmize processor for youku Ϊ�ſ��Զ���� Processor
				new YoukuPageProcessor())
				// use custmize pipeline for youku Ϊ�ſ��Զ���� Pipeline
				.AddPipeline(new YoukuPipeline())
				// dowload html by http client
				.SetDownloader(new HttpClientDownloader())
				// 1 thread
				.SetThreadNum(1);

			spider.EmptySleepTime = 3000;

			// Start crawler ��������
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

				// Other actions like save data to DB. ��������ʵ�ֲ������ݿ�򱣴浽�ļ�
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

				// Save data object by key. ���Զ���KEY����page�����й�Pipeline����
				page.AddResultItem("VideoResult", results);

				// Add target requests to scheduler. ������Ҫ�ɼ���URL
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
	}
}