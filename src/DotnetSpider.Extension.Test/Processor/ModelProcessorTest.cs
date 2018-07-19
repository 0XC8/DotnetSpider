﻿using DotnetSpider.Core;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using DotnetSpider.Extraction.Model.Attribute;
using DotnetSpider.Extraction.Model;
using DotnetSpider.Common;

namespace DotnetSpider.Extension.Test.Processor
{
	public class ModelProcessorTest
	{
		private string Html = @"
<div>
	<div class='int'>100</div>
	<div class='bool'>true</div>
	<div class='bigint'>200</div>
	<div class='string'>abcd</div>
	<div class='datetime'>2018-06-03</div>
	<div class='float'>1.1</div>
	<div class='double'>2.2</div>
	<div class='decimal'>0.8</div>
</div>
";
		[Fact(DisplayName = "DataConvert")]
		public void ModelProcess()
		{
			EntityProcessor<N> processor = new EntityProcessor<N>();
			var page = CreatePage();
			processor.Process(page, null);

			var results = page.ResultItems.GetResultItem(processor.Model.Identity) as Tuple<IModel, IEnumerable<dynamic>>;
			var model = results.Item2.First() as N;
			Assert.Equal(100, model.Int);
			Assert.True(model.Bool);
			Assert.Equal(200, model.BigInt);
			Assert.Equal("abcd", model.String);
			Assert.Equal(new DateTime(2018, 6, 3), model.DateTime);
			Assert.Equal(1.1F, model.Float);
			Assert.Equal(2.2D, model.Double);
			Assert.Equal((Decimal)0.8, model.Decimal);
		}

		private Page CreatePage()
		{
			var request = new Request("http://dotnetspoder.me");
			var site = new Site();
			request.Site = site;
			var page = new Page(request);
			page.Content = Html;
			return page;
		}

		private class N
		{
			[Field(Expression = ".//div[@class='int']")]
			public int Int { get; set; }

			[Field(Expression = ".//div[@class='bool']")]
			public bool Bool { get; set; }

			[Field(Expression = ".//div[@class='bigint']")]
			public long BigInt { get; set; }

			[Field(Expression = ".//div[@class='string']")]
			public string String { get; set; }

			[Field(Expression = ".//div[@class='datetime']")]
			public DateTime DateTime { get; set; }

			[Field(Expression = ".//div[@class='float']")]
			public float Float { get; set; }

			[Field(Expression = ".//div[@class='double']")]
			public double Double { get; set; }

			[Field(Expression = ".//div[@class='decimal']")]
			public decimal Decimal { get; set; }
		}
	}
}
