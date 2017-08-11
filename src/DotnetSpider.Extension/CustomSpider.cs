﻿using DotnetSpider.Core;
using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Extension.Infrastructure;
using DotnetSpider.Extension.Monitor;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetSpider.Extension
{
	public abstract class CustomSpider : IRunable, INamed, IIdentity, ITask
	{
		protected readonly static ILogger Logger = LogCenter.GetLogger();

		private bool _exited;

		private Task _statusReporter;

		public string Name { get; set; }

		public string ConnectString { get; set; }

		public string Identity { get; set; }

		public string TaskId { get; set; }

		protected CustomSpider(string name)
		{
			Name = name;
			if (string.IsNullOrEmpty(ConnectString))
			{
				ConnectString = Config.ConnectString;
			}
		}

		protected abstract void ImplementAction(params string[] arguments);

		protected event Action OnExited;

		public void Run(params string[] arguments)
		{
			if (string.IsNullOrEmpty(Identity) || Identity.Length > 120)
			{
				throw new ArgumentException("Length of Identity should between 1 and 120.");
			}

			if (string.IsNullOrEmpty(ConnectString))
			{
				ConnectString = Config.ConnectString;
			}

			if (string.IsNullOrEmpty(ConnectString))
			{
				throw new ArgumentException("ConnectString is missing.");
			}

			if (!string.IsNullOrEmpty(ConnectString))
			{
				NLogUtil.PrepareDatabase(ConnectString);
				DbMonitor.InitStatusDatabase(ConnectString);

				if (!string.IsNullOrEmpty(TaskId))
				{
					InsertRunningState();
				}

				using (IDbConnection conn = new MySqlConnection(ConnectString))
				{
					conn.Open();
					var command = conn.CreateCommand();
					command.CommandType = CommandType.Text;

					command.CommandText = $"insert ignore into dotnetspider.status (`identity`, `status`,`thread`, `left`, `success`, `error`, `total`, `avgdownloadspeed`, `avgprocessorspeed`, `avgpipelinespeed`, `logged`) values('{Identity}', 'Init',-1, -1, -1, -1, -1, -1, -1, -1, '{DateTime.Now}');";
					command.ExecuteNonQuery();

					Logger.MyLog(Identity, $"开始任务: {Name}", LogLevel.Info);
				}

				_statusReporter = Task.Factory.StartNew(() =>
				{
					using (IDbConnection conn = new MySqlConnection(ConnectString))
					{
						conn.Open();
						var command = conn.CreateCommand();
						command.CommandType = CommandType.Text;

						while (!_exited)
						{
							command.CommandText = $"update dotnetspider.status set `logged`='{DateTime.Now}' WHERE identity='{Identity}';";
							command.ExecuteNonQuery();
							Thread.Sleep(5000);
						}
					}
				});
			}

			try
			{
				ImplementAction(arguments);

				_exited = true;
				_statusReporter.Wait();

				Logger.MyLog(Identity, $"结束任务: {Name}", LogLevel.Info);

				if (!string.IsNullOrEmpty(ConnectString))
				{
					using (IDbConnection conn = new MySqlConnection(ConnectString))
					{
						conn.Open();
						var command = conn.CreateCommand();
						command.CommandType = CommandType.Text;
						command.CommandText = $"update dotnetspider.status set `status`='Finished',`logged`='{DateTime.Now}' WHERE identity='{Identity}';";
						command.ExecuteNonQuery();
					}
				}
				if (OnExited != null)
				{
					Verifier.ProcessVerifidation(Identity, OnExited);
				}
			}
			catch (Exception e)
			{
				Logger.MyLog(Identity, $"退出任务: {Name}", LogLevel.Info);

				if (!string.IsNullOrEmpty(ConnectString))
				{
					using (IDbConnection conn = new MySqlConnection(ConnectString))
					{
						conn.Open();

						var command = conn.CreateCommand();
						command.CommandType = CommandType.Text;
						command.CommandText = $"update dotnetspider.status set `status`='Exited', `logged`='{DateTime.Now}' WHERE identity='{Identity}';";
						command.ExecuteNonQuery();
					}
				}
			}
			finally
			{
				if (!string.IsNullOrEmpty(ConnectString) && !string.IsNullOrEmpty(TaskId))
				{
					RemoveRunningState();
				}
			}
		}

		public Task RunAsync(params string[] arguments)
		{
			return Task.Factory.StartNew(() =>
			{
				Run(arguments);
			});
		}

		private void InsertRunningState()
		{
			using (IDbConnection conn = new MySqlConnection(ConnectString))
			{
				conn.Open();
				var command = conn.CreateCommand();
				command.CommandType = CommandType.Text;

				command.CommandText = "CREATE SCHEMA IF NOT EXISTS `dotnetspider` DEFAULT CHARACTER SET utf8mb4;";
				command.ExecuteNonQuery();

				command.CommandText = "CREATE TABLE IF NOT EXISTS `dotnetspider`.`task_running` (`id` bigint(20) NOT NULL AUTO_INCREMENT, `taskId` varchar(120) NOT NULL, `name` varchar(200) NULL, `identity` varchar(120), `cdate` timestamp NOT NULL, PRIMARY KEY (id), UNIQUE KEY `taskId_unique` (`taskId`)) ENGINE=InnoDB AUTO_INCREMENT=1  DEFAULT CHARSET=utf8";
				command.ExecuteNonQuery();

				command.CommandText = $"INSERT IGNORE INTO `dotnetspider`.`task_running` (`taskId`,`name`,`identity`,`cdate`) values ('{TaskId}','{Name}','{Identity}','{DateTime.Now}');";
				command.ExecuteNonQuery();
			}
		}

		private void RemoveRunningState()
		{
			using (IDbConnection conn = new MySqlConnection(ConnectString))
			{
				conn.Open();
				var command = conn.CreateCommand();
				command.CommandType = CommandType.Text;

				command.CommandText = $"DELETE FROM `dotnetspider`.`task_running` WHERE `identity`='{Identity}';";
				command.ExecuteNonQuery();
			}
		}
	}
}
