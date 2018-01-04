﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System;
using System.Linq;
using System.Reflection;

namespace DotnetSpider.Core.Infrastructure.Database
{
	public abstract class DbProviderFactories
	{
		/// <summary>
		/// <see cref="MySqlProvider"/>
		/// </summary>
		public const string MySqlProvider = "MySql.Data.MySqlClient";

		/// <summary>
		/// <see cref="SqlServerProvider"/>
		/// </summary>
		public const string SqlServerProvider = "System.Data.SqlClient";

		/// <summary>
		/// <see cref="PostgreSqlProvider"/>
		/// </summary>
		public const string PostgreSqlProvider = "Npgsql";

		private static readonly ConcurrentDictionary<string, DbProviderFactory> Configs =
			new ConcurrentDictionary<string, DbProviderFactory>();

		private static readonly string[] DataProviders = { "mysql.data.dll", "npgsql.dll" };

		static DbProviderFactories()
		{
			RegisterFactory(SqlServerProvider, SqlClientFactory.Instance);

			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (var assembly in allAssemblies)
			{
				var dependenceProviderNames = assembly.GetReferencedAssemblies()
					.Where(a => DataProviders.Contains($"{a.Name.ToLower()}.dll"));
				foreach (var assemblyName in dependenceProviderNames)
				{
					Assembly.Load(assemblyName);
				}
			}

			var baseProviders = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => (DataProviders.Contains($"{a.GetName().Name.ToLower()}.dll"))));
			foreach (var assembly in baseProviders)
			{
				var factoryType = assembly.GetExportedTypes()
					.FirstOrDefault(t => t.BaseType == typeof(DbProviderFactory));
				if (factoryType != null)
				{
					FieldInfo instanceField = factoryType.GetField("Instance");
					if (instanceField?.GetValue(null) is DbProviderFactory instance)
					{
						RegisterFactory(factoryType.Namespace, instance);
					}
				}
			}
			var providerDlls = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
				.Where(p => DataProviders.Contains(Path.GetFileName(p).ToLower())).ToList();
			foreach (var providerDll in providerDlls)
			{
				if (string.IsNullOrEmpty(providerDll))
				{
					continue;
				}
				var assembly = Assembly.Load(Path.GetFileName(providerDll).Replace(".dll", ""));
				var factoryType = assembly.GetExportedTypes()
					.FirstOrDefault(t => t.BaseType == typeof(DbProviderFactory));
				if (factoryType != null)
				{
					FieldInfo instanceField = factoryType.GetField("Instance");
					if (instanceField?.GetValue(null) is DbProviderFactory instance)
					{
						RegisterFactory(factoryType.Namespace, instance);
					}
				}
			}
		}

		public static DbProviderFactory GetFactory(string providerInvariantName)
		{
			if (Configs.ContainsKey(providerInvariantName))
			{
				Configs.TryGetValue(providerInvariantName, out var factory);
				if (factory == null)
				{
					throw new SpiderException("Provider not found.");
				}
				return factory;
			}
			throw new SpiderException("Provider not found.");
		}

		public static void RegisterFactory(string providerInvariantName, DbProviderFactory factory)
		{
			Configs.TryAdd(providerInvariantName, factory);
		}
	}
}