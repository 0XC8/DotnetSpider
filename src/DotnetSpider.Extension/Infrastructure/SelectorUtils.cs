﻿using Cassandra;
using DotnetSpider.Core;
using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Selector;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Model.Attribute;
using System;

namespace DotnetSpider.Extension.Infrastructure
{
	public class SelectorUtils
	{
		public static dynamic GetEnviromentValue(string field, Page page, int index)
		{
			switch (field.ToLower())
			{
				case "timeuuid":
					{
						return TimeUuid.NewId();
					}
				case "url":
					{
						return page.Url;
					}
				case "targeturl":
					{
						return page.TargetUrl;
					}
				case "now":
					{
						return DateTime.Now;
					}
				case "monday":
					{
						return DateTimeUtils.MondayOfCurrentWeek;
					}
				case "today":
					{
						return DateTime.Now.Date;
					}
				case "monthly":
					{
						return DateTimeUtils.FirstDayOfCurrentMonth;
					}
				case "index":
					{
						return index;
					}
				default:
					{
						if (!page.Request.ExistExtra(field))
						{
							return field;
						}
						else
						{
							return page.Request.GetExtra(field)?.ToString();
						}
					}
			}
		}

		public static ISelector Parse(BaseSelector selector)
		{
			if (selector != null)
			{
				string expression = selector.Expression;

				switch (selector.Type)
				{
					case SelectorType.Css:
						{
							NotNullExpression(selector);
							return Selectors.Css(expression);
						}
					case SelectorType.Enviroment:
						{
							return Selectors.Enviroment(expression);
						}
					case SelectorType.JsonPath:
						{
							NotNullExpression(selector);
							return Selectors.JsonPath(expression);
						}
					case SelectorType.Regex:
						{
							NotNullExpression(selector);
							if (string.IsNullOrEmpty(selector.Argument))
							{
								return Selectors.Regex(expression);
							}
							else
							{
								if (int.TryParse(selector.Argument, out var group))
								{
									return Selectors.Regex(expression, group);
								}
								throw new SpiderException("Regex argument should be a number set to group: " + selector);
							}
						}
					case SelectorType.XPath:
						{
							NotNullExpression(selector);
							return Selectors.XPath(expression);
						}
					default:
						{
							throw new SpiderException($"Selector {selector} unsupoort.");
						}
				}
			}
			else
			{
				return null;
			}
		}

		public static void NotNullExpression(BaseSelector selector)
		{
			if (string.IsNullOrEmpty(selector.Expression))
			{
				throw new SpiderException($"Expression of {selector} should not be null/empty.");
			}
		}

		public static ISelector Parse(Selector selector)
		{
			if (!string.IsNullOrEmpty(selector?.Expression))
			{
				string expression = selector.Expression;

				switch (selector.Type)
				{
					case SelectorType.Css:
						{
							return Selectors.Css(expression);
						}
					case SelectorType.Enviroment:
						{
							return Selectors.Enviroment(expression);
						}
					case SelectorType.JsonPath:
						{
							return Selectors.JsonPath(expression);
						}
					case SelectorType.Regex:
						{
							return Selectors.Regex(expression);
						}
					case SelectorType.XPath:
						{
							return Selectors.XPath(expression);
						}
				}
			}

			throw new SpiderException("Not support selector: " + selector);
		}
	}
}
