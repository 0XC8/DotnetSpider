﻿using System.Collections.Generic;
using Java2Dotnet.Spider.Core;
using Java2Dotnet.Spider.Core.Pipeline;
using Java2Dotnet.Spider.Core.Scheduler;
using Java2Dotnet.Spider.Extension.Configuration;
using Newtonsoft.Json.Linq;

namespace Java2Dotnet.Spider.Extension.Pipeline
{
	public class LinkSpiderPipeline : CachedPipeline
	{
		public IScheduler NextSpiderScheduler { get; }
		public ISpider NextSpider { get; }
		private readonly LinkSpiderPrepareStartUrls _prepareStartUrls;
		private readonly string _entityName;

		public LinkSpiderPipeline(string entityName, IScheduler nextSpiderScheduler, ISpider nextSpider, LinkSpiderPrepareStartUrls prepareStartUrls)
		{
			NextSpiderScheduler = nextSpiderScheduler;
			NextSpider = nextSpider;
			_prepareStartUrls = prepareStartUrls;
			_entityName = entityName;
		}

		public void Initialize()
		{
		}

		private void Process(List<JObject> datas, ISpider spider)
		{
			_prepareStartUrls.Build(spider.Site, datas);

			foreach (var startRequest in spider.Site.StartRequests)
			{
				NextSpiderScheduler.Push(startRequest);
			}
		}

		protected override void Process(List<ResultItems> resultItemsList, ISpider spider)
		{
			if (resultItemsList == null || resultItemsList.Count == 0)
			{
				return;
			}

			List<JObject> list = new List<JObject>();
			foreach (var resultItems in resultItemsList)
			{
				dynamic data = resultItems.GetResultItem(_entityName);

				if (data != null)
				{
					if (data is JObject)
					{
						list.Add(data);
					}
					else
					{
						list.AddRange(data);
					}
				}
			}
			Process(list, spider);
		}
	}
}
