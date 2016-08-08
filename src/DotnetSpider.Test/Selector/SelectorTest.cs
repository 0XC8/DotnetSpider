﻿using DotnetSpider.Core;
using DotnetSpider.Core.Selector;
#if !NET_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace DotnetSpider.Test.Selector
{
	[TestClass]
	public class SelectorTest
	{
		private string _html = "<div><a href='http://whatever.com/aaa'></a></div><div><a href='http://whatever.com/bbb'></a></div>";

		[TestMethod]
		public void TestChain()
		{
			Selectable selectable = new Selectable(_html, "", ContentType.Html);
			var linksWithoutChain = selectable.Links().GetValues();
			ISelectable xpath = selectable.XPath("//div");
			var linksWithChainFirstCall = xpath.Links().GetValues();
			var linksWithChainSecondCall = xpath.Links().GetValues();
			Assert.AreEqual(linksWithoutChain.Count, linksWithChainFirstCall.Count);
			Assert.AreEqual(linksWithChainFirstCall.Count, linksWithChainSecondCall.Count);
		}

		[TestMethod]
		public void TestNodes()
		{
			Selectable selectable = new Selectable(_html, "", ContentType.Html);
			var links = selectable.XPath(".//a/@href").Nodes();
			Assert.AreEqual(links[0].GetValue(), "http://whatever.com/aaa");

			var links1 = selectable.XPath(".//a/@href").GetValue();
			Assert.AreEqual(links1, "http://whatever.com/aaa");
		}
	}
}
