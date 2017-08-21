﻿using DotnetSpider.Core.Infrastructure;
using NLog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DotnetSpider.Core.Proxy
{
	public class ProxyUtil
	{
		protected static readonly ILogger Logger = LogCenter.GetLogger();

		public static bool ValidateProxy(string ip, int port)
		{
			bool isReachable = false;

			try
			{
				TcpClient tcp = new TcpClient();
				IPAddress ipAddr = IPAddress.Parse(ip);
				tcp.ReceiveTimeout = 5000;
				Stopwatch watch = new Stopwatch();
				watch.Start();
				tcp.ConnectAsync(ipAddr, port).Wait();
				watch.Stop();
				Logger.MyLog($"Detect one usefull proxy: {ip}:{port}, cost {watch.ElapsedMilliseconds}ms.", LogLevel.Debug);
				isReachable = true;
			}
			catch (Exception e)
			{
				Logger.MyLog($"FAILRE - CAN not connect! Proxy: {ip}:{port}.", LogLevel.Error, e);
			}

			return isReachable;
		}
	}
}