﻿using System.Collections.Generic;
using System.Linq;
using DotnetSpider.Core;
using DotnetSpider.Core.Selector;
using DotnetSpider.Extension.Model.Attribute;
using DotnetSpider.Extension.Infrastructure;
using Newtonsoft.Json.Linq;
using System;
using DotnetSpider.Core.Infrastructure;

namespace DotnetSpider.Extension.Model
{
	public class EntityExtractor : IEntityExtractor
	{
		protected readonly List<SharedValueSelector> GlobalValues;

		public EntityDefine EntityDefine { get; }

		public DataHandler DataHandler { get; set; }

		public string Name { get; }

		public EntityExtractor(string name, List<SharedValueSelector> globalValues, EntityDefine entityDefine)
		{
			EntityDefine = entityDefine;
			Name = name;
			DataHandler = entityDefine.DataHandler;
			GlobalValues = globalValues;
		}

		public virtual List<DataObject> Extract(Page page)
		{
			List<DataObject> result = new List<DataObject>();
			if (GlobalValues != null && GlobalValues.Count > 0)
			{
				foreach (var enviromentValue in GlobalValues)
				{
					string name = enviromentValue.Name;
					var value = page.Selectable.Select(SelectorUtils.Parse(enviromentValue)).GetValue();
					page.Request.PutExtra(name, value);
				}
			}
			ISelector selector = SelectorUtils.Parse(EntityDefine.Selector);
			if (selector != null && EntityDefine.Multi)
			{
				var list = page.Selectable.SelectList(selector).Nodes();
				if (list == null || list.Count == 0)
				{
					result = null;
				}
				else
				{
					if (EntityDefine.Take > 0)
					{
						list = list.Take(EntityDefine.Take).ToList();
					}

					int index = 0;
					foreach (var item in list)
					{
						var obj = ExtractSingle(page, item, index);
						if (obj != null)
						{
							result.Add(obj);
						}
						index++;
					}
				}
			}
			else
			{
				ISelectable select = selector == null ? page.Selectable : page.Selectable.Select(selector);

				if (select != null)
				{
					var singleResult = ExtractSingle(page, select, 0);
					result = singleResult != null ? new List<DataObject> { singleResult } : null;
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		public static string GetEnviromentValue(string field, Page page, int index)
		{
			if (field.ToLower() == "url")
			{
				return page.Url;
			}

			if (field.ToLower() == "targeturl")
			{
				return page.TargetUrl;
			}

			if (field.ToLower() == "now")
			{
				return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			}

			if (field.ToLower() == "monday")
			{
				return DateTimeUtils.RunIdOfMonday;
			}

			if (field.ToLower() == "monthly")
			{
				return DateTimeUtils.RunIdOfMonthly;
			}

			if (field.ToLower() == "today")
			{
				return DateTimeUtils.RunIdOfToday;
			}

			if (field.ToLower() == "index")
			{
				return index.ToString();
			}

			if (!page.Request.ExistExtra(field))
			{
				return field;
			}
			else
			{
				return page.Request.GetExtra(field)?.ToString();
			}
		}

		private DataObject ExtractSingle(Page page, ISelectable item, int index)
		{
			DataObject dataObject = new DataObject();

			bool skip = false;
			foreach (var field in EntityDefine.Columns)
			{
				var fieldValue = ExtractField(item, page, field, index);
				if (fieldValue == null)
				{
					if (field.NotNull)
					{
						skip = true;
						break;
					}
				}
				else
				{
					dataObject.Add(field.Name, fieldValue);
				}
			}
			if (EntityDefine.TableInfo != null && EntityDefine.TableInfo.Primary == Core.Environment.IdColumn)
			{
				var id = GetEnviromentValue(Core.Environment.IdColumn, page, index);
				if (!string.IsNullOrEmpty(id))
				{
					dataObject.Add(Core.Environment.IdColumn, id);
				}
			}

			if (skip)
			{
				return null;
			}

			var result = dataObject.Count > 0 ? dataObject : null;
			if (result != null && EntityDefine.LinkToNexts != null)
			{
				foreach (var targetUrl in EntityDefine.LinkToNexts)
				{
					Dictionary<string, dynamic> extras = new Dictionary<string, dynamic>();
					if (targetUrl.Extras != null)
					{
						foreach (var extra in targetUrl.Extras)
						{
							extras.Add(extra, result[extra]);
						}
					}
					Dictionary<string, dynamic> allExtras = new Dictionary<string, dynamic>();
					foreach (var extra in page.Request.Extras.Union(extras))
					{
						allExtras.Add(extra.Key, extra.Value);
					}
					var value = result[targetUrl.PropertyName];
					if (value != null)
					{
						page.AddTargetRequest(new Request(value.ToString(), allExtras));
					}
				}

			}
			return result;
		}

		private dynamic ExtractField(ISelectable item, Page page, Column field, int index)
		{
			if (field == null)
			{
				return null;
			}
			ISelector selector = SelectorUtils.Parse(field.Selector);
			if (selector == null)
			{
				return null;
			}

			string tmpValue;
			if (selector is EnviromentSelector)
			{
				var enviromentSelector = selector as EnviromentSelector;
				tmpValue = GetEnviromentValue(enviromentSelector.Field, page, index);
				foreach (var formatter in field.Formatters)
				{
#if DEBUG
					try
					{
#endif
						tmpValue = formatter.Formate(tmpValue);
#if DEBUG
					}
					catch (Exception e)
					{
					}
#endif
				}
				return tmpValue;
			}
			else
			{
				bool needPlainText = field.Option == PropertyDefine.Options.PlainText;
				if (field.Multi)
				{
					var propertyValues = item.SelectList(selector).Nodes();

					List<string> results = new List<string>();
					foreach (var propertyValue in propertyValues)
					{
						results.Add(propertyValue.GetValue(needPlainText));
					}
					foreach (var formatter in field.Formatters)
					{
#if DEBUG
						try
						{
#endif
							results = formatter.Formate(results);
#if DEBUG
						}
						catch (Exception e)
						{
						}
#endif
					}
					return new JArray(results);
				}
				else
				{
					bool needCount = field.Option == PropertyDefine.Options.Count;
					if (needCount)
					{
						var propertyValues = item.SelectList(selector).Nodes();
						string count = propertyValues?.Count.ToString();
						count = string.IsNullOrEmpty(count) ? "-1" : count;
						return count;
					}
					else
					{
						tmpValue = item.Select(selector)?.GetValue(needPlainText);
						foreach (var formatter in field.Formatters)
						{
#if DEBUG
							try
							{
#endif
								tmpValue = formatter.Formate(tmpValue);
#if DEBUG
							}
							catch (Exception e)
							{
							}
#endif
						}
						return tmpValue;
					}
				}
			}
		}
	}
}
