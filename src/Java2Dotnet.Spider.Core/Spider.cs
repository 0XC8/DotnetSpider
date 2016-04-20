using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
using Java2Dotnet.Spider.JLog;

using Newtonsoft.Json;
using System.Net.Http;

namespace Java2Dotnet.Spider.Core
{
	/// <summary>
	/// A spider contains four modules: Downloader, Scheduler, PageProcessor and Pipeline. 
	/// </summary>
	public class Spider : ISpider
	{

#if !NET_CORE
		//protected static readonly ILog Logger = LogManager.GetLogger(typeof(Spider));
		protected static readonly ILog Logger = LogManager.GetLogger();
#else
		protected static readonly ILog Logger = LogManager.GetLogger();
#endif

		public int ThreadNum { get; set; } = 1;
		public int Deep { get; set; } = int.MaxValue;
		public AutomicLong FinishedPageCount { get; set; } = new AutomicLong(0);
		public bool SpawnUrl { get; set; } = true;
		public DateTime StartTime { get; private set; }
		public DateTime FinishedTime { get; private set; } = DateTime.MinValue;
		public Site Site { get; set; }
		public Action<Page> CustomizePage;
		public bool SaveStatus { get; set; }
		public string Identity { get; }
		public bool ShowConsoleStatus { get; set; } = true;
		public List<IPipeline> Pipelines { get; private set; } = new List<IPipeline>();
		public IDownloader Downloader { get; private set; }
		public bool IsExitWhenComplete { get; set; } = true;
		public Status StatusCode => Stat;
		public IScheduler Scheduler { get; }
		//public IList<ISpiderListener> SpiderListeners { get; set; } = new List<ISpiderListener>();
		public event RequestSuccessed RequestSuccessedEvent;
		public event RequestFailed RequestFailedEvent;
		public event SpiderClosing SpiderClosingEvent;
		public int ThreadAliveCount => ThreadPool.ThreadAlive;
		public Dictionary<string, dynamic> Settings { get; } = new Dictionary<string, dynamic>();

		protected readonly string DataRootDirectory;

		protected IPageProcessor PageProcessor { get; set; }
		protected List<Request> StartRequests { get; set; }
		protected static readonly int WaitInterval = 8;
		protected Status Stat = Status.Init;
		protected CountableThreadPool ThreadPool { get; set; }
		//protected bool DestroyWhenExit { get; set; } = true;

		private int _waitCountLimit = 20;
		private int _waitCount;
		private bool _init;
		private bool _runningExit;
		private static readonly Regex IdentifyRegex = new Regex(@"^[\{\}\d\w\s-/]+$");
		private static bool _printedInfo;

		/// <summary>
		/// Create a spider with pageProcessor.
		/// </summary>
		/// <param name="pageProcessor"></param>
		/// <returns></returns>
		public static Spider Create(IPageProcessor pageProcessor)
		{
			return new Spider(Guid.NewGuid().ToString(), pageProcessor, new QueueDuplicateRemovedScheduler());
		}

		/// <summary>
		/// Create a spider with pageProcessor and scheduler
		/// </summary>
		/// <param name="pageProcessor"></param>
		/// <param name="scheduler"></param>
		/// <returns></returns>
		public static Spider Create(IPageProcessor pageProcessor, IScheduler scheduler)
		{
			return new Spider(Guid.NewGuid().ToString(), pageProcessor, scheduler);
		}

		/// <summary>
		/// Create a spider with indentify, pageProcessor, scheduler.
		/// </summary>
		/// <param name="identify"></param>
		/// <param name="pageProcessor"></param>
		/// <param name="scheduler"></param>
		/// <returns></returns>
		public static Spider Create(string identify, IPageProcessor pageProcessor, IScheduler scheduler)
		{
			return new Spider(identify, pageProcessor, scheduler);
		}

		/// <summary>
		/// Create a spider with pageProcessor.
		/// </summary>
		/// <param name="identify"></param>
		/// <param name="pageProcessor"></param>
		/// <param name="scheduler"></param>
		protected Spider(string identify, IPageProcessor pageProcessor, IScheduler scheduler)
		{
			_waitCount = 0;
			PageProcessor = pageProcessor;
			Site = pageProcessor.Site;
			StartRequests = Site.StartRequests;

			Scheduler = scheduler ?? new QueueDuplicateRemovedScheduler();
			if (string.IsNullOrWhiteSpace(identify))
			{
				Identity = string.IsNullOrEmpty(Site.Domain) ? Guid.NewGuid().ToString() : Site.Domain;
			}
			else
			{
				if (!IdentifyRegex.IsMatch(identify))
				{
					throw new SpiderExceptoin("Task Identify only can contains A-Z a-z 0-9 _ -");
				}
				Identity = identify;
			}

#if !NET_CORE

			DataRootDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\data\\" + Identity;
#else
			DataRootDirectory = Path.Combine(AppContext.BaseDirectory, Path.Combine("data", Identity));
#endif
		}

		/// <summary>
		/// Start with more than one threads
		/// </summary>
		/// <param name="threadNum"></param>
		/// <returns></returns>
		public virtual Spider SetThreadNum(int threadNum)
		{
			if (threadNum <= 0)
			{
				throw new ArgumentException("threadNum should be more than one!");
			}

			CheckIfRunning();

			ThreadNum = threadNum;
			if (Downloader != null)
			{
				Downloader.ThreadNum = threadNum;
			}

			return this;
		}

		/// <summary>
		/// Set wait time when no url is polled.
		/// </summary>
		/// <param name="emptySleepTime"></param>
		public void SetEmptySleepTime(int emptySleepTime)
		{
			if (emptySleepTime >= 10000)
			{
				_waitCountLimit = emptySleepTime / WaitInterval;
			}
			else
			{
				throw new SpiderExceptoin("Sleep time should be large than 10000.");
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
			Downloader.ThreadNum = ThreadNum == 0 ? 1 : ThreadNum;
			return this;
		}

		public void InitComponent()
		{
			if (_init)
			{
#if NET_CORE
				Logger.Info($"Component already init.", true);
#else
				Logger.Info("Component already init.");
#endif

				return;
			}

			Console.CancelKeyPress += ConsoleCancelKeyPress;

			Scheduler.Init(this);

			if (Downloader == null)
			{
				Downloader = new HttpClientDownloader();
			}

			Downloader.ThreadNum = ThreadNum;

			if (Pipelines.Count == 0)
			{
				Pipelines.Add(new FilePipeline());
			}
			if (ThreadPool == null)
			{
				ThreadPool = new CountableThreadPool(ThreadNum);
			}

			if (StartRequests != null)
			{
				if (StartRequests.Count > 0)
				{
#if NET_CORE
				Logger.Info($"Start push Request to queque...", true);
#else
					Logger.Info("Start push Request to queque...");
#endif
					if ((Scheduler is QueueDuplicateRemovedScheduler) || (Scheduler is PriorityScheduler))
					{
						Parallel.ForEach(StartRequests, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, request =>
						{
							Scheduler.Push(request, this);
						});
					}
					else
					{
						QueueDuplicateRemovedScheduler scheduler = new QueueDuplicateRemovedScheduler();
						Parallel.ForEach(StartRequests, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, request =>
						{
							scheduler.Push(request, this);
						});
						Scheduler.Load(scheduler.ToList(this), this);
						ClearStartRequests();
					}

#if NET_CORE
				Logger.Info($"Push Request: {StartRequests.Count} to Scheduler success.", true);
#else
					Logger.Info("Push Request to Scheduler success.");
#endif

				}
				else
				{
#if NET_CORE
				Logger.Info("Push Zero Request to Scheduler.", true);
#else
					Logger.Info("Push Request to Scheduler success.");
#endif

				}
			}

			//Task.Factory.StartNew(() =>
			//{
			//	if (ShowConsoleStatus)
			//	{
			//		IMonitorableScheduler monitor = Scheduler as IMonitorableScheduler;
			//		if (monitor != null)
			//		{
			//			while (true)
			//			{
			//				try
			//				{
			//					if (Stat == Status.Running && !_waitingToExit)
			//					{
			//						Console.WriteLine(
			//							$"Left: {monitor.GetLeftRequestsCount(this)} Total: {monitor.GetTotalRequestsCount(this)} AliveThread: {ThreadPool.ThreadAlive} ThreadNum: {ThreadPool.ThreadNum}");
			//					}
			//				}
			//				catch
			//				{
			//					// ignored
			//				}
			//				Thread.Sleep(2000);
			//			}
			//		}
			//	}
			//});

			_init = true;
		}

		public void Run()
		{
			CheckIfRunning();

			Stat = Status.Running;
			_runningExit = false;

#if !NET_CORE
			// 开启多线程支持
			System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
#endif
			Logger.Info("Spider " + Identity + " InitComponent...");
			InitComponent();

			Logger.Info("Spider " + Identity + " Started!");

			IMonitorableScheduler monitor = (IMonitorableScheduler)Scheduler;

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

				while (Stat == Status.Running)
				{
					Request request = Scheduler.Poll(this);

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
#if !NET_CORE
						Console.WriteLine($"Left: {monitor.GetLeftRequestsCount(this)} Total: {monitor.GetTotalRequestsCount(this)} AliveThread: {ThreadPool.ThreadAlive} ThreadNum: {ThreadPool.ThreadNum}");
#endif
						waitCount = 0;

						try
						{
							ProcessRequest(request);

							Thread.Sleep(Site.SleepTime);

#if DEBUG
							System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
							sw.Reset();
							sw.Start();
#endif

							OnSuccess(request);
#if DEBUG
							sw.Stop();
							Console.WriteLine("OnSuccess:" + (sw.ElapsedMilliseconds).ToString());
#endif
						}
						catch (Exception e)
						{
							OnError(request);
							Logger.Error("Request " + request.Url + " failed.", e);
						}
						finally
						{
#if !NET_CORE
							if (Site.HttpProxyPoolEnable && request.GetExtra(Request.Proxy) != null)
							{
								Site.ReturnHttpProxyToPool((HttpHost)request.GetExtra(Request.Proxy), (int)request.GetExtra(Request.StatusCode));
							}
#endif
							FinishedPageCount.Inc();
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

			foreach (IPipeline pipeline in Pipelines)
			{
				SafeDestroy(pipeline);
			}

			if (Stat == Status.Finished)
			{
				OnClose();

				Logger.Info($"Spider {Identity} Finished.");
			}

			if (Stat == Status.Stopped)
			{
				Logger.Info("Spider " + Identity + " stop success!");
			}

			Log.WaitForExit();

			_runningExit = true;
		}

		public static void PrintInfo()
		{
			if (!_printedInfo)
			{
#if NET_CORE
				Log.WriteLine("=============================================================");
				Log.WriteLine("== DotnetSpider is an open source .Net spider              ==");
				Log.WriteLine("== It's a light, stable, high performce spider             ==");
				Log.WriteLine("== Support multi thread, ajax page, http                   ==");
				Log.WriteLine("== Support save data to file, mysql, mssql, mongodb etc    ==");
				Log.WriteLine("== License: LGPL3.0                                        ==");
				Log.WriteLine("== Version: 0.9.10                                         ==");
				Log.WriteLine("== Author: zlzforever@163.com                              ==");
				Log.WriteLine("=============================================================");
#else
				Console.WriteLine("=============================================================");
				Console.WriteLine("== DotnetSpider is an open source .Net spider              ==");
				Console.WriteLine("== It's a light, stable, high performce spider             ==");
				Console.WriteLine("== Support multi thread, ajax page, http                   ==");
				Console.WriteLine("== Support save data to file, mysql, mssql, mongodb etc    ==");
				Console.WriteLine("== License: LGPL3.0                                        ==");
				Console.WriteLine("== Version: 0.9.10                                         ==");
				Console.WriteLine("== Author: zlzforever@163.com                              ==");
				Console.WriteLine("=============================================================");
#endif
				_printedInfo = true;
			}
		}

		public void RunAsync()
		{
			Task.Factory.StartNew(Run).ContinueWith(t =>
			{
				if (t.Exception != null)
				{
					Logger.Error(t.Exception.Message);
				}
			});
		}

		public void Start()
		{
			RunAsync();
		}

		public void Stop()
		{
			Stat = Status.Stopped;
			Logger.Warn("Trying to stop Spider " + Identity + "...");
		}

		protected void OnClose()
		{
			SpiderClosingEvent?.Invoke();
			foreach (var pipeline in Pipelines)
			{
				SafeDestroy(pipeline);
			}

			try
			{
				var scheduler = Scheduler as IDuplicateRemover;
				scheduler?.ResetDuplicateCheck(this);
			}
			catch
			{
				// ignored
			}


			SafeDestroy(Scheduler);
			SafeDestroy(PageProcessor);
			SafeDestroy(Downloader);
		}

		protected void OnError(Request request)
		{
			lock (this)
			{
				FileInfo file = FilePersistentBase.PrepareFile(Path.Combine(DataRootDirectory, "ErrorRequests.txt"));
				File.AppendAllText(file.FullName, JsonConvert.SerializeObject(request) + Environment.NewLine, Encoding.UTF8);
			}

			RequestFailedEvent?.Invoke(request);
		}

		protected void OnSuccess(Request request)
		{
			RequestSuccessedEvent?.Invoke(request);
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

		protected void ProcessRequest(Request request)
		{
			Page page = null;
#if DEBUG
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
#endif
			while (true)
			{
				try
				{
#if DEBUG
					sw.Reset();
					sw.Start();
#endif
					page = Downloader.Download(request, this);

#if DEBUG
					sw.Stop();
					Console.WriteLine("Download:" + (sw.ElapsedMilliseconds).ToString());
#endif
					if (page.IsSkip)
					{
						return;
					}

					CustomizePage?.Invoke(page);

#if DEBUG
					sw.Reset();
					sw.Start();
#endif
					PageProcessor.Process(page);
#if DEBUG
					sw.Stop();
					Console.WriteLine("Process:" + (sw.ElapsedMilliseconds).ToString());
#endif
					break;
				}
				catch (Exception e)
				{
					if (Site.CycleRetryTimes > 0)
					{
						page = AddToCycleRetry(request, Site);
					}
					Logger.Warn("Download or parse page " + request.Url + " failed:" + e);
					break;
				}
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

			if (page.MissTargetUrls)
			{
				//Logger.Info("Stoper trigger worked on this page.");
			}
			else
			{
				ExtractAndAddRequests(page, SpawnUrl);
			}

#if DEBUG
			sw.Reset();
			sw.Start();
#endif
			if (!page.ResultItems.IsSkip)
			{
				foreach (IPipeline pipeline in Pipelines)
				{
					pipeline.Process(page.ResultItems, this);
				}

#if NET_CORE
				Log.WriteLine($"Request: {request.Url} Sucess.");
#else
				Console.WriteLine($"Request: {request.Url} Sucess.");
#endif
			}
			else
			{
				var message = $"Request {request.Url} 's result count is zero.";
				Logger.Warn(message);
			}
#if DEBUG
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
				throw new SpiderExceptoin("Spider is already running!");
			}
		}

		//protected virtual List<ICollectorPipeline> GetCollectorPipeline(params Type[] types)
		//{
		//	return new List<ICollectorPipeline>() { new ResultItemsCollectorPipeline() };
		//}

		///// <summary>
		///// Download urls synchronizing.
		///// </summary>
		///// <typeparam name="T"></typeparam>
		///// <param name="urls"></param>
		///// <returns></returns>
		//public IList<T> GetAll<T>(params string[] urls)
		//{
		//	DestroyWhenExit = false;
		//	SpawnUrl = false;

		//	foreach (Request request in UrlUtils.ConvertToRequests(urls, 1))
		//	{
		//		AddRequest(request);
		//	}
		//	ICollectorPipeline collectorPipeline = GetCollectorPipeline<T>();
		//	Pipelines.Clear();
		//	Pipelines.Add(collectorPipeline);
		//	Run();
		//	SpawnUrl = true;
		//	DestroyWhenExit = true;

		//	ICollection collection = collectorPipeline.GetCollected();

		//	try
		//	{
		//		return (from object current in collection select (T)current).ToList();
		//	}
		//	catch (Exception)
		//	{
		//		throw new SpiderExceptoin($"Your pipeline didn't extract data to model: {typeof(T).FullName}");
		//	}
		//}

		//[MethodImpl(MethodImplOptions.Synchronized)]
		//public Dictionary<Type, List<dynamic>> GetAll(Type[] types, params string[] urls)
		//{
		//	//DestroyWhenExit = false;
		//	SpawnUrl = false;

		//	foreach (Request request in UrlUtils.ConvertToRequests(urls, 1))
		//	{
		//		AddRequest(request);
		//	}
		//	List<ICollectorPipeline> collectorPipelineList = GetCollectorPipeline(types);
		//	Pipelines.Clear();
		//	Pipelines.AddRange(collectorPipelineList);
		//	Run();
		//	SpawnUrl = true;
		//	//DestroyWhenExit = true;

		//	Dictionary<Type, List<dynamic>> result = new Dictionary<Type, List<dynamic>>();
		//	foreach (var collectorPipeline in collectorPipelineList)
		//	{
		//		ICollection collection = collectorPipeline.GetCollected();

		//		foreach (var entry in collection)
		//		{
		//			var de = (KeyValuePair<Type, List<dynamic>>)entry;

		//			if (result.ContainsKey(de.Key))
		//			{
		//				result[de.Key].AddRange(de.Value);
		//			}
		//			else
		//			{
		//				result.Add(de.Key, new List<dynamic>(de.Value));
		//			}
		//		}
		//	}

		//	return result;
		//}

		private void ClearStartRequests()
		{
			lock (this)
			{
				//Request tmpTequest;
				//while (StartRequests.TryTake(out tmpTequest))
				//{
				//	tmpTequest.Dispose();
				//}
				StartRequests.Clear();
				GC.Collect();
			}
		}

		private void AddStartRequest(Request request)
		{
			Scheduler.Push(request, this);
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
				//double check
				//if (ThreadPool.GetThreadAlive() == 0 && ExitWhenComplete)
				//{
				//	return;
				//}
				Thread.Sleep(WaitInterval);
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
			while (!_runningExit)
			{
				Thread.Sleep(1500);
			}
		}

		public void Dispose()
		{
			OnClose();
		}
	}
}