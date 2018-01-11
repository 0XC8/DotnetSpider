﻿using System.Collections.Generic;
using DotnetSpider.Extension.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System;
using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Redial;
using System.Linq;
using DotnetSpider.Core;

namespace DotnetSpider.Extension.Pipeline
{
	public class MongoDbEntityPipeline : BaseEntityPipeline
	{
		private readonly ConcurrentDictionary<string, IMongoCollection<BsonDocument>> _collections = new ConcurrentDictionary<string, IMongoCollection<BsonDocument>>();

		private string ConnectString { get; }

		public MongoDbEntityPipeline(string connectString)
		{
			ConnectString = connectString;
		}

		public override void AddEntity(IEntityDefine metadata)
		{
			if (metadata.TableInfo == null)
			{
				Logger.Log($"Schema is necessary, skip {GetType().Name} for {metadata.Name}.", Level.Warn);
				return;
			}

			MongoClient client = new MongoClient(ConnectString);
			var db = client.GetDatabase(metadata.TableInfo.Database);

			_collections.TryAdd(metadata.Name, db.GetCollection<BsonDocument>(metadata.TableInfo.CalculateTableName()));
		}

		public override int Process(string entityName, IEnumerable<dynamic> datas, ISpider spider)
		{
			if (_collections.TryGetValue(entityName, out var collection))
			{
				var action = new Action(() =>
				{
					List<BsonDocument> reslut = new List<BsonDocument>();
					foreach (var data in datas)
					{
						BsonDocument item = BsonDocument.Create(data);
						reslut.Add(item);
					}
					reslut.Add(BsonDocument.Create(DateTime.Now));
					collection.InsertMany(reslut);
				});
				if (DbExecutor.UseNetworkCenter)
				{
					NetworkCenter.Current.Execute("db", action);
				}
				else
				{
					action();
				}
			}
			return datas.Count();
		}
	}
}