﻿using DotnetSpider.Core;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Pipeline;
using DotnetSpider.Extension.Processor;
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
			AddEntityType(dataHandler, null);
		}

		public void AddEntityType<T>(DataHandler<T> dataHandler, string tableName) where T : ISpiderEntity
		{
			CheckIfRunning();

			EntityProcessor<T> processor = new EntityProcessor<T>(dataHandler, tableName);
			AddPageProcessor(processor);
		}

		protected override IPipeline GetDefaultPipeline()
		{
			return BaseEntityPipeline.GetPipelineFromAppConfig();
		}

		protected override void InitPipelines(params string[] arguments)
		{
			base.InitPipelines(arguments);

			if (!arguments.Contains("skip"))
			{
				var entityProcessors = PageProcessors.Where(p => p is IEntityProcessor).ToList();
				var entityPipelines = Pipelines.Where(p => p is BaseEntityPipeline).ToList();

				if (entityProcessors.Count != 0 && entityPipelines.Count == 0)
				{
					throw new SpiderException("You may miss a entity pipeline.");
				}
				foreach (var processor in entityProcessors)
				{
					foreach (var pipeline in entityPipelines)
					{
						var entityProcessor = processor as IEntityProcessor;
						if (pipeline is BaseEntityPipeline newPipeline)
						{
							if (entityProcessor != null)
							{
								newPipeline.AddEntity(entityProcessor.EntityDefine);
								newPipeline.Init();
							}
						}
					}
				}
			}
		}
	}
}