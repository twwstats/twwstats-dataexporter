using System;
using System.Data;
using System.Linq;
using twwstats_dataexporter.Services;

namespace twwstats_dataexporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var data_path = Common.DataPath;
            var versions = Common.GameVersions.Select(d => { 
                bool isTWW = (d.Item1 == Game.TWW || d.Item1 == Game.TWW2);
                string dataPath = isTWW ? d.Item1.ToString().ToLower() : "mods";
                return new GameVersion
                {
                    Game = d.Item1,
                    Name = d.Item2,
                    Id = $"{d.Item3}",
                    Data = $@"{data_path}/{dataPath}/{d.Item3}/"
                };
            }).ToList();

            var exporter = new TwwstatsDataExporter();
            exporter.Export(versions);
        }
    }
}
