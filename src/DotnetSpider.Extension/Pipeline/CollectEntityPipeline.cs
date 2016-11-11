﻿using System;
using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Extension.Model;
using Newtonsoft.Json.Linq;

namespace DotnetSpider.Extension.Pipeline
{
	public class CollectEntityPipeline : BaseEntityPipeline, ICollectEntityPipeline
	{
		private readonly List<JObject> _collector = new List<JObject>();

		public override BaseEntityPipeline Clone()
		{
			return new CollectEntityPipeline();
		}

		public override void Dispose()
		{
			_collector.Clear();
		}

		public IEnumerable<JObject> GetCollected()
		{
			return _collector;
		}

		public override void InitiEntity(EntityMetadata metadata)
		{
		}

		public override void Process(List<JObject> datas)
		{
			lock (this)
			{
				_collector.AddRange(datas);
			}
		}
	}
}
