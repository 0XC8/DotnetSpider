﻿using System;
using System.Collections.Generic;
using Java2Dotnet.Spider.Common;
using Java2Dotnet.Spider.Redial;
using Java2Dotnet.Spider.JLog;

namespace Java2Dotnet.Spider.Core.Downloader
{
	public class BaseDownloader : IDownloader, IDisposable
	{
		public DownloadValidation DownloadValidation { get; set; }
		public int ThreadNum { set; get; }

#if !NET_CORE
		//protected static readonly ILog Logger = LogManager.GetLogger(typeof(BaseDownloader));
		protected static readonly ILog Logger = LogManager.GetLogger();
#else
		protected static readonly ILog Logger = LogManager.GetLogger();
#endif

		protected SingleExecutor SingleExecutor = new SingleExecutor();

		public Action CustomizeCookie;

		public virtual Page Download(Request request, ISpider spider)
		{
			return null;
		}

		public virtual void Dispose()
		{
		}

		protected void ValidatePage(Page page)
		{
			//customer verify
			if (DownloadValidation != null)
			{
				var validatResult = DownloadValidation(page);

				switch (validatResult)
				{
					case DownloadValidationResult.Failed:
						{
							throw new RedialException("Customize validate failed.");
						}
					case DownloadValidationResult.FailedAndNeedRedial:
						{
							if (RedialManagerUtils.RedialManager == null)
							{
								throw new RedialException("RedialManager is null.");
							}

							RedialManagerUtils.RedialManager?.Redial();
							throw new RedialException("Download failed and Redial already.");
						}
					case DownloadValidationResult.Success:
						{
							break;
						}
					case DownloadValidationResult.FailedAndNeedUpdateCookie:
						{
							SingleExecutor.Execute(() =>
							{
								CustomizeCookie?.Invoke();
							});
							throw new RedialException("Cookie validate failed.");
						}
					case DownloadValidationResult.FailedAndNeedRetryOrWait:
						{
							throw new SpiderExceptoin("Need retry.");
						}
					case DownloadValidationResult.Miss:
						{
							page.IsSkip = true;
							break;
						}
				}
			}
		}

		public virtual IDownloader Clone()
		{
			return (IDownloader)MemberwiseClone();
		}
	}
}
