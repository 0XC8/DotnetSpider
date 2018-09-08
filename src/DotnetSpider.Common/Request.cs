﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using Newtonsoft.Json;

namespace DotnetSpider.Common
{
	/// <summary>
	/// 链接请求
	/// </summary>
	public class Request : IDisposable
	{
		private string _url;

		#region Headers

		/// <summary>
		/// User-Agent
		/// </summary>
		public string UserAgent { get; set; }

		/// <summary>
		/// 请求链接时Referer参数的值
		/// </summary>
		public string Referer { get; set; }

		/// <summary>
		/// 请求链接时Origin参数的值
		/// </summary>
		public string Origin { get; set; }

		/// <summary>
		/// Accept
		/// </summary>
		public string Accept { get; set; }

		/// <summary>
		/// 仅在发送 POST 请求时需要设置
		/// </summary>
		public string ContentType { get; set; }

		/// <summary>
		/// 除了特殊 Header 以下的 Header
		/// </summary>
		public Headers Headers { get; set; }

		#endregion

		/// <summary>
		/// 字符编码
		/// </summary>
		public string EncodingName { get; set; }

		/// <summary>
		/// 请求链接的方法
		/// </summary>
		public HttpMethod Method { get; set; } = HttpMethod.Get;

		/// <summary>
		/// 存储此链接对应的额外数据字典
		/// </summary>
		public Dictionary<string, dynamic> Properties { get; set; } = new Dictionary<string, dynamic>();

		/// <summary>
		/// 请求此链接时需要POST的数据
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// 如果是 POST 请求, 可以设置压缩模式上传数据
		/// </summary>
		public CompressMode CompressMode { get; set; }

		/// <summary>
		/// 请求链接, 不使用 Uri 的原因是可能引起多重编码的问题
		/// </summary>
		[Required]
		public string Url
		{
			get => _url;
			set
			{
				_url = value;
				var uri = new Uri(_url);
				Host = uri.Host;
				LocalPath = uri.LocalPath;
			}
		}

		public string Host { get; private set; }

		public string LocalPath { get; private set; }

		public virtual string Identity => $"{Referer}.{Origin}.{Method}.{Content}.{Url}".ToShortMd5();

		/// <summary>
		/// 构造方法
		/// </summary>
		public Request()
		{
		}

		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="url">链接</param>
		public Request(string url) : this(url, null)
		{
		}

		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="url">链接</param>
		/// <param name="properties">额外属性</param>
		public Request(string url, Dictionary<string, dynamic> properties = null)
		{
			Url = url;
			if (properties != null)
			{
				Properties = properties;
			}
		}

		/// <summary>
		/// 设置此链接的额外信息
		/// </summary>
		/// <param name="key">键值</param>
		/// <param name="value">额外信息</param>
		public void AddProperty(string key, dynamic value)
		{
			lock (this)
			{
				if (Properties == null)
				{
					Properties = new Dictionary<string, dynamic>();
				}

				if (null == key)
				{
					return;
				}

				if (Properties.ContainsKey(key))
				{
					Properties[key] = value;
				}
				else
				{
					Properties.Add(key, value);
				}
			}
		}

		public dynamic GetProperty(string key)
		{
			lock (this)
			{
				if (Properties == null)
				{
					return null;
				}

				return Properties.ContainsKey(key) ? Properties[key] : null;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (this == obj) return true;
			if (obj == null || GetType() != obj.GetType()) return false;

			Request request = (Request)obj;

			if (!Equals(Referer, request.Referer)) return false;
			if (!Equals(Origin, request.Origin)) return false;
			if (!Equals(Method, request.Method)) return false;
			if (!Equals(Content, request.Content)) return false;

			if (Properties == null)
			{
				Properties = new Dictionary<string, object>();
			}

			if (request.Properties == null)
			{
				request.Properties = new Dictionary<string, object>();
			}

			if (Properties.Count != request.Properties.Count) return false;

			foreach (var entry in Properties)
			{
				if (!request.Properties.ContainsKey(entry.Key)) return false;
				if (!Equals(entry.Value, request.Properties[entry.Key])) return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the System.Type of the current instance.
		/// </summary>
		/// <returns>The exact runtime type of the current instance.</returns>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Properties.Clear();
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}

		public virtual Request Clone()
		{
			return (Request)MemberwiseClone();
		}
	}
}