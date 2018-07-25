using Dallar.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dallar.Services
{
    public interface IFunServiceCollection
    {
        IChuckNorrisJokeService ChuckNorrisJokeService { get; }
        IDadJokeService DadJokeService { get; }
        IRandomManagerService RandomManagerService { get; }
        IYoMommaJokeService YoMommaJokeService { get; }
    }

    public class FunServiceCollection : IFunServiceCollection
    {
        public IChuckNorrisJokeService ChuckNorrisJokeService { get; internal set; }
        public IDadJokeService DadJokeService { get; internal set; }
        public IRandomManagerService RandomManagerService { get; internal set; }
        public IYoMommaJokeService YoMommaJokeService { get; internal set; }

        public FunServiceCollection()
        {
            ChuckNorrisJokeService = new ChuckNorrisJokeService();
            DadJokeService = new DadJokeService();
            RandomManagerService = new RandomManagerService();
            YoMommaJokeService = new YoMommaJokeService();
        }
    }
}
