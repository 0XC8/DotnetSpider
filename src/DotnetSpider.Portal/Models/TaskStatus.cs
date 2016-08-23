﻿using System;
using System.Collections.Generic;

namespace DotnetSpider.Portal.Models
{
	public class TaskStatus
	{
		[Flags]
		public enum StatusCode
		{
			Init = 0,
			Running = 1,
			Exited = 2,
			Stopped = 3,
			Finished = 4
		}

		public string TaskGroup { get; set; }
		public string UserId { get; set; }
		public string Identity { get; set; }
		public StatusCode Status { get; set; }

		public string StatusClass
		{
			get
			{
				switch (Status)
				{
					case StatusCode.Running:
						{
							return "badge bg-green";
						}
					case StatusCode.Exited:
						{
							return "badge bg-red";
						}
					case StatusCode.Finished:
						{
							return "badge bg-light-blue";
						}
					case StatusCode.Stopped:
						{
							return "badge bg-yellow";
						}
					case StatusCode.Init:
						{
							return "badge bg-gray";
						}
				}
				return "";
			}
		}
		public string Message { get; set; }
		public DateTime Logged { get; set; }
		public Int64 Id { get; set; }

		public static List<TaskStatus> Create(int num)
		{
			var list = new List<TaskStatus>();
			for (int i = 0; i < num; ++i)
			{
				list.Add(new TaskStatus
				{
					Id = i,
					TaskGroup = "YY",
					UserId = "86Research",
					Identity = "YY Channel " + i,
					Status = StatusCode.Finished,
					Message = "Left: 0 Total: 100 Success: 100 Error: 0",
					Logged = DateTime.Now
				});
			}
			return list;
		}
	}
}
