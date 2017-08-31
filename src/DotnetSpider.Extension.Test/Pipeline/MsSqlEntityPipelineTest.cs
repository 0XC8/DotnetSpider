﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Dapper;
using DotnetSpider.Core;
using DotnetSpider.Core.Selector;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Model.Attribute;
using DotnetSpider.Extension.ORM;
using DotnetSpider.Extension.Pipeline;
using Xunit;
using System.Runtime.InteropServices;

namespace DotnetSpider.Extension.Test.Pipeline
{
	/// <summary>
	/// CREATE database  test firstly
	/// </summary>
	public class MsSqlEntityPipelineTest
	{
		private const string ConnectString = "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True";

		private void ClearDb()
		{
			using (SqlConnection conn = new SqlConnection(ConnectString))
			{
				var tableName = $"sku_{DateTime.Now.ToString("yyyy_MM_dd")}";
				var tableName2 = $"sku2_{DateTime.Now.ToString("yyyy_MM_dd")}";
				conn.Execute("if not exists(select * from sys.databases where name = 'test') create database test;");
				conn.Execute($"USE test; IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP table {tableName};");
				conn.Execute($"USE test; IF OBJECT_ID('{tableName2}', 'U') IS NOT NULL DROP table {tableName2};");
			}
		}

		[Fact]
		public void Update()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}

			ClearDb();

			using (SqlConnection conn = new SqlConnection(ConnectString))
			{
				ISpider spider = new DefaultSpider("test", new Site());

				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
				var metadata = EntitySpider.GenerateEntityDefine(typeof(ProductInsert).GetTypeInfo());
				insertPipeline.AddEntity(metadata);
				insertPipeline.InitPipeline(spider);

				DataObject data1 = new DataObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				DataObject data2 = new DataObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
				insertPipeline.Process(metadata.Name, new List<DataObject> { data1, data2 });

				SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(ConnectString);
				var metadat2 = EntitySpider.GenerateEntityDefine(typeof(ProductUpdate).GetTypeInfo());
				updatePipeline.AddEntity(metadat2);
				updatePipeline.InitPipeline(spider);

				DataObject data3 = new DataObject { { "Sku", "110" }, { "Category", "4C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				updatePipeline.Process(metadat2.Name, new List<DataObject> { data3 });

				var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
				Assert.Equal(2, list.Count);
				Assert.Equal("110", list[0].Sku);
				Assert.Equal("4C", list[0].Category);
			}

			ClearDb();
		}

		[Fact]
		public void UpdateWhenUnionPrimary()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
			ClearDb();

			using (SqlConnection conn = new SqlConnection(ConnectString))
			{
				ISpider spider = new DefaultSpider("test", new Site());

				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
				var metadata = EntitySpider.GenerateEntityDefine(typeof(Product2).GetTypeInfo());
				insertPipeline.AddEntity(metadata);
				insertPipeline.InitPipeline(spider);

				var data1 = new DataObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				var data2 = new DataObject { { "Sku", "111" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
				insertPipeline.Process(metadata.Name, new List<DataObject> { data1, data2 });

				SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(ConnectString);
				var metadata2 = EntitySpider.GenerateEntityDefine(typeof(Product2Update).GetTypeInfo());
				updatePipeline.AddEntity(metadata2);
				updatePipeline.InitPipeline(spider);

				var data3 = new DataObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "AAAA" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				updatePipeline.Process(metadata2.Name, new List<DataObject> { data3 });

				var list = conn.Query<Product2>($"use test;select * from sku2_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
				Assert.Equal(2, list.Count);
				Assert.Equal("110", list[0].Sku);
				Assert.Equal("AAAA", list[0].Category);
			}

			ClearDb();
		}

		[Fact]
		public void UpdateCheckIfSameBeforeUpdate()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
			ClearDb();

			using (SqlConnection conn = new SqlConnection(ConnectString))
			{
				ISpider spider = new DefaultSpider("test", new Site());

				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
				var metadata = EntitySpider.GenerateEntityDefine(typeof(ProductInsert).GetTypeInfo());
				insertPipeline.AddEntity(metadata);
				insertPipeline.InitPipeline(spider);

				var data1 = new DataObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				var data2 = new DataObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
				insertPipeline.Process(metadata.Name, new List<DataObject> { data1, data2 });

				SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(ConnectString, true);
				var metadata2 = EntitySpider.GenerateEntityDefine(typeof(ProductUpdate).GetTypeInfo());
				updatePipeline.AddEntity(metadata2);
				updatePipeline.InitPipeline(spider);

				var data3 = new DataObject { { "Sku", "110" }, { "Category", "4C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				updatePipeline.Process(metadata2.Name, new List<DataObject> { data3 });

				var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
				Assert.Equal(2, list.Count);
				Assert.Equal("110", list[0].Sku);
				Assert.Equal("4C", list[0].Category);
			}

			ClearDb();
		}

		[Fact]
		public void UpdateWhenUnionPrimaryCheckIfSameBeforeUpdate()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
			ClearDb();

			using (SqlConnection conn = new SqlConnection(ConnectString))
			{
				ISpider spider = new DefaultSpider("test", new Site());

				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
				var metadata = EntitySpider.GenerateEntityDefine(typeof(Product2).GetTypeInfo());
				insertPipeline.AddEntity(metadata);
				insertPipeline.InitPipeline(spider);

				var data1 = new DataObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				var data2 = new DataObject { { "Sku", "111" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
				insertPipeline.Process(metadata.Name, new List<DataObject> { data1, data2 });

				SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(ConnectString, true);
				var metadata2 = EntitySpider.GenerateEntityDefine(typeof(Product2Update).GetTypeInfo());
				updatePipeline.AddEntity(EntitySpider.GenerateEntityDefine(typeof(Product2Update).GetTypeInfo()));
				updatePipeline.InitPipeline(spider);

				var data3 = new DataObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "AAAA" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				updatePipeline.Process(metadata2.Name, new List<DataObject> { data3 });

				var list = conn.Query<Product2>($"use test;select * from sku2_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
				Assert.Equal(2, list.Count);
				Assert.Equal("110", list[0].Sku);
				Assert.Equal("AAAA", list[0].Category);
			}

			ClearDb();
		}

		[Fact]
		public void Insert()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
			ClearDb();

			using (SqlConnection conn = new SqlConnection(ConnectString))
			{
				ISpider spider = new DefaultSpider("test", new Site());

				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
				var metadata = EntitySpider.GenerateEntityDefine(typeof(ProductInsert).GetTypeInfo());
				insertPipeline.AddEntity(EntitySpider.GenerateEntityDefine(typeof(ProductInsert).GetTypeInfo()));
				insertPipeline.InitPipeline(spider);

				// Common data
				var data1 = new DataObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
				var data2 = new DataObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
				// Value is null
				var data3 = new DataObject { { "Sku", "112" }, { "Category", null }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
				insertPipeline.Process(metadata.Name, new List<DataObject> { data1, data2, data3 });

				var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
				Assert.Equal(3, list.Count);
				Assert.Equal("110", list[0].Sku);
				Assert.Equal("111", list[1].Sku);
				Assert.Null(list[2].Category);
			}

			ClearDb();
		}

		[Fact]
		public void DefineUpdateEntity()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}
			SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
			try
			{
				insertPipeline.AddEntity(EntitySpider.GenerateEntityDefine(typeof(UpdateEntity1).GetTypeInfo()));
				throw new SpiderException("TEST FAILED.");
			}
			catch (SpiderException e)
			{
				Assert.Equal("Columns set as primary is not a property of your entity.", e.Message);
			}

			try
			{
				insertPipeline.AddEntity(EntitySpider.GenerateEntityDefine(typeof(UpdateEntity2).GetTypeInfo()));
				throw new SpiderException("TEST FAILED.");
			}
			catch (SpiderException e)
			{
				Assert.Equal("Columns set as update is not a property of your entity.", e.Message);
			}

			try
			{
				insertPipeline.AddEntity(EntitySpider.GenerateEntityDefine(typeof(UpdateEntity3).GetTypeInfo()));
				throw new SpiderException("TEST FAILED.");
			}
			catch (SpiderException e)
			{
				Assert.Equal("There is no column need update.", e.Message);
			}
			var metadata = EntitySpider.GenerateEntityDefine(typeof(UpdateEntity4).GetTypeInfo());
			insertPipeline.AddEntity(EntitySpider.GenerateEntityDefine(typeof(UpdateEntity4).GetTypeInfo()));
			Assert.Single(insertPipeline.GetUpdateColumns(metadata.Name));
			Assert.Equal("Value", insertPipeline.GetUpdateColumns(metadata.Name).First());

			SqlServerEntityPipeline insertPipeline2 = new SqlServerEntityPipeline(ConnectString);
			var metadata2 = EntitySpider.GenerateEntityDefine(typeof(UpdateEntity5).GetTypeInfo());
			insertPipeline2.AddEntity(metadata2);
			Assert.Single(insertPipeline2.GetUpdateColumns(metadata2.Name));
			Assert.Equal("Value", insertPipeline2.GetUpdateColumns(metadata2.Name).First());
		}

		//#region Use App.config

		//[Fact]
		//public void UpdateUseAppConfig()
		//{
		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
		//	ClearDb();

		//	using (SqlConnection conn = new SqlConnection(ConnectString))
		//	{
		//		ISpider spider = new DefaultSpider("test", new Site());

		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo());
		//		insertPipeline.AddEntity(metadata);
		//		insertPipeline.InitPipeline(spider);

		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline();
		//		var metadat2 = EntitySpider.GenerateEntityMetaData(typeof(ProductUpdate).GetTypeInfo());
		//		updatePipeline.AddEntity(metadat2);
		//		updatePipeline.InitPipeline(spider);

		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category", "4C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		updatePipeline.Process(metadat2.Name, new List<JObject> { data3 });

		//		var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
		//		Assert.Equal(2, list.Count);
		//		Assert.Equal("110", list[0].Sku);
		//		Assert.Equal("4C", list[0].Category);
		//	}

		//	ClearDb();
		//}

		//[Fact]
		//public void UpdateWhenUnionPrimaryUseAppConfig()
		//{
		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
		//	ClearDb();

		//	using (SqlConnection conn = new SqlConnection(ConnectString))
		//	{
		//		ISpider spider = new DefaultSpider("test", new Site());

		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(Product2).GetTypeInfo());
		//		insertPipeline.AddEntity(metadata);
		//		insertPipeline.InitPipeline(spider);

		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline();
		//		var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(Product2Update).GetTypeInfo());
		//		updatePipeline.AddEntity(metadata2);
		//		updatePipeline.InitPipeline(spider);

		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "AAAA" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		updatePipeline.Process(metadata2.Name, new List<JObject> { data3 });

		//		var list = conn.Query<Product2>($"use test;select * from sku2_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
		//		Assert.Equal(2, list.Count);
		//		Assert.Equal("110", list[0].Sku);
		//		Assert.Equal("AAAA", list[0].Category);
		//	}

		//	ClearDb();
		//}

		//[Fact]
		//public void UpdateCheckIfSameBeforeUpdateUseAppConfig()
		//{
		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");

		//	ClearDb();

		//	using (SqlConnection conn = new SqlConnection(ConnectString))
		//	{
		//		ISpider spider = new DefaultSpider("test", new Site());

		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo());
		//		insertPipeline.AddEntity(metadata);
		//		insertPipeline.InitPipeline(spider);

		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(null, true);
		//		var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(ProductUpdate).GetTypeInfo());
		//		updatePipeline.AddEntity(metadata2);
		//		updatePipeline.InitPipeline(spider);

		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category", "4C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		updatePipeline.Process(metadata2.Name, new List<JObject> { data3 });

		//		var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
		//		Assert.Equal(2, list.Count);
		//		Assert.Equal("110", list[0].Sku);
		//		Assert.Equal("4C", list[0].Category);
		//	}

		//	ClearDb();
		//}

		//[Fact]
		//public void UpdateWhenUnionPrimaryCheckIfSameBeforeUpdateUseAppConfig()
		//{
		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
		//	ClearDb();

		//	using (SqlConnection conn = new SqlConnection(ConnectString))
		//	{
		//		ISpider spider = new DefaultSpider("test", new Site());

		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(Product2).GetTypeInfo());
		//		insertPipeline.AddEntity(metadata);
		//		insertPipeline.InitPipeline(spider);

		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(null, true);
		//		var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(Product2Update).GetTypeInfo());
		//		updatePipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(Product2Update).GetTypeInfo()));
		//		updatePipeline.InitPipeline(spider);

		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "AAAA" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		updatePipeline.Process(metadata2.Name, new List<JObject> { data3 });

		//		var list = conn.Query<Product2>($"use test;select * from sku2_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
		//		Assert.Equal(2, list.Count);
		//		Assert.Equal("110", list[0].Sku);
		//		Assert.Equal("AAAA", list[0].Category);
		//	}

		//	ClearDb();
		//}

		//[Fact]
		//public void InsertUseAppConfig()
		//{
		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");

		//	ClearDb();

		//	using (SqlConnection conn = new SqlConnection(ConnectString))
		//	{
		//		ISpider spider = new DefaultSpider("test", new Site());

		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo());
		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo()));
		//		insertPipeline.InitPipeline(spider);

		//		// Common data
		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
		//		// Value is null
		//		JObject data3 = new JObject { { "Sku", "112" }, { "Category", null }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2, data3 });

		//		var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
		//		Assert.Equal(3, list.Count);
		//		Assert.Equal("110", list[0].Sku);
		//		Assert.Equal("111", list[1].Sku);
		//		Assert.Equal(null, list[2].Category);
		//	}

		//	ClearDb();
		//}

		//[Fact]
		//public void DefineUpdateEntityUseAppConfig()
		//{
		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
		//	SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
		//	try
		//	{
		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity1).GetTypeInfo()));
		//		throw new SpiderException("TEST FAILED.");
		//	}
		//	catch (SpiderException e)
		//	{
		//		Assert.Equal("Columns set as Primary is not a property of your entity.", e.Message);
		//	}

		//	try
		//	{
		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity2).GetTypeInfo()));
		//		throw new SpiderException("TEST FAILED.");
		//	}
		//	catch (SpiderException e)
		//	{
		//		Assert.Equal("Columns set as update is not a property of your entity.", e.Message);
		//	}

		//	try
		//	{
		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity3).GetTypeInfo()));
		//		throw new SpiderException("TEST FAILED.");
		//	}
		//	catch (SpiderException e)
		//	{
		//		Assert.Equal("There is no column need update.", e.Message);
		//	}
		//	var metadata = EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity4).GetTypeInfo());
		//	insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity4).GetTypeInfo()));
		//	Assert.Equal(1, insertPipeline.GetUpdateColumns(metadata.Name).Length);
		//	Assert.Equal("Value", insertPipeline.GetUpdateColumns(metadata.Name).First());

		//	SqlServerEntityPipeline insertPipeline2 = new SqlServerEntityPipeline(ConnectString);
		//	var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity5).GetTypeInfo());
		//	insertPipeline2.AddEntity(metadata2);
		//	Assert.Equal(1, insertPipeline2.GetUpdateColumns(metadata2.Name).Length);
		//	Assert.Equal("Value", insertPipeline2.GetUpdateColumns(metadata2.Name).First());
		//}

		//#endregion

		[Table("test", "sku", TableSuffix.Today, Primary = "Sku", Indexs = new[] { "Category" }, Uniques = new[] { "Category,Sku" })]
		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
		public class ProductInsert : SpiderEntity
		{
			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
			public string Category { get; set; }

			[PropertyDefine(Expression = "./div[1]/a/@href")]
			public string Url { get; set; }

			[PropertyDefine(Expression = "./div[1]/a", Length = 100)]
			public string Sku { get; set; }
		}

		[Table("test", "sku", TableSuffix.Today, Primary = "Sku", UpdateColumns = new[] { "Category" })]
		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
		public class ProductUpdate : SpiderEntity
		{
			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
			public string Category { get; set; }

			[PropertyDefine(Expression = "./div[1]/a/@href")]
			public string Url { get; set; }

			[PropertyDefine(Expression = "./div[1]/a", Length = 100)]
			public string Sku { get; set; }
		}


		[Table("test", "sku2", TableSuffix.Today, Primary = "Sku", Indexs = new[] { "Sku,Category1" })]
		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
		public class Product2 : SpiderEntity
		{
			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
			public string Category1 { get; set; }

			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment)]
			public string Category { get; set; }

			[PropertyDefine(Expression = "./div[1]/a/@href")]
			public string Url { get; set; }

			[PropertyDefine(Expression = "./div[1]/a", Length = 100)]
			public string Sku { get; set; }
		}


		[Table("test", "sku2", TableSuffix.Today, Primary = "Sku", UpdateColumns = new[] { "Category" })]
		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
		public class Product2Update : SpiderEntity
		{
			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
			public string Category1 { get; set; }

			[PropertyDefine(Expression = "name", Type = SelectorType.Enviroment)]
			public string Category { get; set; }

			[PropertyDefine(Expression = "./div[1]/a/@href")]
			public string Url { get; set; }

			[PropertyDefine(Expression = "./div[1]/a", Length = 100)]
			public string Sku { get; set; }
		}

		[Table("test", "sku2", Primary = "Sku", UpdateColumns = new[] { "category" })]
		public class UpdateEntity1 : SpiderEntity
		{
			[PropertyDefine(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
			public string Key { get; set; }

			[PropertyDefine(Expression = "value", Type = SelectorType.Enviroment)]
			public string Value { get; set; }

		}

		[Table("test", "sku2", Primary = "Key", UpdateColumns = new[] { "calue" })]
		public class UpdateEntity2 : SpiderEntity
		{
			[PropertyDefine(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
			public string Key { get; set; }

			[PropertyDefine(Expression = "value", Type = SelectorType.Enviroment, Length = 100)]
			public string Value { get; set; }
		}

		[Table("test", "sku2", Primary = "Key", UpdateColumns = new[] { "Key" })]
		public class UpdateEntity3 : SpiderEntity
		{
			[PropertyDefine(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
			public string Key { get; set; }

			[PropertyDefine(Expression = "value", Type = SelectorType.Enviroment)]
			public string Value { get; set; }
		}

		[Table("test", "sku2", Primary = "Key", UpdateColumns = new[] { "Value" })]
		public class UpdateEntity4 : SpiderEntity
		{
			[PropertyDefine(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
			public string Key { get; set; }

			[PropertyDefine(Expression = "value", Type = SelectorType.Enviroment)]
			public string Value { get; set; }
		}

		[Table("test", "sku2", Primary = "Key", UpdateColumns = new[] { "Value", "Key" })]
		public class UpdateEntity5 : SpiderEntity
		{
			[PropertyDefine(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
			public string Key { get; set; }

			[PropertyDefine(Expression = "value", Type = SelectorType.Enviroment)]
			public string Value { get; set; }
		}
	}
}
