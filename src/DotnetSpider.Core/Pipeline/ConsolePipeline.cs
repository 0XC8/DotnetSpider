using System.Collections.Generic;

namespace DotnetSpider.Core.Pipeline
{
	/// <summary>
	/// ��ӡ���ݽ��������̨
	/// </summary>
	public class ConsolePipeline : BasePipeline
	{
		/// <summary>
		/// ��ӡ���ݽ��������̨
		/// </summary>
		/// <param name="resultItems">���ݽ��</param>
		/// <param name="spider">����</param>
		public override void Process(IEnumerable<ResultItems> resultItems, ISpider spider)
		{
			foreach (var resultItem in resultItems)
			{
				resultItem.Request.CountOfResults = 0;
				resultItem.Request.EffectedRows = 0;

				foreach (var entry in resultItem.Results)
				{
					System.Console.WriteLine(entry.Key + ":\t" + entry.Value);

					resultItem.Request.CountOfResults += 1;
					resultItem.Request.EffectedRows += 1;
				}
			}
		}
	}
}
