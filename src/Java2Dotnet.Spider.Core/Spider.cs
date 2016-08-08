using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Java2Dotnet.Spider.Common;
using Java2Dotnet.Spider.Core.Downloader;
using Java2Dotnet.Spider.Core.Monitor;
using Java2Dotnet.Spider.Core.Pipeline;
using Java2Dotnet.Spider.Core.Processor;
using Java2Dotnet.Spider.Core.Proxy;
using Java2Dotnet.Spider.Core.Scheduler;
using Java2Dotnet.Spider.Core.Scheduler.Component;
using Java2Dotnet.Spider.Core.Utils;
using Newtonsoft.Json;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Java2Dotnet.Spider.Core
{
	/// <summary>
	/// A spider contains four modules: Downloader, Scheduler, PageProcessor and Pipeline. 
	/// </summary>
	public class Spider : ISpider
	{
		public ILogger Logger { get; set; }
		public int ThreadNum { get; private set; } = 1;
		public int Deep { get; set; } = int.MaxValue;
		public bool SpawnUrl { get; set; } = true;
		public bool SkipWhenResultIsEmpty { get; set; } = false;
		public DateTime StartTime { get; private set; }
		public DateTime FinishedTime { get; private set; } = DateTime.MinValue;
		public Site Site { get; private set; }
		public string Identity { get; }
		public List<IPipeline> Pipelines { get; private set; } = new List<IPipeline>();
		public IDownloader Downloader { get; private set; }
		public bool IsExitWhenComplete { get; set; } = true;
		public Status StatusCode => Stat;
		public IScheduler Scheduler { get; }
		public event SpiderEvent OnSuccess;
		public event SpiderClosingHandler SpiderClosing;
		public Dictionary<string, dynamic> Settings { get; } = new Dictionary<string, dynamic>();
		public string UserId { get; }
		public string TaskGroup { get; }
		public bool IsExited { get; private set; }

		public IPageProcessor PageProcessor { get; set; }
		protected List<Request> StartRequests { get; set; }
		protected static readonly int WaitInterval = 8;
		protected Status Stat = Status.Init;

		private int _waitCountLimit = 20;
		private int _waitCount;
		private bool _init;
		//private static readonly Regex IdentifyRegex = new Regex(@"^[\S]+$");
		private static bool _printedInfo;
		private FileInfo _errorRequestFile;
		private Random _random = new Random();

		/// <summary>
		/// Create a spider with pageProcessor.
		/// </summary>
		/// <param name="pageProcessor"></param>
		/// <returns></returns>
		public static Spider Create(Site site, IPageProcessor pageProcessor)
		{
			return new Spider(site, Guid.NewGuid().ToString(), null, null, pageProcessor, new QueueDuplicateRemovedScheduler());
		}

		/// <summary>
		/// Create a spider with pageProcessor and scheduler
		/// </summary>
		/// <param name="pageProcessor"></param>
		/// <param name="scheduler"></param>
		/// <returns></returns>
		public static Spider Create(Site site, IPageProcessor pageProcessor, IScheduler scheduler)
		{
			return new Spider(site, Guid.NewGuid().ToString(), null, null, pageProcessor, scheduler);
		}

		/// <summary>
		/// Create a spider with indentify, pageProcessor, scheduler.
		/// </summary>
		/// <param name="identify"></param>
		/// <param name="pageProcessor"></param>
		/// <param name="scheduler"></param>
		/// <returns></returns>
		public static Spider Create(Site site, string identify, string userid, string taskGroup, IPageProcessor pageProcessor, IScheduler scheduler)
		{
			return new Spider(site, identify, userid, taskGroup, pageProcessor, scheduler);
		}

		/// <summary>
		/// Create a spider with pageProcessor.
		/// </summary>
		/// <param name="identity"></param>
		/// <param name="pageProcessor"></param>
		/// <param name="scheduler"></param>
		protected Spider(Site site, string identity, string userid, string taskGroup, IPageProcessor pageProcessor, IScheduler scheduler)
		{
			LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(Path.Combine(SpiderEnviroment.BaseDirectory, "nlog.config"));

			if (string.IsNullOrWhiteSpace(identity))
			{
				Identity = string.IsNullOrEmpty(Site.Domain) ? Guid.NewGuid().ToString() : Site.Domain;
			}
			else
			{
				//if (!IdentifyRegex.IsMatch(identity))
				//{
				//	throw new SpiderExceptoin("任务ID不能有空字符.");
				//}
				Identity = identity;
			}

			UserId = string.IsNullOrEmpty(userid) ? "DotnetSpider" : userid;
			TaskGroup = string.IsNullOrEmpty(taskGroup) ? "DotnetSpider" : taskGroup;
			Logger = LogManager.GetCurrentClassLogger();

			_waitCount = 0;
			if (pageProcessor == null)
			{
				throw new SpiderException("PageProcessor should not be null.");
			}
			PageProcessor = pageProcessor;
			Site = site;
			if (Site == null)
			{
				throw new SpiderException("Site should not be null.");
			}
			PageProcessor.Site = site;
			StartRequests = Site.StartRequests;
			Scheduler = scheduler ?? new QueueDuplicateRemovedScheduler();
			Scheduler.Init(this);
#if !NET_CORE
			_errorRequestFile = BasePipeline.PrepareFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorRequests", Identity, "errors.txt"));
#else
			_errorRequestFile = BasePipeline.PrepareFile(Path.Combine(AppContext.BaseDirectory, "ErrorRequests", Identity, "errors.txt"));
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
		}

		/// <summary>
		/// Start with more than one threads
		/// </summary>
		/// <param name="threadNum"></param>
		/// <returns></returns>
		public virtual Spider SetThreadNum(int threadNum)
		{
			CheckIfRunning();

			if (threadNum <= 0)
			{
				throw new ArgumentException("threadNum should be more than one!");
			}

			ThreadNum = threadNum;

			return this;
		}

		/// <summary>
		/// Set wait time when no url is polled.
		/// </summary>
		/// <param name="emptySleepTime"></param>
		public void SetEmptySleepTime(int emptySleepTime)
		{
			if (emptySleepTime >= 1000)
			{
				_waitCountLimit = emptySleepTime / WaitInterval;
			}
			else
			{
				throw new SpiderException("Sleep time should be large than 1000.");
			}
		}

		/// <summary>
		/// Set startUrls of Spider. 
		/// Prior to startUrls of Site.
		/// </summary>
		/// <param name="startUrls"></param>
		/// <returns></returns>
		public Spider AddStartUrls(IList<string> startUrls)
		{
			CheckIfRunning();
			StartRequests = new List<Request>(UrlUtils.ConvertToRequests(startUrls, 1));
			return this;
		}

		/// <summary>
		/// Set startUrls of Spider. 
		/// Prior to startUrls of Site.
		/// </summary>
		/// <param name="startRequests"></param>
		/// <returns></returns>
		public Spider AddStartRequests(IList<Request> startRequests)
		{
			CheckIfRunning();
			StartRequests = new List<Request>(startRequests);
			return this;
		}

		/// <summary>
		/// Add urls to crawl.
		/// </summary>
		/// <param name="urls"></param>
		/// <returns></returns>
		public Spider AddStartUrl(params string[] urls)
		{
			foreach (string url in urls)
			{
				AddStartRequest(new Request(url, 1, null));
			}
			return this;
		}

		public Spider AddStartUrl(ICollection<string> urls)
		{
			foreach (string url in urls)
			{
				AddStartRequest(new Request(url, 1, null));
			}
			return this;
		}

		/// <summary>
		/// Add urls with information to crawl.
		/// </summary>
		/// <param name="requests"></param>
		/// <returns></returns>
		public Spider AddRequest(params Request[] requests)
		{
			foreach (Request request in requests)
			{
				AddStartRequest(request);
			}
			return this;
		}

		/// <summary>
		/// Add a pipeline for Spider
		/// </summary>
		/// <param name="pipeline"></param>
		/// <returns></returns>
		public Spider AddPipeline(IPipeline pipeline)
		{
			CheckIfRunning();
			Pipelines.Add(pipeline);
			return this;
		}

		/// <summary>
		/// Set pipelines for Spider
		/// </summary>
		/// <param name="pipelines"></param>
		/// <returns></returns>
		public Spider AddPipelines(IList<IPipeline> pipelines)
		{
			CheckIfRunning();
			foreach (var pipeline in pipelines)
			{
				AddPipeline(pipeline);
			}
			return this;
		}

		/// <summary>
		/// Clear the pipelines set
		/// </summary>
		/// <returns></returns>
		public Spider ClearPipeline()
		{
			Pipelines = new List<IPipeline>();
			return this;
		}

		/// <summary>
		/// Set the downloader of spider
		/// </summary>
		/// <param name="downloader"></param>
		/// <returns></returns>
		public Spider SetDownloader(IDownloader downloader)
		{
			CheckIfRunning();
			Downloader = downloader;
			return this;
		}

		public void InitComponent()
		{
			if (_init)
			{
				return;
			}

			Console.CancelKeyPress += ConsoleCancelKeyPress;

			if (Downloader == null)
			{
				Downloader = new HttpClientDownloader();
			}

			if (Pipelines.Count == 0)
			{
				Pipelines.Add(new FilePipeline());
			}

			foreach (var pipeline in Pipelines)
			{
				pipeline.InitPipeline(this);
			}

			if (StartRequests != null && StartRequests.Count > 0)
			{
				Logger.Info(LogInfo.Create($"添加链接到调度中心, 数量: {StartRequests.Count}.", this));
				if ((Scheduler is QueueDuplicateRemovedScheduler) || (Scheduler is PriorityScheduler))
				{
					Parallel.ForEach(StartRequests, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, request =>
					{
						Scheduler.Push(request);
					});
				}
				else
				{
					Scheduler.Load(new HashSet<Request>(StartRequests));
					ClearStartRequests();
				}
			}
			else
			{
				Logger.Info(LogInfo.Create("添加链接到调度中心, 数量: 0.", this));
			}

			_init = true;
		}

		public void Run()
		{
			CheckIfRunning();

			Stat = Status.Running;
			IsExited = false;

#if !NET_CORE
			// 开启多线程支持
			System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
#endif

			InitComponent();

			if (StartTime == DateTime.MinValue)
			{
				StartTime = DateTime.Now;
			}

			Parallel.For(0, ThreadNum, new ParallelOptions
			{
				MaxDegreeOfParallelism = ThreadNum
			}, i =>
			{
				int waitCount = 0;
				bool firstTask = false;

				var downloader = Downloader.Clone();

				while (Stat == Status.Running)
				{
					Request request = Scheduler.Poll();

					if (request == null)
					{
						if (waitCount > _waitCountLimit && IsExitWhenComplete)
						{
							Stat = Status.Finished;
							break;
						}

						// wait until new url added
						WaitNewUrl(ref waitCount);
					}
					else
					{
						waitCount = 0;

						try
						{
							ProcessRequest(request, downloader);
							Thread.Sleep(_random.Next(Site.MinSleepTime, Site.MaxSleepTime));
#if TEST
							System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
							sw.Reset();
							sw.Start();
#endif

							_OnSuccess(request);
#if TEST
							sw.Stop();
							Console.WriteLine("OnSuccess:" + (sw.ElapsedMilliseconds).ToString());
#endif
						}
						catch (Exception e)
						{
							OnError(request);
							Logger.Error(LogInfo.Create($"采集失败: {request.Url}.", this), e);
						}
						finally
						{
#if !NET_CORE
							if (Site.HttpProxyPoolEnable && request.GetExtra(Request.Proxy) != null)
							{
								Site.ReturnHttpProxyToPool((HttpHost)request.GetExtra(Request.Proxy), (int)request.GetExtra(Request.StatusCode));
							}
#endif
						}

						if (!firstTask)
						{
							Thread.Sleep(3000);
							firstTask = true;
						}
					}
				}
			});

			FinishedTime = DateTime.Now;

			SpiderClosing?.Invoke();

			foreach (IPipeline pipeline in Pipelines)
			{
				SafeDestroy(pipeline);
			}

			if (Stat == Status.Finished)
			{
				OnClose();
				Logger.Info(LogInfo.Create($"任务 {Identity} 结束, 运行时间: {(FinishedTime - StartTime).TotalSeconds} 秒.", this));
			}

			if (Stat == Status.Stopped)
			{
				Logger.Info(LogInfo.Create($"任务 {Identity} 停止成功, 运行时间: {(FinishedTime - StartTime).TotalSeconds} 秒.", this));
			}

			if (Stat == Status.Exited)
			{
				Logger.Info(LogInfo.Create($"任务 {Identity} 退出成功, 运行时间: {(FinishedTime - StartTime).TotalSeconds} 秒.", this));
			}
			//Logger.Dispose();
			IsExited = true;
		}

		public static void PrintInfo()
		{
			if (!_printedInfo)
			{
				Console.WriteLine("=============================================================");
				Console.WriteLine("== DotnetSpider is an open source .Net spider              ==");
				Console.WriteLine("== It's a light, stable, high performce spider             ==");
				Console.WriteLine("== Support multi thread, ajax page, http                   ==");
				Console.WriteLine("== Support save data to file, mysql, mssql, mongodb etc    ==");
				Console.WriteLine("== License: LGPL3.0                                        ==");
				Console.WriteLine("== Version: 0.9.10                                         ==");
				Console.WriteLine("== Author: zlzforever@163.com                              ==");
				Console.WriteLine("=============================================================");
				_printedInfo = true;
			}
		}

		public Task RunAsync()
		{
			return Task.Factory.StartNew(Run).ContinueWith(t =>
			{
				if (t.Exception != null)
				{
					Logger.Error(t.Exception.Message);
				}
			});
		}

		public Task Start()
		{
			return RunAsync();
		}

		public void Stop()
		{
			Stat = Status.Stopped;
			Logger.Warn(LogInfo.Create($"停止任务中 {Identity} ...", this));
		}

		public void Exit()
		{
			Stat = Status.Exited;
			Logger.Warn(LogInfo.Create($"退出任务中 {Identity} ...", this));
			SpiderClosing?.Invoke();
		}

		public bool IsExit { get; private set; }

		protected void OnClose()
		{
			foreach (var pipeline in Pipelines)
			{
				SafeDestroy(pipeline);
			}

			SafeDestroy(Scheduler);
			SafeDestroy(PageProcessor);
			SafeDestroy(Downloader);
		}

		protected void OnError(Request request)
		{
			lock (this)
			{
				File.AppendAllText(_errorRequestFile.FullName, JsonConvert.SerializeObject(request) + Environment.NewLine, Encoding.UTF8);
			}
			(Scheduler as IMonitorableScheduler).IncreaseErrorCounter();
		}

		protected void _OnSuccess(Request request)
		{
			(Scheduler as IMonitorableScheduler).IncreaseSuccessCounter();
			OnSuccess?.Invoke(request);
		}

		protected Page AddToCycleRetry(Request request, Site site)
		{
			Page page = new Page(request, site.ContentType);
			dynamic cycleTriedTimesObject = request.GetExtra(Request.CycleTriedTimes);
			if (cycleTriedTimesObject == null)
			{
				request.Priority = 0;
				page.AddTargetRequest(request.PutExtra(Request.CycleTriedTimes, 1));
			}
			else
			{
				int cycleTriedTimes = (int)cycleTriedTimesObject;
				cycleTriedTimes++;
				if (cycleTriedTimes >= site.CycleRetryTimes)
				{
					return null;
				}
				request.Priority = 0;
				page.AddTargetRequest(request.PutExtra(Request.CycleTriedTimes, cycleTriedTimes));
			}
			page.IsNeedCycleRetry = true;
			return page;
		}

		protected void ProcessRequest(Request request, IDownloader downloader)
		{
			Page page = null;
#if TEST
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
#endif

			try
			{
#if TEST
				sw.Reset();
				sw.Start();
#endif
				page = downloader.Download(request, this);

#if TEST
				sw.Stop();
				Console.WriteLine("Download:" + (sw.ElapsedMilliseconds).ToString());
#endif
				if (page.IsSkip)
				{
					return;
				}

#if TEST
				sw.Reset();
				sw.Start();
#endif
				PageProcessor.Process(page);
#if TEST
				sw.Stop();
				Console.WriteLine("Process:" + (sw.ElapsedMilliseconds).ToString());
#endif
			}
			//catch (Redial.RedialException re)
			//{
			//	if (Site.CycleRetryTimes > 0)
			//	{
			//		page = AddToCycleRetry(request, Site);
			//	}
			//	Logger.Warn(re.Message);
			//}
			catch (DownloadException de)
			{
				if (Site.CycleRetryTimes > 0)
				{
					page = AddToCycleRetry(request, Site);
				}
				Logger.Warn(de.Message);
			}
			catch (Exception e)
			{
				if (Site.CycleRetryTimes > 0)
				{
					page = AddToCycleRetry(request, Site);
				}
				Logger.Warn(LogInfo.Create($"解析页数数据失败: {request.Url}, 请检查您的数据抽取设置: {e.Message}", this));
			}

			//watch.Stop();
			//Logger.Info("dowloader cost time:" + watch.ElapsedMilliseconds);

			if (page == null)
			{
				OnError(request);
				return;
			}

			if (page.IsNeedCycleRetry)
			{
				ExtractAndAddRequests(page, true);
				return;
			}

			//watch.Stop();
			//Logger.Info("process cost time:" + watch.ElapsedMilliseconds);

			if (!page.MissTargetUrls)
			{
				if (!(SkipWhenResultIsEmpty && page.ResultItems.IsSkip))
				{
					ExtractAndAddRequests(page, SpawnUrl);
				}
			}
#if TEST
			sw.Reset();
			sw.Start();
#endif
			if (!page.ResultItems.IsSkip)
			{
				foreach (IPipeline pipeline in Pipelines)
				{
					pipeline.Process(page.ResultItems);
				}
				Logger.Info(LogInfo.Create($"采集: {request.Url} 成功.", this));
			}
			else
			{
				Logger.Warn(LogInfo.Create($"采集: {request.Url} 成功, 解析结果为 0.", this));
			}

#if TEST
			sw.Stop();
			Console.WriteLine("IPipeline:" + (sw.ElapsedMilliseconds).ToString());
#endif
		}

		protected void ExtractAndAddRequests(Page page, bool spawnUrl)
		{
			if (spawnUrl && page.Request.NextDepth < Deep && page.TargetRequests != null && page.TargetRequests.Count > 0)
			{
				foreach (Request request in page.TargetRequests)
				{
					AddStartRequest(request);
				}
			}
		}

		protected void CheckIfRunning()
		{
			if (Stat == Status.Running)
			{
				throw new SpiderException("Spider is already running!");
			}
		}

		private void ClearStartRequests()
		{
			lock (this)
			{
				StartRequests.Clear();
				GC.Collect();
			}
		}

		private void AddStartRequest(Request request)
		{
			Scheduler.Push(request);
		}

		private void WaitNewUrl(ref int waitCount)
		{
			Thread.Sleep(WaitInterval);
			++waitCount;
		}

		private void WaitNewUrl()
		{
			lock (this)
			{
				Thread.Sleep(WaitInterval);

				if (!IsExitWhenComplete)
				{
					return;
				}

				++_waitCount;
			}
		}

		private void SafeDestroy(object obj)
		{
			var disposable = obj as IDisposable;
			if (disposable != null)
			{
				try
				{
					disposable.Dispose();
				}
				catch (Exception e)
				{
					Logger.Warn(e.ToString());
				}
			}
		}

		private void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			Stop();
			while (!IsExited)
			{
				Thread.Sleep(1500);
			}
		}

		public void Dispose()
		{
			while (!IsExited)
			{
				Thread.Sleep(500);
			}

			OnClose();
		}
	}
}