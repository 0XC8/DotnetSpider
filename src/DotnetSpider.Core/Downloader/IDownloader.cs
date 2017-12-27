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

		void AddAfterDownloadCompleteHandler(IAfterDownloadCompleteHandler handler);

		void AddBeforeDownloadHandler(IBeforeDownloadHandler handler);
	}
}
