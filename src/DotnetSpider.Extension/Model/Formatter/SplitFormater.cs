﻿using System;

namespace DotnetSpider.Extension.Model.Formatter
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SplitFormatter : Formatter
	{
		public string[] Splitors { get; set; }
		public int Index { get; set; }

		public override string Formate(string value)
		{
			var result = value.Split(Splitors, StringSplitOptions.RemoveEmptyEntries);
			if (result.Length > Index)
			{
				return result[Index];
			}
			else
			{
				return result[result.Length - 1];
			}
		}
	}
}
