using System;
using System.Collections.Generic;

namespace DotnetSpider.Core.Pipeline
{
	/// <summary>
	/// ���ݹܵ��ӿ�, ͨ�����ݹܵ��ѽ��������ݴ浽��ͬ�Ĵ洢��(�ļ������ݿ⣩
	/// </summary>
	public interface IPipeline : IDisposable
	{
		///// <summary>
		///// ���ݹܵ�������������
		///// </summary>
		//ISpider Spider { get; }

		///// <summary>
		///// ��ʼ�����ݹܵ�
		///// </summary>
		///// <param name="spider">��������</param>
		//void Init(ISpider spider);

		/// <summary>
		/// ����ҳ������������������ݽ��
		/// </summary>
		/// <param name="resultItems">���ݽ��</param>
		void Process(IEnumerable<ResultItems> resultItems, ISpider spider);

		/// <summary>
		/// ��ʹ�����ݹܵ�ǰ, ����һЩ��ʼ������, �������е����ݹܵ�����Ҫ���г�ʼ��
		/// </summary>
		void Init();
	}
}