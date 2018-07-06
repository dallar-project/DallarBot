using System;
using System.IO;

namespace DallarBot.Services
{
    public class YoMommaJokeService
    {
        protected static string[] YoMommaJokes = { };
        protected static Random YoMommaRandom = new Random();

        public YoMommaJokeService()
        {
            PopulateMommaJokes();
        }

        public bool PopulateMommaJokes()
        {
            if (YoMommaJokes.Length == 0)
            {
                YoMommaJokes = File.ReadAllLines("momma.txt");
            }

            return YoMommaJokes.Length > 1;
        }

        public string GetRandomYoMommaJoke()
        {
            PopulateMommaJokes();
            return YoMommaJokes[YoMommaRandom.Next(YoMommaJokes.Length - 1)];
        }
    }
}
