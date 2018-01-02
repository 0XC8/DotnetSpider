using System.Collections.Generic;

namespace DotnetSpider.Core.Pipeline
{
	/// <summary>
	/// Write results to console.
	/// Usually used in test.
	/// </summary>
	public class ConsolePipeline : BasePipeline
	{
		public override void Process(IEnumerable<ResultItems> resultItems, ISpider spider)
		{
			foreach (var resultItem in resultItems)
			{
				foreach (var entry in resultItem.Results)
				{
					System.Console.WriteLine(entry.Key + ":\t" + entry.Value);
				}
			}
		}
	}
}
