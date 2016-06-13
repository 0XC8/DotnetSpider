
using Java2Dotnet.Spider.Extension.Configuration.Json;
using Newtonsoft.Json;
using System;

namespace Java2Dotnet.Spider.Extension
{
	public abstract class SpiderBuilder
	{
		protected virtual Action AfterSpiderFinished { get; }

		protected abstract SpiderContext GetSpiderContext();

		public void Run(params string[] args)
		{
#if Test
			// ת��JSON��ת����SpiderContext, ���ڲ���JsonSpiderContext�Ƿ�����
			string json = JsonConvert.SerializeObject(GetSpiderContext());
			ModelSpider spider = new ModelSpider(JsonConvert.DeserializeObject<JsonSpiderContext>(json).ToRuntimeContext());
#elif Publish
			ModelSpider spider = new ModelSpider(GetSpiderContext());
#endif
			spider.AfterSpiderFinished = AfterSpiderFinished;
			spider.Run(args);
		}
	}
}