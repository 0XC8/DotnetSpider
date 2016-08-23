﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using Dapper;
using DotnetSpider.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StackExchange.Redis;

namespace DotnetSpider.Portal.Controllers
{
	public class TaskStatusController : Controller
	{
		public IActionResult Dashboad(int? page = 1, int? pageSize = 15, string taskGroup = null, string date = null)
		{
			bool needRedirect = !HttpContext.Request.Query.ContainsKey("page") || !HttpContext.Request.Query.ContainsKey("pageSize") || !HttpContext.Request.Query.ContainsKey("taskGroup") || !HttpContext.Request.Query.ContainsKey("date");
			int no = page ?? 1;
			int size = pageSize ?? 15;
			taskGroup = taskGroup ?? "All";
			date = date ?? "All";
			if (needRedirect)
			{
				return Redirect($"/taskstatus/dashboad/?page={no}&pageSize={size}&taskGroup={taskGroup}&date={date}");
			}
			using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetSection("ConnectionStrings")["MySqlConnectString"]))
			{
				var totalCount = conn.Query<CountResult>(GetSelectTotalCountSql(taskGroup, date)).First();
				var totalPage = totalCount.Count / size + (totalCount.Count % size > 0 ? 1 : 0);
				ViewBag.UpdateUrl = $"/taskstatus/list/?page={no}&pageSize={size}&taskGroup={taskGroup}&date={date}";
				ViewBag.BaseUrl = $"/taskstatus/dashboad/?page={no}&pageSize={size}&";
				ViewBag.TotalPage = (int)totalPage;
				ViewBag.CurrentPage = no;
				ViewBag.TaskGroup = taskGroup;
				ViewBag.Date = date;
				ViewBag.TaskGroups = conn.Query<TaskStatus>("SELECT taskgroup FROM nlog.status GROUP BY taskgroup").ToList();
				return View();
			}
		}

		private string GetSelectTotalCountSql(string taskGroup, string date)
		{
			string taskGroupFilter = "";
			if (!string.IsNullOrEmpty(taskGroup))
			{
				if (taskGroup == "All")
				{
					taskGroupFilter = "";
				}
				else
				{
					taskGroupFilter = $"`taskGroup`='{taskGroup}'";
				}
			}
			string dateFilter = "";
			if (!string.IsNullOrEmpty(date))
			{
				switch (date)
				{
					case "Today":
					{
						dateFilter = $"`logged`>='{DateTime.Now.ToString("yyyy-MM-dd")}'";
						break;
					}
					case "All":
					{
						dateFilter = "";
						break;
					}
					case "LastThreeDays":
					{
						dateFilter = $"`logged`>='{DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd")}'";
						break;
					}
					case "LastSevenDays":
					{
						dateFilter = $"`logged`>='{DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")}'";
						break;
					}
					default:
					{
						dateFilter = "";
						break;
					}
				}
			}
			StringBuilder sql = new StringBuilder("SELECT COUNT(*) as Count FROM nlog.status WHERE 1=1 ");

			if (!string.IsNullOrEmpty(taskGroupFilter))
			{
				sql.Append($" AND {taskGroupFilter}");
			}
			if (!string.IsNullOrEmpty(dateFilter))
			{
				sql.Append($" AND {dateFilter}");
			}

			return sql.ToString();
		}

		public IActionResult List(int page = 1, int pageSize = 15, string taskGroup = null, string date = null)
		{
			string taskGroupFilter = "";
			if (!string.IsNullOrEmpty(taskGroup))
			{
				if (taskGroup == "All")
				{
					taskGroupFilter = "";
				}
				else
				{
					taskGroupFilter = $"`taskGroup`='{taskGroup}'";
				}
			}
			string dateFilter = "";
			if (!string.IsNullOrEmpty(date))
			{
				switch (date)
				{
					case "Today":
						{
							dateFilter = $"`logged`>='{DateTime.Now.ToString("yyyy-MM-dd")}'";
							break;
						}
					case "All":
						{
							dateFilter = "";
							break;
						}
					case "LastThreeDays":
						{
							dateFilter = $"`logged`>='{DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd")}'";
							break;
						}
					case "LastSevenDays":
						{
							dateFilter = $"`logged`>='{DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")}'";
							break;
						}
					default:
						{
							dateFilter = "";
							break;
						}
				}
			}

			StringBuilder sql = new StringBuilder("SELECT * FROM nlog.status WHERE 1=1 ");

			if (!string.IsNullOrEmpty(taskGroupFilter))
			{
				sql.Append($" AND {taskGroupFilter}");
			}
			if (!string.IsNullOrEmpty(dateFilter))
			{
				sql.Append($" AND {dateFilter}");
			}
			sql.Append($" ORDER BY id DESC LIMIT {(page - 1) * pageSize},{pageSize}");

			using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetSection("ConnectionStrings")["MySqlConnectString"]))
			{
				
				List<TaskStatus> list =
					conn.Query<TaskStatus>(sql.ToString())
						.ToList();
				return View(list);
			}
		}

		public string Stop(string identity)
		{
			var host = Startup.Configuration.GetSection("Redis")["Host"];
			var password = Startup.Configuration.GetSection("Redis")["Password"];
			int port;
			if (!int.TryParse(Startup.Configuration.GetSection("Redis")["Port"], out port) || port <= 0)
			{
				return "REDIS Port is incorrect.";
			}
			if (!string.IsNullOrEmpty(host) && !string.IsNullOrWhiteSpace(host))
			{
				var confiruation = new ConfigurationOptions()
				{
					ServiceName = "DotnetSpider",
					Password = password,
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
					confiruation.EndPoints.Add(new IPEndPoint(address, port));
				}
				else
				{
					confiruation.EndPoints.Add(new DnsEndPoint(host, port));
				}
#else
				confiruation.EndPoints.Add(new DnsEndPoint(host, port));
#endif
				var redis = ConnectionMultiplexer.Connect(confiruation);
				redis.GetSubscriber().Publish($"{identity}", "EXIT");
				return "OK";
			}

			return "REDIS Host is incorrect.";
		}

	}
}
