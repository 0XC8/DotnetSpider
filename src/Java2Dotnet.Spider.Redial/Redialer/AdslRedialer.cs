﻿using System;
using Java2Dotnet.Spider.Redial.Utils;
using System.Diagnostics;

namespace Java2Dotnet.Spider.Redial.Redialer
{
	public class AdslRedialer : BaseAdslRedialer
	{
		public AdslRedialer()
		{
		}

		public AdslRedialer(string interface1, string user, string password) : base(interface1, user, password)
		{
		}

		public override void Redial()
		{
			Console.WriteLine($"Try to redial: {Interface} {User} {Password}");
#if !NET_CORE
			RasDisplay ras = new RasDisplay();
			ras.Disconnect();//断开连接
			ras.Connect(Interface);//重新拨号
#else
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process process = Process.Start("adsl-stop");
				process.WaitForExit();
				process = Process.Start("adsl-start");
				process.WaitForExit();
			}
#endif
		}
	}
}

