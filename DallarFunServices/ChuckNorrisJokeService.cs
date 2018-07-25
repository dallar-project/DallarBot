using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;

namespace Dallar.Services
{
    public interface IChuckNorrisJokeService
    {
        string GetJoke(string FirstName = "Michael", string LastName = "Allar");
    }

    public class ChuckNorrisJokeService : IChuckNorrisJokeService
    {
        public ChuckNorrisJokeService()
        {

        }

        public string GetJoke(string FirstName = "Michael", string LastName = "Allar")
        {
            var httpClient = new HttpClient();
            var content = httpClient.GetStringAsync($"http://api.icndb.com/jokes/random?firstName={FirstName}&lastName={LastName}").GetAwaiter().GetResult();

            try
            {
                dynamic jokeResult = JsonConvert.DeserializeObject(content);
                string joke = jokeResult.value.joke;
                return System.Net.WebUtility.HtmlDecode(joke);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to fetch Norris joke. " + e.Message);
            }

            return null;
        }
    }
}
