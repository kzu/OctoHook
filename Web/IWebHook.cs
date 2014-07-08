namespace OctoHook
{
    using Octokit;
	using System.Threading.Tasks;

    public interface IWebHook<TEvent>
    {
		string Describe(TEvent @event);
        void Process(TEvent @event);
    }
}