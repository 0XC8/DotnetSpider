﻿#if NET_CORE
using System.Runtime.InteropServices;
#endif
using System;
using System.Configuration;
using System.IO;
using System.Text;

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
		public const string IdColumn = "__Id";
		public const string EnvLocation = "LOCATION";
		public const string EnvConfig = "CONFIG";
		public const string EnvDbConfig = "DBCONFIG";

		public static ConnectionStringSettings SystemConnectionStringSettings { get; private set; }
		public static ConnectionStringSettings DataConnectionStringSettings { get; private set; }

		public static string RedisConnectString { get; private set; }
		public static string EmailHost { get; private set; }
		public static string EmailPort { get; private set; }
		public static string EmailAccount { get; private set; }
		public static string EmailPassword { get; private set; }
		public static string EmailDisplayName { get; private set; }
		public static bool SaveLogAndStatusToDb => SystemConnectionStringSettings != null;
		public static string GlobalDirectory { get; private set; }
		public static string BaseDirectory { get; private set; }
		public static string PathSeperator { get; private set; }

		public static string SystemConnectionString => SystemConnectionStringSettings?.ConnectionString;
		public static string DataConnectionString => DataConnectionStringSettings?.ConnectionString;

		public static string GlobalAppConfigPath;

		public static Configuration GlobalConfiguraiton;

		public static ConnectionStringSettings GlobalDataConnectionStringSettings { get; private set; }

		public static string GetAppSettings(string key)
		{
			return ConfigurationManager.AppSettings[key];
		}

		public static ConnectionStringSettings GetConnectStringSettings(string key)
		{
			return ConfigurationManager.ConnectionStrings[key];
		}

		public static void LoadConfiguration(string fileName)
		{
			var path = string.IsNullOrEmpty(fileName) ? "app.config" : (File.Exists(fileName) ? fileName : "app.config");
			var fileMap = new ExeConfigurationFileMap
			{
				ExeConfigFilename = path
			};

			var configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

			RedisConnectString = configuration.AppSettings.Settings[RedisConnectStringKey]?.Value?.Trim();
			EmailHost = configuration.AppSettings.Settings[EmailHostKey].Value?.Trim();
			EmailPort = configuration.AppSettings.Settings[EmailPortKey].Value?.Trim();
			EmailAccount = configuration.AppSettings.Settings[EmailAccountKey].Value?.Trim();
			EmailPassword = configuration.AppSettings.Settings[EmailPasswordKey].Value?.Trim();
			EmailDisplayName = configuration.AppSettings.Settings[EmailDisplayNameKey].Value?.Trim();

			SystemConnectionStringSettings = configuration.ConnectionStrings.ConnectionStrings[SystemConnectionStringKey];
			DataConnectionStringSettings = configuration.ConnectionStrings.ConnectionStrings[DataConnectionStringKey];

			if ("GLOBAL" == AppDomain.CurrentDomain.GetData(EnvDbConfig)?.ToString().ToUpper())
			{
				var globalFileMap = new ExeConfigurationFileMap
				{
					ExeConfigFilename = GlobalAppConfigPath
				};

				GlobalConfiguraiton = ConfigurationManager.OpenMappedExeConfiguration(globalFileMap, ConfigurationUserLevel.None);
				GlobalDataConnectionStringSettings = GlobalConfiguraiton.ConnectionStrings.ConnectionStrings[DataConnectionStringKey];
			}
		}

		public static void PrintLine(char word = '=')
		{
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < Console.WindowWidth; ++i)
			{
				builder.Append(word);
			}

			Console.WriteLine(builder.ToString());
		}

		static Environment()
		{
			Reload();
		}

		public static void Reload()
		{
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
			GlobalAppConfigPath = Path.Combine(GlobalDirectory, "app.config");

			var path = AppDomain.CurrentDomain.GetData(EnvConfig)?.ToString();
			LoadConfiguration(File.Exists(path) ? path : "app.config");
		}
	}
}
