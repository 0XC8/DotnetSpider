﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NET_CORE
using Microsoft.Extensions.DependencyModel;
using System.Text;
#endif

namespace DotnetSpider.Core
{
	public class Startup
	{
		public static void Run(params string[] args)
		{
			Console.WriteLine("");
			Spider.PrintInfo();
			Console.WriteLine("");
			Console.ForegroundColor = ConsoleColor.Cyan;
			var commands = string.Join(" ", args);
			Console.WriteLine("Args: " + commands);
			Console.WriteLine("");
			Console.ForegroundColor = ConsoleColor.White;

#if NET_CORE
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
			Dictionary<string, string> arguments = new Dictionary<string, string>();
			foreach (var arg in args)
			{
				var results = arg.Split(':');
				if (results.Length == 2)
				{
					var key = results[0].Trim();
					if (arguments.ContainsKey(key))
					{
						arguments[key] = results[1].Trim();
					}
					else
					{
						arguments.Add(key, results[1].Trim());
					}
				}
				else if (results.Length == 1)
				{
					var key = results[0].Trim();
					if (!arguments.ContainsKey(key))
					{
						arguments.Add(key, string.Empty);
					}
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Please use command like: -s:[spider type name] -i:[identity] -a:[arg1,arg2...] -tid:[taskId]");
					Console.ForegroundColor = ConsoleColor.White;
					return;
				}
			}
			string spiderName;
			if (arguments.Count == 0 || !arguments.ContainsKey("-s") || !arguments.ContainsKey("-tid"))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("-s or -tid are necessary.");
				Console.ForegroundColor = ConsoleColor.White;
				return;
			}
			else
			{
				spiderName = arguments["-s"];
			}

#if NET_CORE
			var deps = DependencyContext.Default;
#endif

			var spiders = new Dictionary<string, object>();
#if NET_CORE
			foreach (var library in deps.CompileLibraries.Where(l => l.Name.ToLower().EndsWith("dotnetspider.sample") || l.Name.ToLower().EndsWith("spiders") || l.Name.ToLower().EndsWith("crawlers")))
			{
				var asm = Assembly.Load(new AssemblyName(library.Name));
				var types = asm.GetTypes();
#else
			foreach (var file in DetectDlls())
			{
				var asm = Assembly.LoadFrom(file);
				var types = asm.GetTypes();
#endif
				Console.WriteLine($"Fetch assembly: {asm.FullName}.");
				foreach (var type in types)
				{
					bool hasNonParametersConstructor = type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0);

					if (hasNonParametersConstructor)
					{
						var interfaces = type.GetInterfaces();

						var isNamed = interfaces.Any(t => t.FullName == "DotnetSpider.Core.INamed");
						var isIdentity = interfaces.Any(t => t.FullName == "DotnetSpider.Core.IIdentity");
						var isRunnable = interfaces.Any(t => t.FullName == "DotnetSpider.Core.IRunable");

						if (isNamed && isRunnable && isIdentity)
						{
							var property = type.GetProperties().First(p => p.Name == "Name");
							object runner = Activator.CreateInstance(type);
							var name = (string)property.GetValue(runner);
							if (!spiders.ContainsKey(name))
							{
								spiders.Add(name, runner);
							}
							else
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine();
								Console.WriteLine($"Spider {name} are duplicate.");
								Console.WriteLine();
								Console.ForegroundColor = ConsoleColor.White;
								return;
							}
						}
					}
				}
			}

			if (spiders.Count == 0)
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine();
				Console.WriteLine("Did not detect any spider.");
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.White;
				return;
			}

			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"Detected {spiders.Keys.Count} crawlers.");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine();
			Console.WriteLine("=================================================================");
			Console.WriteLine();

			if (!spiders.ContainsKey(spiderName))
			{
				Console.WriteLine($"There is no spider named: {spiderName}.");
				return;
			}
			var spider = spiders[spiderName];
			string identity = "";
			if (arguments.ContainsKey("-i"))
			{
				var property = spider.GetType().GetProperties().First(p => p.Name == "Identity");
				identity = arguments["-i"].ToLower();
				if (arguments["-i"].ToLower() == "guid")
				{
					property.SetValue(spider, Guid.NewGuid().ToString("N"));
				}
				else
				{
					if (!string.IsNullOrEmpty(identity))
					{
						property.SetValue(spider, arguments["-i"]);
					}
				}
			}

			if (arguments.ContainsKey("-tid"))
			{
				var property = spider.GetType().GetProperties().First(p => p.Name == "TaskId");
				if (arguments["-tid"].ToLower() == "guid")
				{
					property.SetValue(spider, Guid.NewGuid().ToString("N"));
				}
				else
				{
					if (!string.IsNullOrEmpty(identity))
					{
						property.SetValue(spider, arguments["-tid"]);
					}
				}
			}

			var spiderType = spider.GetType();
			var method = spiderType.GetMethod("Run");

			if (!arguments.ContainsKey("-a"))
			{
				method.Invoke(spider, new object[] { new string[] { } });
			}
			else
			{
				var parameters = arguments["-a"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (parameters.Contains("report"))
				{
					var emptySleepTime = spiderType.GetProperty("EmptySleepTime");
					if (emptySleepTime != null)
					{
						emptySleepTime.SetValue(spider, 1000);
					}
				}

				method.Invoke(spider, new object[] { parameters });
			}
		}

#if !NET_CORE
		private static List<string> DetectDlls()
		{
			var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
			return System.IO.Directory.GetFiles(path).Where(f => f.ToLower().EndsWith("dotnetspider.sample.exe") || f.ToLower().EndsWith("dotnetspider.sample.dll") || f.ToLower().EndsWith("spiders.dll") || f.ToLower().EndsWith("spiders.exe") || f.ToLower().EndsWith("crawlers.dll") || f.ToLower().EndsWith("crawlers.exe")).ToList();
		}
#endif
	}
}