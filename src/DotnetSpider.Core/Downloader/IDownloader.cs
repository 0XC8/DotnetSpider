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
		/// ����Cookie
		/// </summary>
		/// <param name="cookies">Cookies</param>
		void ResetCookies(Cookies cookies);
	}
}
