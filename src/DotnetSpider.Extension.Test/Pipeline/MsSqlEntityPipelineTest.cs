﻿//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using DotnetSpider.Core;
//using DotnetSpider.Core.Selector;
//using DotnetSpider.Extension.Model;
//using DotnetSpider.Extension.Model.Attribute;
//using DotnetSpider.Extension.Pipeline;
//using Xunit;
//using Dapper;
//#if NETSTANDARD
//using System.Runtime.InteropServices;
//#endif

//namespace DotnetSpider.Extension.Test.Pipeline
//{
//	/// <summary>
//	/// CREATE database  test firstly
//	/// </summary>
//	public class MsSqlEntityPipelineTest
//	{
//		private const string ConnectString = "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True";

//		private void ClearDb()
//		{
//			using (SqlConnection conn = new SqlConnection(ConnectString))
//			{
//				var tableName = $"sku_{DateTime.Now.ToString("yyyy_MM_dd")}";
//				var tableName2 = $"sku2_{DateTime.Now.ToString("yyyy_MM_dd")}";
//				conn.Execute("if not exists(select * from sys.databases where name = 'test') create database test;");
//				conn.Execute($"USE test; IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP table {tableName};");
//				conn.Execute($"USE test; IF OBJECT_ID('{tableName2}', 'U') IS NOT NULL DROP table {tableName2};");
//			}
//		}

//		[Fact]
//		public void Update()
//		{
//#if NETSTANDARD
//			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//			{
//				return;
//			}
//#endif

//			ClearDb();

//			using (SqlConnection conn = new SqlConnection(ConnectString))
//			{
//				ISpider spider = new DefaultSpider("test", new Site());

//				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
//				var metadata = new ModelDefine<ProductInsert>();
		 
//				var data1 = new ProductInsert { Sku = "110", Category = "3C", Url = "http://jd.com/110" };
//				var data2 = new ProductInsert { Sku = "111", Category = "3C", Url = "http://jd.com/111" };
//				insertPipeline.Process(metadata, new List<dynamic> { data1, data2 }, spider);

//				SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(ConnectString);
//				var metadat2 = new ModelDefine<ProductUpdate>();
			 
//				var data3 = conn.Query<ProductUpdate>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")} where Sku=110").First();
//				data3.Category = "4C";

//				updatePipeline.Process(metadat2, new List<dynamic> { data3 }, spider);

//				var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//				Assert.Equal(2, list.Count);
//				Assert.Equal("110", list[0].Sku);
//				Assert.Equal("4C", list[0].Category);
//			}

//			ClearDb();
//		}

//		[Fact]
//		public void Insert()
//		{
//#if NETSTANDARD
//			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//			{
//				return;
//			}
//#endif
//			ClearDb();

//			using (SqlConnection conn = new SqlConnection(ConnectString))
//			{
//				ISpider spider = new DefaultSpider("test", new Site());

//				SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
//				var metadata = new ModelDefine<ProductInsert>();

//				// Common data
//				var data1 = new ProductInsert { Sku = "110", Category = "3C", Url = "http://jd.com/110" };
//				var data2 = new ProductInsert { Sku = "111", Category = "3C", Url = "http://jd.com/111" };
//				var data3 = new ProductInsert { Sku = "112", Category = null, Url = "http://jd.com/111" };
//				// Value is null
//				insertPipeline.Process(metadata, new List<dynamic> { data1, data2, data3 }, spider);

//				var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//				Assert.Equal(3, list.Count);
//				Assert.Equal("110", list[0].Sku);
//				Assert.Equal("111", list[1].Sku);
//				Assert.Null(list[2].Category);
//			}

//			ClearDb();
//		}

//		[Fact]
//		public void DefineUpdateEntity()
//		{
//#if NETSTANDARD
//			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//			{
//				return;
//			}
//#endif
//			SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline(ConnectString);
//			try
//			{
//				new ModelDefine<UpdateEntity1>();
//				throw new SpiderException("TEST FAILED.");
//			}
//			catch (SpiderException e)
//			{
//				Assert.Equal("Columns set as unique are not a property of your entity", e.Message);
//			}

//			try
//			{
//				new ModelDefine<UpdateEntity2>();
//				throw new SpiderException("TEST FAILED.");
//			}
//			catch (SpiderException e)
//			{
//				Assert.Equal("Columns set to update are not a property of your entity", e.Message);
//			}

//			//try
//			//{
//			//	new ModelDefine<UpdateEntity3>();
//			//	throw new SpiderException("TEST FAILED.");
//			//}
//			//catch (SpiderException e)
//			//{
//			//	Assert.Equal("There is no column need update", e.Message);
//			//}
//			var metadata = new ModelDefine<UpdateEntity4>();

//			Assert.Single(metadata.TableInfo.UpdateColumns);
//			Assert.Equal("Value", metadata.TableInfo.UpdateColumns.First());

//			SqlServerEntityPipeline insertPipeline2 = new SqlServerEntityPipeline(ConnectString);
//			var metadata2 = new ModelDefine<UpdateEntity5>();

//			var columns = metadata2.Fields.ToArray();
//			Assert.Equal(2, columns.Count());
//			Assert.Equal("Value", columns[0].Name);
//			Assert.Equal("Key", columns[1].Name);
//		}

//		//#region Use App.config

//		//[Fact]
//		//public void UpdateUseAppConfig()
//		//{
//		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
//		//	ClearDb();

//		//	using (SqlConnection conn = new SqlConnection(ConnectString))
//		//	{
//		//		ISpider spider = new DefaultSpider("test", new Site());

//		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
//		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo());
//		//		insertPipeline.AddEntity(metadata);
//		//		insertPipeline.InitPipeline(spider);

//		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
//		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

//		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline();
//		//		var metadat2 = EntitySpider.GenerateEntityMetaData(typeof(ProductUpdate).GetTypeInfo());
//		//		updatePipeline.AddEntity(metadat2);
//		//		updatePipeline.InitPipeline(spider);

//		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category", "4C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		updatePipeline.Process(metadat2.Name, new List<JObject> { data3 });

//		//		var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//		//		Assert.Equal(2, list.Count);
//		//		Assert.Equal("110", list[0].Sku);
//		//		Assert.Equal("4C", list[0].Category);
//		//	}

//		//	ClearDb();
//		//}

//		//[Fact]
//		//public void UpdateWhenUnionPrimaryUseAppConfig()
//		//{
//		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
//		//	ClearDb();

//		//	using (SqlConnection conn = new SqlConnection(ConnectString))
//		//	{
//		//		ISpider spider = new DefaultSpider("test", new Site());

//		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
//		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(Product2).GetTypeInfo());
//		//		insertPipeline.AddEntity(metadata);
//		//		insertPipeline.InitPipeline(spider);

//		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
//		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

//		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline();
//		//		var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(Product2Update).GetTypeInfo());
//		//		updatePipeline.AddEntity(metadata2);
//		//		updatePipeline.InitPipeline(spider);

//		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "AAAA" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		updatePipeline.Process(metadata2.Name, new List<JObject> { data3 });

//		//		var list = conn.Query<Product2>($"use test;select * from sku2_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//		//		Assert.Equal(2, list.Count);
//		//		Assert.Equal("110", list[0].Sku);
//		//		Assert.Equal("AAAA", list[0].Category);
//		//	}

//		//	ClearDb();
//		//}

//		//[Fact]
//		//public void UpdateCheckIfSameBeforeUpdateUseAppConfig()
//		//{
//		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");

//		//	ClearDb();

//		//	using (SqlConnection conn = new SqlConnection(ConnectString))
//		//	{
//		//		ISpider spider = new DefaultSpider("test", new Site());

//		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
//		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo());
//		//		insertPipeline.AddEntity(metadata);
//		//		insertPipeline.InitPipeline(spider);

//		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
//		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

//		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(null, true);
//		//		var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(ProductUpdate).GetTypeInfo());
//		//		updatePipeline.AddEntity(metadata2);
//		//		updatePipeline.InitPipeline(spider);

//		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category", "4C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		updatePipeline.Process(metadata2.Name, new List<JObject> { data3 });

//		//		var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//		//		Assert.Equal(2, list.Count);
//		//		Assert.Equal("110", list[0].Sku);
//		//		Assert.Equal("4C", list[0].Category);
//		//	}

//		//	ClearDb();
//		//}

//		//[Fact]
//		//public void UpdateWhenUnionPrimaryCheckIfSameBeforeUpdateUseAppConfig()
//		//{
//		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
//		//	ClearDb();

//		//	using (SqlConnection conn = new SqlConnection(ConnectString))
//		//	{
//		//		ISpider spider = new DefaultSpider("test", new Site());

//		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
//		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(Product2).GetTypeInfo());
//		//		insertPipeline.AddEntity(metadata);
//		//		insertPipeline.InitPipeline(spider);

//		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category1", "4C" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
//		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2 });

//		//		SqlServerEntityPipeline updatePipeline = new SqlServerEntityPipeline(null, true);
//		//		var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(Product2Update).GetTypeInfo());
//		//		updatePipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(Product2Update).GetTypeInfo()));
//		//		updatePipeline.InitPipeline(spider);

//		//		JObject data3 = new JObject { { "Sku", "110" }, { "Category1", "4C" }, { "Category", "AAAA" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		updatePipeline.Process(metadata2.Name, new List<JObject> { data3 });

//		//		var list = conn.Query<Product2>($"use test;select * from sku2_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//		//		Assert.Equal(2, list.Count);
//		//		Assert.Equal("110", list[0].Sku);
//		//		Assert.Equal("AAAA", list[0].Category);
//		//	}

//		//	ClearDb();
//		//}

//		//[Fact]
//		//public void InsertUseAppConfig()
//		//{
//		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");

//		//	ClearDb();

//		//	using (SqlConnection conn = new SqlConnection(ConnectString))
//		//	{
//		//		ISpider spider = new DefaultSpider("test", new Site());

//		//		SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
//		//		var metadata = EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo());
//		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(ProductInsert).GetTypeInfo()));
//		//		insertPipeline.InitPipeline(spider);

//		//		// Common data
//		//		JObject data1 = new JObject { { "Sku", "110" }, { "Category", "3C" }, { "Url", "http://jd.com/110" }, { "CDate", "2016-08-13" } };
//		//		JObject data2 = new JObject { { "Sku", "111" }, { "Category", "3C" }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
//		//		// Value is null
//		//		JObject data3 = new JObject { { "Sku", "112" }, { "Category", null }, { "Url", "http://jd.com/111" }, { "CDate", "2016-08-13" } };
//		//		insertPipeline.Process(metadata.Name, new List<JObject> { data1, data2, data3 });

//		//		var list = conn.Query<ProductInsert>($"use test;select * from sku_{DateTime.Now.ToString("yyyy_MM_dd")}").ToList();
//		//		Assert.Equal(3, list.Count);
//		//		Assert.Equal("110", list[0].Sku);
//		//		Assert.Equal("111", list[1].Sku);
//		//		Assert.Equal(null, list[2].Category);
//		//	}

//		//	ClearDb();
//		//}

//		//[Fact]
//		//public void DefineUpdateEntityUseAppConfig()
//		//{
//		//	Core.Environment.DataConnectionStringSettings = new System.Configuration.ConnectionStringSettings("SqlServer", "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True", "System.Data.SqlClient");
//		//	SqlServerEntityPipeline insertPipeline = new SqlServerEntityPipeline();
//		//	try
//		//	{
//		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity1).GetTypeInfo()));
//		//		throw new SpiderException("TEST FAILED.");
//		//	}
//		//	catch (SpiderException e)
//		//	{
//		//		Assert.Equal("Columns set as Primary is not a property of your entity.", e.Message);
//		//	}

//		//	try
//		//	{
//		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity2).GetTypeInfo()));
//		//		throw new SpiderException("TEST FAILED.");
//		//	}
//		//	catch (SpiderException e)
//		//	{
//		//		Assert.Equal("Columns set as update is not a property of your entity.", e.Message);
//		//	}

//		//	try
//		//	{
//		//		insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity3).GetTypeInfo()));
//		//		throw new SpiderException("TEST FAILED.");
//		//	}
//		//	catch (SpiderException e)
//		//	{
//		//		Assert.Equal("There is no column need update.", e.Message);
//		//	}
//		//	var metadata = EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity4).GetTypeInfo());
//		//	insertPipeline.AddEntity(EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity4).GetTypeInfo()));
//		//	Assert.Equal(1, insertPipeline.GetUpdateColumns(metadata.Name).Length);
//		//	Assert.Equal("Value", insertPipeline.GetUpdateColumns(metadata.Name).First());

//		//	SqlServerEntityPipeline insertPipeline2 = new SqlServerEntityPipeline(ConnectString);
//		//	var metadata2 = EntitySpider.GenerateEntityMetaData(typeof(UpdateEntity5).GetTypeInfo());
//		//	insertPipeline2.AddEntity(metadata2);
//		//	Assert.Equal(1, insertPipeline2.GetUpdateColumns(metadata2.Name).Length);
//		//	Assert.Equal("Value", insertPipeline2.GetUpdateColumns(metadata2.Name).First());
//		//}

//		//#endregion

//		[TableInfo("test", "sku", TableNamePostfix.Today, Indexs = new[] { "Category" }, Uniques = new[] { "Category,Sku", "Sku" })]
//		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
//		public class ProductInsert
//		{
//			[Field(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
//			public string Category { get; set; }

//			[Field(Expression = "./div[1]/a/@href")]
//			public string Url { get; set; }

//			[Field(Expression = "./div[1]/a", Length = 100)]
//			public string Sku { get; set; }
//		}

//		[TableInfo("test", "sku", TableNamePostfix.Today, UpdateColumns = new[] { "Category", "Sku" })]
//		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
//		public class ProductUpdate
//		{
//			[Field(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
//			public string Category { get; set; }

//			[Field(Expression = "./div[1]/a/@href")]
//			public string Url { get; set; }

//			[Field(Expression = "./div[1]/a", Length = 100)]
//			public string Sku { get; set; }
//		}


//		[TableInfo("test", "sku2", TableNamePostfix.Today, Indexs = new[] { "Sku,Category1" }, Uniques = new[] { "Sku" })]
//		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
//		public class Product2
//		{
//			[Field(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
//			public string Category1 { get; set; }

//			[Field(Expression = "name", Type = SelectorType.Enviroment)]
//			public string Category { get; set; }

//			[Field(Expression = "./div[1]/a/@href")]
//			public string Url { get; set; }

//			[Field(Expression = "./div[1]/a", Length = 100)]
//			public string Sku { get; set; }
//		}


//		[TableInfo("test", "sku2", TableNamePostfix.Today, Uniques = new[] { "Sku" }, UpdateColumns = new[] { "Category" })]
//		[EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
//		public class Product2Update
//		{
//			[Field(Expression = "name", Type = SelectorType.Enviroment, Length = 100)]
//			public string Category1 { get; set; }

//			[Field(Expression = "name", Type = SelectorType.Enviroment)]
//			public string Category { get; set; }

//			[Field(Expression = "./div[1]/a/@href")]
//			public string Url { get; set; }

//			[Field(Expression = "./div[1]/a", Length = 100)]
//			public string Sku { get; set; }
//		}

//		[TableInfo("test", "sku2", Uniques = new[] { "sku2" }, UpdateColumns = new[] { "Key" })]
//		public class UpdateEntity1
//		{
//			[Field(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
//			public string Key { get; set; }

//			[Field(Expression = "value", Type = SelectorType.Enviroment)]
//			public string Value { get; set; }

//		}

//		[TableInfo("test", "sku2", Uniques = new[] { "key" }, UpdateColumns = new[] { "value" })]
//		public class UpdateEntity2
//		{
//			[Field(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
//			public string Key { get; set; }

//			[Field(Expression = "value", Type = SelectorType.Enviroment, Length = 100)]
//			public string Value { get; set; }
//		}

//		[TableInfo("test", "sku2", Uniques = new[] { "Key" }, UpdateColumns = new[] { "id" })]
//		public class UpdateEntity3
//		{
//			[Field(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
//			public string Key { get; set; }

//			[Field(Expression = "value", Type = SelectorType.Enviroment)]
//			public string Value { get; set; }
//		}

//		[TableInfo("test", "sku2", Uniques = new[] { "Key" }, UpdateColumns = new[] { "Value" })]
//		public class UpdateEntity4
//		{
//			[Field(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
//			public string Key { get; set; }

//			[Field(Expression = "value", Type = SelectorType.Enviroment)]
//			public string Value { get; set; }
//		}

//		[TableInfo("test", "sku2", Uniques = new[] { "Key" }, UpdateColumns = new[] { "Value", "Key", "__Id" })]
//		public class UpdateEntity5
//		{
//			[Field(Expression = "key", Type = SelectorType.Enviroment, Length = 100)]
//			public string Key { get; set; }

//			[Field(Expression = "value", Type = SelectorType.Enviroment)]
//			public string Value { get; set; }
//		}
//	}
//}
