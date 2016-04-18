﻿using System.IO;
using Java2Dotnet.Spider.Core.Downloader;
using System;
using Java2Dotnet.Spider.JLog;

namespace Java2Dotnet.Spider.Core.Utils
{
	/// <summary>
	/// Base object of file persistence.
	/// </summary>
	public class FilePersistentBase
	{
		public DownloadValidation DownloadValidation { get; set; }

#if !NET_CORE
		//protected readonly ILog Logger = LogManager.GetLogger(typeof(FilePersistentBase));
		protected static readonly ILog Logger = LogManager.GetLogger();
#else
		protected readonly ILog Logger = LogManager.GetLogger();
#endif


		protected string BasePath;

		protected static string PathSeperator = "\\";

		protected void SetPath(string path)
		{
			if (!path.EndsWith(PathSeperator))
			{
				path += PathSeperator;
			}

#if !NET_CORE
			BasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
#else
			BasePath = Path.Combine(AppContext.BaseDirectory, path);
#endif
		}

		public static FileInfo PrepareFile(string fullName)
		{
			CheckAndMakeParentDirecotry(fullName);
			return new FileInfo(fullName);
		}

		public static DirectoryInfo PrepareDirectory(string fullName)
		{
			return new DirectoryInfo(CheckAndMakeParentDirecotry(fullName));
		}

		private static string CheckAndMakeParentDirecotry(string fullName)
		{
			string path = Path.GetDirectoryName(fullName);

			if (path != null)
			{
				DirectoryInfo directory = new DirectoryInfo(path);
				if (!directory.Exists)
				{
					directory.Create();
				}
			}
			return path;
		}
	}
}
