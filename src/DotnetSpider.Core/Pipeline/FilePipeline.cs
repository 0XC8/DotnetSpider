using System;
using System.Collections;
using System.IO;
using System.Text;
using DotnetSpider.Core.Common;
using DotnetSpider.Core.Utils;
using System.Runtime.InteropServices;

using System.Linq;

namespace DotnetSpider.Core.Pipeline
{
	/// <summary>
	/// Store results in files.
	/// </summary>
	public sealed class FilePipeline : BasePipeline
	{
		/// <summary>
		/// create a FilePipeline with default path"/data/dotnetspider/"
		/// </summary>
		public FilePipeline()
		{
#if NET_CORE
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				SetPath("\\data\\files");
			}
			else
			{
				SetPath("/data/files");
			}
#else
			SetPath("\\data\\files");
#endif
		}

		public FilePipeline(string path)
		{
			SetPath(path);
		}

		public override void Process(ResultItems resultItems)
		{
			try
			{
				string filePath = $"{BasePath}{PathSeperator}{Spider.Identity}{PathSeperator}{Encrypt.Md5Encrypt(resultItems.Request.Url.ToString())}.fd";
				FileInfo file = PrepareFile(filePath);
				using (StreamWriter printWriter = new StreamWriter(file.OpenWrite(), Encoding.UTF8))
				{
					printWriter.WriteLine("url:\t" + resultItems.Request.Url);

					foreach (var entry in resultItems.Results)
					{
						var value = entry.Value as IList;
						if (value != null)
						{
							IList list = value;
							printWriter.WriteLine(entry.Key + ":");
							foreach (var o in list)
							{
								printWriter.WriteLine(o);
							}
						}
						else
						{
							printWriter.WriteLine(entry.Key + ":\t" + entry.Value);
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.Warn(e, LogInfo.Create("Write file error.", Spider));
				throw;
			}
		}
	}
}