﻿#if NET_CORE
using System.Runtime.InteropServices;
#endif
using System;
using System.Configuration;
using System.IO;

namespace DotnetSpider.Core
{
	public static class Environment
	{
		public const string RedisConnectStringKey = "redisConnectString";
		public const string EmailHostKey = "emailHost";
		public const string EmailPortKey = "emailPort";
		public const string EmailAccountKey = "emailAccount";
		public const string EmailPasswordKey = "emailPassword";
		public const string EmailDisplayNameKey = "emailDisplayName";
		public const string SystemConnectionStringKey = "SystemConnection";
		public const string DataConnectionStringKey = "DataConnection";
		public const string IdColumn = "__id";

		public static readonly Configuration Configuration;
		public static ConnectionStringSettings SystemConnectionStringSettings { get; private set; }
		public static ConnectionStringSettings DataConnectionStringSettings { get; private set; }

		public static string RedisConnectString { get; private set; }
		public static string EmailHost { get; private set; }
		public static string EmailPort { get; private set; }
		public static string EmailAccount { get; private set; }
		public static string EmailPassword { get; private set; }
		public static string EmailDisplayName { get; private set; }
		public static bool SaveLogAndStatusToDb => SystemConnectionStringSettings != null;
		public static string GlobalDirectory { get; }
		public static string BaseDirectory { get; }
		public static string PathSeperator { get; }

		public static string SystemConnectionString => SystemConnectionStringSettings?.ConnectionString;
		public static string DataConnectionString => DataConnectionStringSettings?.ConnectionString;

		public static string GetAppSettings(string key)
		{
			if (Configuration == null)
			{
				return ConfigurationManager.AppSettings[key];
			}
			else
			{
				return Configuration.AppSettings.Settings[key].Value;
			}
		}

		public static ConnectionStringSettings GetConnectStringSettings(string key)
		{
			if (Configuration == null)
			{
				return ConfigurationManager.ConnectionStrings[key];
			}
			else
			{
				return Configuration.ConnectionStrings.ConnectionStrings[key];
			}
		}

		public static void LoadConfiguration(string fileName)
		{
			var configuration = ConfigurationManager.OpenExeConfiguration(fileName);

			RedisConnectString = configuration.AppSettings.Settings[RedisConnectStringKey].Value?.Trim();
			EmailHost = configuration.AppSettings.Settings[EmailHostKey].Value?.Trim();
			EmailPort = configuration.AppSettings.Settings[EmailPortKey].Value?.Trim();
			EmailAccount = configuration.AppSettings.Settings[EmailAccountKey].Value?.Trim();
			EmailPassword = configuration.AppSettings.Settings[EmailPasswordKey].Value?.Trim();
			EmailDisplayName = configuration.AppSettings.Settings[EmailDisplayNameKey].Value?.Trim();

			SystemConnectionStringSettings = configuration.ConnectionStrings.ConnectionStrings[SystemConnectionStringKey];
			DataConnectionStringSettings = configuration.ConnectionStrings.ConnectionStrings[DataConnectionStringKey];
		}

		static Environment()
		{
			RedisConnectString = ConfigurationManager.AppSettings[RedisConnectStringKey]?.Trim();
			EmailHost = ConfigurationManager.AppSettings[EmailHostKey]?.Trim();
			EmailPort = ConfigurationManager.AppSettings[EmailPortKey]?.Trim();
			EmailAccount = ConfigurationManager.AppSettings[EmailAccountKey]?.Trim();
			EmailPassword = ConfigurationManager.AppSettings[EmailPasswordKey]?.Trim();
			EmailDisplayName = ConfigurationManager.AppSettings[EmailDisplayNameKey]?.Trim();

			SystemConnectionStringSettings = ConfigurationManager.ConnectionStrings[SystemConnectionStringKey];
			DataConnectionStringSettings = ConfigurationManager.ConnectionStrings[DataConnectionStringKey];

#if !NET_CORE
			PathSeperator = "\\";
#else
			PathSeperator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";
#endif

#if !NET_CORE
			GlobalDirectory = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "DotnetSpider");
			BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#else
			BaseDirectory = AppContext.BaseDirectory;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				GlobalDirectory = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), "dotnetspider");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				GlobalDirectory = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), "dotnetspider");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				GlobalDirectory = $"C:\\Users\\{System.Environment.GetEnvironmentVariable("USERNAME")}\\Documents\\DotnetSpider\\";
			}
			else
			{
				throw new ArgumentException("Unknow OS.");
			}

			DirectoryInfo di = new DirectoryInfo(GlobalDirectory);
			if (!di.Exists)
			{
				di.Create();
			}
#endif
		}
	}
}
