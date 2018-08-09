### ��ɫ

**Broker**

+ �ַ�����飨Blockoutput���� POST http://broker/node/heartbeat
+ ���շֲ�ʽ�������Ļش����� (Blockinput): POST http://broker/node/block
+ ���� Request ����� Block, ���� Request ����: POST http://broker/requestqueue/{identity}
+ ͨ�� Identity ��ѯ�Ѿ���ɵ� Request �� Block Ϊ��λ : GET http://broker/requestqueue/{identity}

#### JOB

| Column | DataType | Value| Key |
|:---|:---|:---|:---|
|Id| VARCHAR(32)| GUID | Primary |
| JobType | INT | Block \| Application |  |
| Name | VARCHAR(50) | TASK1 |  |  |

#### JobProperty

		JobId VARCHAR(32): GUID
		NodeCount INT: 1
		NodeGroup VARCHAR(50): Vps | InHouse | Vps_Static_Ip
		Package VARCHAR(256): Http://a.com/app1.zip
		OS VARCHAR(50): Windows



+ �������������֣�Block | Application, Block ��ʹ�÷ֲ�ʽ��������������, �����������ݹܵ����� Worker ��

##### �������

1. Portal ͨ�� Broker API ���һ������
		
		NAME: TASK1
		CRON: */5 * * * *
		PROCESSOR: Console.SampleProcessor
		ARGUMENTS:
		DESCRIPTION: This is a test task.

+ ͨ�� Scheduler.NET API ������񲢻�÷��ص� SchedulerNetId, ����ɹ���ִ����һ��������ʧ�������쳣
+ ��� Job ��Ϣ�����ݿ���

2. Portal ͨ�� Broker API �޸�һ������

+ ͨ�� Scheduler.NET API �޸����� ����޸ĳɹ���ִ����һ��������ʧ�������쳣
+ �޸����ݿ��е� Job ��Ϣ

3. Portal ͨ�� Broker API ɾ��һ������

+ ͨ�� Scheduler.NET API ɾ������ ���ɾ���ɹ���ִ����һ��������ʧ�������쳣
+ �����ݿ���ɾ�� Job ��Ϣ

##### ����ִ��

1. ���� Wroker ʵ��





