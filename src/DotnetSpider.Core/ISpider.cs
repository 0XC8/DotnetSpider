using DotnetSpider.Core.Downloader;
using DotnetSpider.Core.Monitor;
using System;

namespace DotnetSpider.Core
{
	/// <summary>
	/// ����ӿڶ���
	/// </summary>
	public interface ISpider : IDisposable, IControllable, IAppBase
	{
		/// <summary>
		/// �ɼ�վ�����Ϣ����
		/// </summary>
		Site Site { get; }

		/// <summary>
		/// Cookies, �����Ҫ����Cookies, ��Դ����Ը�һ��ȫ�µ�Cookies���󼴿�(������Ҳ�����滻)
		/// ���������в���ͨ��Cookies.AddCookies�ȷ���������µ�Cookie
		/// </summary>
		Cookies Cookies { get; set; }

		/// <summary>
		/// ��ؽӿ�
		/// </summary>
		IMonitor Monitor { get; set; }
	}
}
