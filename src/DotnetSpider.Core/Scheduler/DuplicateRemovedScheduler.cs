using DotnetSpider.Core.Scheduler.Component;
using System.Collections.Generic;
using DotnetSpider.Core.Redial;

namespace DotnetSpider.Core.Scheduler
{
	/// <summary>
	/// Remove duplicate urls and only push urls which are not duplicate.
	/// </summary>
	public abstract class DuplicateRemovedScheduler : Named, IScheduler
	{
		protected IDuplicateRemover DuplicateRemover { get; set; } = new HashSetDuplicateRemover();
		protected ISpider Spider { get; set; }

		public abstract void IncreaseSuccessCount();
		public abstract void IncreaseErrorCount();

		public abstract void Import(HashSet<Request> requests);

		protected abstract bool UseInternet { get; set; }

		public abstract long LeftRequestsCount { get; }

		public abstract long TotalRequestsCount { get; }

		public abstract long SuccessRequestsCount { get; }

		public abstract long ErrorRequestsCount { get; }

		public bool DepthFirst { get; set; } = true;

		public void Push(Request request)
		{
			if (UseInternet)
			{
				NetworkCenter.Current.Execute("sch-push", () =>
				{
					DoPush(request);
				});
			}
			else
			{
				DoPush(request);
			}
		}

		public virtual void Init(ISpider spider)
		{
			if (Spider == null)
			{
				Spider = spider;
			}
			else
			{
				throw new SpiderException("Scheduler already init.");
			}
		}

		public abstract void ResetDuplicateCheck();

		public virtual Request Poll()
		{
			return null;
		}

		public virtual void Dispose()
		{
			DuplicateRemover.Dispose();
		}

		public virtual void Export()
		{
		}

		public virtual void Clear()
		{
		}

		protected virtual void PushWhenNoDuplicate(Request request)
		{
		}

		private bool ShouldReserved(Request request)
		{
			return request.CycleTriedTimes > 0 && request.CycleTriedTimes <= Spider.Site.CycleRetryTimes;
		}

		private void DoPush(Request request)
		{
			if (!DuplicateRemover.IsDuplicate(request) || ShouldReserved(request))
			{
				PushWhenNoDuplicate(request);
			}
		}
	}
}