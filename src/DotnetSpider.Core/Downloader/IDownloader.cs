using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;

namespace DotnetSpider.Core.Downloader
{
	/// <summary>
	/// �������ӿ�
	/// </summary>
	public interface IDownloader : System.IDisposable
	{
		/// <summary>
		/// ������������
		/// </summary>
		/// <param name="request">��������</param>
		/// <param name="spider">����ӿ�</param>
		/// <returns>�������ݷ�װ�õ�ҳ�����</returns>
		Page Download(Request request, ISpider spider);

		/// <summary>
		/// ���������ɺ�ĺ����������
		/// </summary>
		/// <param name="handler"><see cref="IAfterDownloadCompleteHandler"/></param>
		void AddAfterDownloadCompleteHandler(IAfterDownloadCompleteHandler handler);

		/// <summary>
		/// ������ز���ǰ�Ĵ������
		/// </summary>
		/// <param name="handler"><see cref="IBeforeDownloadHandler"/></param>
		void AddBeforeDownloadHandler(IBeforeDownloadHandler handler);

		/// <summary>
		/// ���� Cookie
		/// </summary>
		/// <param name="cookie">Cookie</param>
		void AddCookie(Cookie cookie);

		/// <summary>
		/// ���� Cookie
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="value">Value</param>
		/// <param name="domain">������</param>
		/// <param name="path">����·��</param>
		void AddCookie(string name, string value, string domain, string path = "/");

		/// <summary>
		/// ���Cookies
		/// </summary>
		/// <param name="cookies">Cookies�ļ�ֵ��</param>
		/// <param name="domain">������</param>
		/// <param name="path">����·��</param>
		void AddCookies(IDictionary<string, string> cookies, string domain, string path = "/");

		/// <summary>
		/// ���� Cookies
		/// </summary>
		/// <param name="cookiesStr">Cookies�ļ�ֵ���ַ���, ��: a1=b;a2=c;</param>
		/// <param name="domain">������</param>
		/// <param name="path">����·��</param>
		void AddCookies(string cookiesStr, string domain, string path = "/");

		/// <summary>
		/// Cookie ע����
		/// </summary>
		ICookieInjector CookieInjector { get; set; }

		/// <summary>
		/// ��¡һ��������, ���߳�ʱ, ÿ���߳�ʹ��һ������������, ������WebDriver������������Ҫ����WebDriver����ĸ�����, ÿ����������ֻ����һ��WebDriver
		/// </summary>
		/// <returns>������</returns>
		IDownloader Clone();

		/// <summary>
		/// Gets a System.Net.CookieCollection that contains the System.Net.Cookie instances that are associated with a specific URI.
		/// </summary>
		/// <param name="uri">The URI of the System.Net.Cookie instances desired.</param>
		/// <returns>A System.Net.CookieCollection that contains the System.Net.Cookie instances that are associated with a specific URI.</returns>
		CookieCollection GetCookies(Uri uri);
	}
}
