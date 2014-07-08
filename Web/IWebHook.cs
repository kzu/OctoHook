namespace OctoHook
{
    using Octokit;
	using System.Threading.Tasks;

    public interface IWebHook<TEvent>
    {
        Task ProcessAsync(TEvent @event);
    }
}