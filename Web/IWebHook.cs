namespace OctoHook
{
    using Octokit;
	using System.Threading.Tasks;

    public interface IWebHook<TEvent>
    {
        void Process(TEvent @event);
    }
}