using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotnetSpider.Core.Pipeline
{
	/// <summary>
	/// �洢���ݽ�����ļ���
	/// </summary>
	public class FilePipeline : BaseFilePipeline
	{
		/// <summary>
		/// �����ļ��е�ַΪ: {BaseDirecoty}/data/{Identity}
		/// </summary>
		public FilePipeline() : base("file")
		{
		}

		/// <summary>
		/// �����ļ��е�ַΪ: {BaseDirecoty}/data/{interval}
		/// </summary>
		public FilePipeline(string interval) : base(interval)
		{
		}

		/// <summary>
		/// �洢���ݽ�����ļ���
		/// </summary>
		/// <param name="resultItems">���ݽ��</param>
		/// <param name="spider">����</param>
		public override void Process(IEnumerable<ResultItems> resultItems, ISpider spider)
		{
			try
			{
				foreach (var resultItem in resultItems)
				{
					resultItem.Request.CountOfResults = 0;
					resultItem.Request.EffectedRows = 0;

					string filePath = Path.Combine(GetDataFolder(spider), $"{ Guid.NewGuid():N}.dsd");
					using (StreamWriter printWriter = new StreamWriter(File.OpenWrite(filePath), Encoding.UTF8))
					{
						printWriter.WriteLine("url:\t" + resultItem.Request.Url);

						foreach (var entry in resultItem.Results)
						{
							if (entry.Value is IList value)
							{
								IList list = value;
								printWriter.WriteLine(entry.Key + ":");
								foreach (var o in list)
								{
									printWriter.WriteLine(o);
								}

								resultItem.Request.CountOfResults += list.Count;
								resultItem.Request.EffectedRows += list.Count;
							}
							else
							{
								printWriter.WriteLine(entry.Key + ":\t" + entry.Value);

								resultItem.Request.CountOfResults += 1;
								resultItem.Request.EffectedRows += 1;
							}
						}
					}
				}
			}
			catch
			{
				spider.Logger.Error("Write file error.");
				throw;
			}
		}
	}
}