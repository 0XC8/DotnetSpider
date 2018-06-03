using System;
using System.Collections.Generic;
using DotnetSpider.Extension.Model;
using System.Linq;
using DotnetSpider.Core;
using Newtonsoft.Json;

namespace DotnetSpider.Extension.Pipeline
{
	/// <summary>
	/// Print datas in console
	/// Usually used in test.
	/// </summary>
	public class ConsoleEntityPipeline : ModelPipeline
	{
		/// <summary>
		/// ��ӡ����ʵ���������������ʵ�����ݽ��������̨
		/// </summary>
		/// <param name="model">����ʵ���������</param>
		/// <param name="datas">ʵ��������</param>
		/// <param name="spider">����</param>
		/// <returns>����Ӱ��������(�����ݿ�Ӱ������)</returns>
		public override int Process(IModel model, IEnumerable<dynamic> datas, ISpider spider)
		{
			foreach (var data in datas)
			{
				Console.WriteLine($"{model.Identity}: {JsonConvert.SerializeObject(data)}");
			}
			return datas.Count();
		}
	}
}
