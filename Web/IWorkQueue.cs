namespace OctoHook
{
	using System;
	using System.Threading.Tasks;

	public interface IWorkQueue
	{
		void Queue(Action work, string description);
	}
}
