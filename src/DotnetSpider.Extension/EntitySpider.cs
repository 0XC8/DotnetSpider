﻿using DotnetSpider.Core;
using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Pipeline;
using DotnetSpider.Extension.Processor;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetSpider.Extension
{
	public abstract class EntitySpider : CommonSpider
	{
		public EntitySpider() : this(new Site())
		{
		}

		public EntitySpider(Site site) : base(site)
		{
		}

		public EntitySpider(string name) : base(name)
		{
		}

		public EntitySpider(string name, Site site) : base(name, site)
		{
		}

		public void AddEntityType<T>(string tableName = null) where T : ISpiderEntity
		{
			AddEntityType<T>(null, tableName);
		}

		public void AddEntityType<T>(DataHandler<T> dataHandler) where T : ISpiderEntity
		{
			AddEntityType<T>(dataHandler, null);
		}

		public void AddEntityType<T>(DataHandler<T> dataHandler, string tableName) where T : ISpiderEntity
		{
			CheckIfRunning();

			EntityProcessor<T> processor = new EntityProcessor<T>(Site, dataHandler);
			AddPageProcessor(processor);
		}

		protected override IPipeline GetDefaultPipeline()
		{
			return BaseEntityDbPipeline.GetPipelineFromAppConfig();
		}

		protected override void PreInitComponent(params string[] arguments)
		{
			base.PreInitComponent(arguments);

			if (arguments.Contains("skip"))
			{
				return;
			}

			foreach (var processor in PageProcessors)
			{
				var entityProcessor = processor as IEntityProcessor;
				if (entityProcessor != null)
				{
					foreach (var pipeline in Pipelines)
					{
						BaseEntityPipeline newPipeline = pipeline as BaseEntityPipeline;
						newPipeline?.AddEntity(entityProcessor.EntityDefine);
					}
				}
			}

			if (IfRequireInitStartRequests(arguments) && StartUrlBuilders != null && StartUrlBuilders.Count > 0)
			{
				for (int i = 0; i < StartUrlBuilders.Count; ++i)
				{
					var builder = StartUrlBuilders[i];
					Logger.MyLog(Identity, $"[{i + 1}] Add extra start urls to scheduler.", LogLevel.Info);
					builder.Build(Site);
				}
			}
		}
	}
}
