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
	}
}
