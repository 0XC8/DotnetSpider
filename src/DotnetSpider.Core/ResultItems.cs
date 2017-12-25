using System.Collections.Concurrent;

namespace DotnetSpider.Core
{
	/// <summary>
	/// �洢ҳ����������ݽ��
	/// �˶��������ҳ�������, ���������ݹܵ�������
	/// </summary>
	public class ResultItems : ConcurrentDictionary<string, dynamic>
	{
		/// <summary>
		/// ��ǰ���������Ӧ��Ŀ��������Ϣ
		/// </summary>
		public Request Request { get; set; }

		/// <summary>
		/// ͨ����ֵȡ�����ݽ��
		/// </summary>
		/// <param name="key">��ֵ</param>
		/// <returns>���ݽ��</returns>
		public dynamic GetResultItem(string key)
		{
			return TryGetValue(key, out dynamic result) ? result : null;
		}
	}
}