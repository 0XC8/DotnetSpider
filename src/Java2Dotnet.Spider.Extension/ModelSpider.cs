﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Java2Dotnet.Spider.Core;
using Java2Dotnet.Spider.Core.Scheduler;
using Java2Dotnet.Spider.Extension.Configuration;
using Java2Dotnet.Spider.Extension.Model;
using Java2Dotnet.Spider.Extension.ORM;
using Java2Dotnet.Spider.Extension.Pipeline;
using Java2Dotnet.Spider.Extension.Processor;
using Java2Dotnet.Spider.Common;
using Java2Dotnet.Spider.Redial;
using Java2Dotnet.Spider.Redial.NetworkValidater;
using Java2Dotnet.Spider.Redial.RedialManager;
using Java2Dotnet.Spider.Validation;
using System.Linq;
using System.Threading.Tasks;
using Java2Dotnet.Spider.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using Java2Dotnet.Spider.Core.Monitor;
using System.Net;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;
using NLog;
#if NET_45
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
#endif

namespace Java2Dotnet.Spider.Extension
{
	public class ModelSpider
	{
		private const string InitStatusSetName = "init-status";
		private const string ValidateStatusName = "validate-status";
		protected readonly ILogger Logger;
		protected ConnectionMultiplexer Redis;
		protected IDatabase Db;
		protected Core.Spider spider;
		protected readonly SpiderContext SpiderContext;
		public Action AfterSpiderFinished { get; set; }
		public string Name { get; }

		public ModelSpider(SpiderContext spiderContext)
		{
#if NET_CORE
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
			SpiderContext = spiderContext;

			if (!SpiderContext.IsBuilt)
			{
				SpiderContext.Build();
			}

			Name = SpiderContext.SpiderName;

			Logger = LogManager.GetCurrentClassLogger();

			InitEnvoriment();
		}

		private void InitEnvoriment()
		{
			if (SpiderContext.Redialer != null)
			{
				if (SpiderContext.Redialer.RedialManager == null)
				{
					SpiderContext.Redialer.RedialManager = new FileRedialManager();
				}
				SpiderContext.Redialer.RedialManager.SetRedialManager(SpiderContext.Redialer.NetworkValidater.GetNetworkValidater(), SpiderContext.Redialer.GetRedialer());
			}

			if (SpiderContext.Downloader == null)
			{
				SpiderContext.Downloader = new HttpDownloader();
			}

			if (SpiderContext.Site == null)
			{
				SpiderContext.Site = new Site();
			}
			if (!string.IsNullOrEmpty(ConfigurationManager.Get("redisHost")) && string.IsNullOrWhiteSpace(ConfigurationManager.Get("redisHost")))
			{
				var host = ConfigurationManager.Get("redisHost");

				var confiruation = new ConfigurationOptions()
				{
					ServiceName = "DotnetSpider",
					Password = ConfigurationManager.Get("redisPassword"),
					ConnectTimeout = 65530,
					KeepAlive = 8,
					ConnectRetry = 20,
					SyncTimeout = 65530,
					ResponseTimeout = 65530
				};
#if NET_CORE
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					// Lewis: This is a Workaround for .NET CORE can't use EndPoint to create Socket.
					var address = Dns.GetHostAddressesAsync(host).Result.FirstOrDefault();
					if (address == null)
					{
						throw new SpiderException("Can't resovle your host: " + host);
					}
					confiruation.EndPoints.Add(new IPEndPoint(address, 6379));
				}
				else
				{
					confiruation.EndPoints.Add(new DnsEndPoint(host, 6379));
				}
#else
				confiruation.EndPoints.Add(new DnsEndPoint(host, 6379));
#endif
				Redis = ConnectionMultiplexer.Connect(confiruation);
				Db = Redis.GetDatabase(1);
			}
		}

		public virtual void Run(params string[] args)
		{
			try
			{
				spider = PrepareSpider(args);

				if (spider == null)
				{
					return;
				}

				RegisterControl(spider);

				spider.Start();

				while (spider.StatusCode == Status.Running || spider.StatusCode == Status.Init)
				{
					Thread.Sleep(1000);
				}

				spider.Dispose();

				AfterSpiderFinished?.Invoke();

				DoValidate();
			}
			finally
			{
				SpiderMonitor.Default.Dispose();
			}
		}

		private void RegisterControl(Core.Spider spider)
		{
			if (Redis != null)
			{
				try
				{
					Redis.GetSubscriber().Subscribe($"{spider.Identity}", (c, m) =>
					{
						switch (m)
						{
							case "stop":
								{
									spider.Stop();
									break;
								}
							case "start":
								{
									spider.Start();
									break;
								}
							case "exit":
								{
									spider.Exit();
									break;
								}
						}
					});
				}
				catch
				{
					// ignored
				}
			}
		}

		private void DoValidate()
		{
			if (SpiderContext.Validations == null)
			{
				return;
			}

			string key = "locker-validate-" + Name;

			try
			{
				var validations = SpiderContext.Validations.GetValidations();

				if (validations != null && validations.Count > 0)
				{
					foreach (var validation in validations)
					{
						validation.CheckArguments();
					}
				}
				bool needInitStartRequest = true;
				if (Redis != null)
				{
					while (!Db.LockTake(key, "0", TimeSpan.FromMinutes(10)))
					{
						Thread.Sleep(1000);
					}

					var lockerValue = Db.HashGet(ValidateStatusName, Name);
					needInitStartRequest = lockerValue != "validate finished";
				}
				if (needInitStartRequest)
				{
					Logger.Info(LogInfo.Create("开始数据验证 ...", SpiderContext));

					if (validations != null && validations.Count > 0)
					{
						MailBodyBuilder builder = new MailBodyBuilder(Name, SpiderContext.Validations.Corporation);
						foreach (var validation in validations)
						{
							builder.AddValidateResult(validation.Validate());
						}
						string mailBody = builder.Build();

						using (EmailClient client = new EmailClient(SpiderContext.Validations.EmailSmtpServer, SpiderContext.Validations.EmailUser, SpiderContext.Validations.EmailPassword, SpiderContext.Validations.EmailSmtpPort))
						{
							client.SendMail(new EmaillMessage($"{Name} " + "validation report", mailBody, SpiderContext.Validations.EmailTo) { IsHtml = true });
						}
					}
				}
				else
				{
					Logger.Info(LogInfo.Create("有其他线程执行了数据验证.", SpiderContext));
				}

				if (needInitStartRequest && Redis != null)
				{
					Db.HashSet(ValidateStatusName, Name, "validate finished");
				}
			}
			catch (Exception e)
			{
				Logger.Error(e.Message, e);
			}
			finally
			{
				if (Redis != null)
				{
					Db.LockRelease(key, 0);
				}
			}
		}

		private Core.Spider PrepareSpider(params string[] args)
		{
			Logger.Info(LogInfo.Create("创建爬虫...", SpiderContext));
			bool needInitStartRequest = true;
			string key = "locker-" + Name;
			if (Db != null)
			{
				while (!Db.LockTake(key, "0", TimeSpan.FromMinutes(10)))
				{
					Thread.Sleep(1000);
				}
				var lockerValue = Db.HashGet(InitStatusSetName, Name);
				needInitStartRequest = lockerValue != "init finished";
			}
			var spider = GenerateSpider(SpiderContext.Scheduler.GetScheduler());

			if (args.Contains("rerun"))
			{
				spider.Scheduler.Clear();
				needInitStartRequest = true;
			}

			if (needInitStartRequest)
			{
				PrepareSite();
			}
			Logger.Info(LogInfo.Create("构建内部模块...", SpiderContext));
			SpiderMonitor.Default.Register(spider);
			spider.InitComponent();

			if (Db != null)
			{
				Db.LockRelease(key, 0);
			}

			return spider;
		}

		private void PrepareSite()
		{
			Logger.Info(LogInfo.Create("准备爬虫数据...", SpiderContext));
			if (SpiderContext.PrepareStartUrls != null)
			{
				foreach (var prepareStartUrl in SpiderContext.PrepareStartUrls)
				{
					prepareStartUrl.Build(SpiderContext.Site, null);
				}
			}

#if !NET_CORE
			if (SpiderContext.CookieThief != null)
			{
				string cookie = SpiderContext.CookieThief.GetCookie();
				if (cookie != "Exception!!!")
				{
					SpiderContext.Site.Cookie = cookie;
				}
			}
#endif
		}

		protected virtual Core.Spider GenerateSpider(IScheduler scheduler)
		{
			EntityProcessor processor = new EntityProcessor(SpiderContext);
			processor.TargetUrlExtractInfos = SpiderContext.TargetUrlExtractInfos?.Select(t => t.GetTargetUrlExtractInfo()).ToList();
			foreach (var entity in SpiderContext.Entities)
			{
				processor.AddEntity(entity);
			}

			EntityGeneralSpider spider = new EntityGeneralSpider(SpiderContext.Site, Name, SpiderContext.UserId, SpiderContext.TaskGroup, processor, scheduler);

			foreach (var entity in SpiderContext.Entities)
			{
				string entiyName = entity.Name;

				var schema = entity.Schema;

				List<IEntityPipeline> pipelines = new List<IEntityPipeline>();
				foreach (var pipeline in SpiderContext.Pipelines)
				{
					pipelines.Add(pipeline.GetPipeline(schema, entity));
				}
				spider.AddPipeline(new EntityPipeline(entiyName, pipelines));
			}
			spider.SetCachedSize(SpiderContext.CachedSize);
			spider.SetEmptySleepTime(SpiderContext.EmptySleepTime);
			spider.SetThreadNum(SpiderContext.ThreadNum);
			spider.Deep = SpiderContext.Deep;
			var downloader = SpiderContext.Downloader.GetDownloader();
			downloader.Handlers = SpiderContext.Downloader.Handlers;
			spider.SetDownloader(downloader);
			spider.SkipWhenResultIsEmpty = SpiderContext.SkipWhenResultIsEmpty;

			if (SpiderContext.TargetUrlsHandler != null)
			{
				spider.SetCustomizeTargetUrls(SpiderContext.TargetUrlsHandler.Handle);
			}

			return spider;
		}

		private INetworkValidater GetNetworValidater(NetworkValidater networkValidater)
		{
			switch (networkValidater.Type)
			{
				case NetworkValidater.Types.Vps:
					{
						return new Redial.NetworkValidater.VpsNetworkValidater(((Configuration.VpsNetworkValidater)networkValidater).InterfaceNum);
					}
				case NetworkValidater.Types.Defalut:
					{
						return new Redial.NetworkValidater.DefaultNetworkValidater();
					}
				case NetworkValidater.Types.Vpn:
					{
#if !NET_CORE
						return new Redial.NetworkValidater.VpnNetworkValidater(((Configuration.VpnNetworkValidater)networkValidater).VpnName);
#else
						throw new SpiderException("unsport vpn redial on linux.");
#endif
					}
			}
			return null;
		}
	}
}
