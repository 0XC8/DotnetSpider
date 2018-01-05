using DotnetSpider.Core.Downloader;
using DotnetSpider.Core.Monitor;
using System;
using System.Net;

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
		/// ��ؽӿ�
		/// </summary>
		IMonitor Monitor { get; set; }

		/// <summary>
		/// ���� Cookie
		/// </summary>
		/// <param name="cookie">Cookie</param>
		void AddCookie(Cookie cookie);

		/// <summary>
		/// ���� Cookies
		/// </summary>
		/// <param name="cookiesStr">Cookies�ļ�ֵ���ַ���, ��: a1=b;a2=c;</param>
		/// <param name="domain">������</param>
		/// <param name="path">����·��</param>
		void AddCookies(string cookiesStr, string domain, string path = "/");
	}
}
