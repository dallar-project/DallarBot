using System.Net;

namespace Dallar.Services
{
    public interface IDadJokeService
    {
        string GetJoke();
    }

    public class DadJokeService : IDadJokeService
    {
        public DadJokeService()
        {

        }

        public string GetJoke()
        {
            var client = new WebClient();
            client.Headers.Add("Accept", "text/plain");
            return client.DownloadStringTaskAsync("https://icanhazdadjoke.com/").GetAwaiter().GetResult();
        }
    }
}
