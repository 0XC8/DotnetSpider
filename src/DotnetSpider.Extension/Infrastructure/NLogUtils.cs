﻿//using MySql.Data.MySqlClient;
//using System.IO;
//using System.Linq;
//using Dapper;
//using System.Xml.Linq;
//using DotnetSpider.Core.Infrastructure.Database;

//namespace DotnetSpider.Extension.Infrastructure
//{
//	public class NLogUtils
//	{
//		public static void PrepareDatabase(string connectString)
//		{
//			var fileInfo = new FileInfo(Path.Combine(Core.Environment.BaseDirectory, "nlog.config"));
//			if (fileInfo.Exists)
//			{
//				XElement root = XElement.Parse(File.ReadAllText(fileInfo.FullName));

//				var xElement = root.Element("{http://www.nlog-project.org/schemas/NLog.xsd}targets");
//				if (xElement != null)
//				{
//					var targets = xElement.Elements("{http://www.nlog-project.org/schemas/NLog.xsd}target").ToList();
//					var dblog = targets.First(e =>
//					{
//						var xAttribute = e.Attribute("name");
//						return xAttribute != null && xAttribute.Value == "dblog";
//					});
//					var connectionStringName = dblog.Attribute("connectionStringName").Value;
//					var commands = dblog.Elements("{http://www.nlog-project.org/schemas/NLog.xsd}install-command");
//					using (var conn = Core.Environment.GetConnectStringSettings(connectionStringName).GetDbConnection())
//					{
//						foreach (var command in commands)
//						{
//							var xAttribute = command.Attribute("text");
//							if (xAttribute != null)
//							{
//								var sql = xAttribute.Value;
//								conn.Execute(sql);
//							}
//						}
//					}
//				}
//			}
//		}
//	}
//}
