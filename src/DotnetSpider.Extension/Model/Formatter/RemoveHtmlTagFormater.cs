﻿using System;
using HtmlAgilityPack;

namespace DotnetSpider.Extension.Model.Formatter
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class RemoveHtmlTagFormater : Formatter
	{
		public override string Formate(string value)
		{
			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(value);
			return htmlDocument.DocumentNode.InnerText;
		}
	}
}
