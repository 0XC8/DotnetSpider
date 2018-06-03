using DotnetSpider.Extension.Model.Attribute;
using System.Collections.Generic;

namespace DotnetSpider.Extension.Model
{
	/// <summary>
	/// ��������ģ�͵Ķ���
	/// </summary>
	public interface IModel
	{
		/// <summary>
		/// ����ģ�͵�ѡ����
		/// </summary>
		Selector Selector { get; }

		/// <summary>
		/// �����ս������Ľ����ȡǰ Take ��ʵ��
		/// </summary>
		int Take { get; }

		/// <summary>
		/// ���� Take �ķ���, Ĭ���Ǵ�ͷ��ȡ
		/// </summary>
		bool TakeFromHead { get; }

		/// <summary>
		/// ����ʵ���Ӧ�����ݿ����Ϣ
		/// ���� TableInfo Ϊ��, �п�������ʱ���ݲ�����Ҫ����
		/// </summary>
		TableInfo TableInfo { get; }

		/// <summary>
		/// ����ʵ�嶨������ݿ�����Ϣ
		/// </summary>
		HashSet<Field> Fields { get; }

		/// <summary>
		/// Ŀ�����ӵ�ѡ����
		/// </summary>
		IEnumerable<TargetUrlsSelector> TargetUrlsSelectors { get; }

		/// <summary>
		/// ����ֵ��ѡ����
		/// </summary>
		IEnumerable<SharedValueSelector> SharedValueSelectors { get; }

		string Identity { get; }
	}
}