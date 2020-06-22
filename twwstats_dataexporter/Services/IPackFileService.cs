using Common;
using System.Collections.Generic;
using System.Data;

namespace twwstats_dataexporter.Services
{
    public enum Game
    {
        TWW = 0,
        TWW2,
        SFO,
        CTT,
        RADIOUS,
        LSO,
        ENDTIMES
    }

    public class GameVersion
    {
        public Game Game { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public string Data { get; set; }
        public string Local { get; set; }
        public List<string> AdditionalDataPacks { get; set; }
    }

    public interface IPackFileService
    {
        void Initialize(IList<GameVersion> versions, string typemappath);

        IList<GameVersion> Versions();

        List<PackFile> Get(string version);

        DataTable Table(string version, string name);
        Dictionary<string, string> Text(string version, string name);
    }
}
