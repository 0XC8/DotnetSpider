﻿using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Java2Dotnet.Spider.Extension;
using Java2Dotnet.Spider.Test.Example;
using Java2Dotnet.Spider.Test.Pipeline;
using Java2Dotnet.Spider.JLog;
using Newtonsoft.Json;
using Java2Dotnet.Spider.Core.Downloader;
using System.Threading.Tasks;
using System.Threading;
using Java2Dotnet.Spider.Core;

namespace Java2Dotnet.Spider.Test
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//JdSkuSampleSpider spiderBuilder = new JdSkuSampleSpider();
			//var context = spiderBuilder.GetBuilder().Context;
			//ContextSpider spider = new ContextSpider(context);
			//spider.Run(args);
			CountableThreadPool pool = new CountableThreadPool(1);
			for (int i = 0; i < 100; ++i)
			{
				pool.Push(o =>
				{
					HttpClient client = new HttpClient();
					var str = client.GetStringAsync("http://www.baidu.com").Result;
					Console.WriteLine(o);
				}, i);

			}

			while (true)
			{
				Thread.Sleep(10);
			}
			//TestStatusServer();

			Console.WriteLine("OK");
		}

		private static void TestStatusServer()
		{
			Log.UserId = "86Research-DotnetSpider-log";
			Log.TaskId = "Tmall Gmv Monthly";

			var ErrorPageCount = 10;
			var LeftPageCount = 10;
			var PagePerSecond = 10;
			var StartTime = new DateTime(2016, 4, 19);
			var EndTime = DateTime.Now;
			var Status = "running";
			var SuccessPageCount = 100;
			var ThreadCount = 5;
			var TotalPageCount = 1000;
			var AliveThreadCount = 5;
			var Name = Log.TaskId + DateTime.Now.ToString("yyyy-MM-dd");

			var status = new
			{
				Message = new
				{
					ErrorPageCount,
					LeftPageCount,
					PagePerSecond,
					StartTime,
					EndTime,
					Status,
					SuccessPageCount,
					ThreadCount,
					TotalPageCount,
					AliveThreadCount
				},
				Name,
				Machine = Log.Machine,
				UserId = Log.UserId,
				TaskId = Log.TaskId
			};

			HttpClient client = new HttpClient();

			var task = client.PostAsync("http://localhost:62823/api/status/uploadstatus", new StringContent(JsonConvert.SerializeObject(status), Encoding.UTF8, "application/json"));
			var r = task.Result;
		}

		static void Run(Action a, string type, string name)
		{
			try
			{
				a();
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write($"[{type}] [{name}] ");
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("PASS.");
				Console.WriteLine();
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write($"[{type}] [{name}] ");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("FAILD.");
				Console.WriteLine();
			}
		}
	}
}
