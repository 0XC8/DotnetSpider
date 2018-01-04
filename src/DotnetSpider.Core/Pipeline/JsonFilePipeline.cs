using System.IO;
using System.Text;
using DotnetSpider.Core.Infrastructure;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.Collections.Concurrent;
#if NET_CORE
using System.Runtime.InteropServices;
#endif

namespace DotnetSpider.Core.Pipeline
{
	/// <summary>
	/// �������л���JSON���洢���ļ���
	/// </summary>
	public class JsonFilePipeline : BaseFilePipeline
	{
		private readonly ConcurrentDictionary<string, StreamWriter> _writers = new ConcurrentDictionary<string, StreamWriter>();

		/// <summary>
		/// ���췽��
		/// </summary>
		public JsonFilePipeline() : base("json")
		{
		}

		/// <summary>
		/// ���췽��
		/// </summary>
		/// <param name="interval">���ݸ�Ŀ¼���������Ŀ¼·�������ֵ</param>
		public JsonFilePipeline(string interval) : base(interval)
		{
		}

		/// <summary>
		/// �������л���JSON���洢���ļ���
		/// </summary>
		/// <param name="resultItems">���ݽ��</param>
		/// <param name="spider">����</param>
		public override void Process(IEnumerable<ResultItems> resultItems, ISpider spider)
		{
			try
			{
				var jsonFile = Path.Combine(GetDataFolder(spider), $"{spider.Identity}.json");
				var streamWriter = GetDataWriter(jsonFile);
				foreach (var resultItem in resultItems)
				{
					streamWriter.WriteLine(JsonConvert.SerializeObject(resultItem.Results));
				}
			}
			catch (IOException e)
			{
				Logger.AllLog(spider.Identity, "Write data to json file failed.", LogLevel.Error, e);
				throw;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();
			foreach (var pair in _writers)
			{
				pair.Value.Dispose();
			}
			_writers.Clear();
		}

		private StreamWriter GetDataWriter(string file)
		{
			if (_writers.ContainsKey(file))
			{
				return _writers[file];
			}
			else
			{
				var streamWriter = new StreamWriter(File.OpenWrite(file), Encoding.UTF8);
				_writers.TryAdd(file, streamWriter);
				return streamWriter;
			}
		}
	}
}
