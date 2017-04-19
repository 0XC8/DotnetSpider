﻿using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Extension.Model;
using Newtonsoft.Json.Linq;

namespace DotnetSpider.Extension.Pipeline
{
	public abstract class BaseEntityPipeline : IEntityPipeline
	{
		public bool IsEnabled { get; protected set; } = false;
		public ISpider Spider { get; protected set; }

		public virtual void Dispose()
		{
		}

		public abstract void InitEntity(EntityMetadata metadata);

		public virtual void InitPipeline(ISpider spider)
		{
			Spider = spider;
		}

		public abstract void Process(List<JObject> datas);

		public abstract BaseEntityPipeline Clone();
	}
}
