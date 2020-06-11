using Common;
using Filetypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace twwstats_dataexporter.Services
{
    public class PackFileService : IPackFileService
    {
        private IList<GameVersion> _versions;
        private Dictionary<string, List<PackFile>> _dataPack;
        private Dictionary<string, List<PackFile>> _localPack;
        private Dictionary<string, DataTable> _loadedTables;
        private Dictionary<string, Dictionary<string, string>> _loadedLocFiles;
        List<string> _pks;

        public void Initialize(IList<GameVersion> versions, string typemappath)
        {
            _pks = new List<string> { "id", "key", "unit", "unit_key", "unit_class" };

            // For reference purposes
            _versions = versions;

            DBTypeMap.Instance.InitializeTypeMap(typemappath);

            var codec = new PackFileCodec();

            _dataPack = new Dictionary<string, List<PackFile>>();
            _localPack = new Dictionary<string, List<PackFile>>();
            _loadedTables = new Dictionary<string, DataTable>();
            _loadedLocFiles = new Dictionary<string, Dictionary<string, string>>();
            foreach (var v in versions)
            {
                foreach (var packFile in Directory.GetFiles(v.Data, "data*.pack")) {
                    if (_dataPack.ContainsKey(v.Id)) _dataPack[v.Id].Add(codec.Open(packFile)); 
                    else _dataPack[v.Id] = new List<PackFile> { codec.Open(packFile) };
                }
                foreach (var packFile in Directory.GetFiles(v.Data, "local*.pack"))
                {
                    if (_localPack.ContainsKey(v.Id)) _localPack[v.Id].Add(codec.Open(packFile));
                    else _localPack[v.Id] = new List<PackFile> { codec.Open(packFile) };
                }

                var dir = $"{Common.DataPath}/Export/{v.Id}";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);

                    // Export ALL the tables of the db subdirectories
                    var virtualDirectories = _dataPack[v.Id].Select(dp => dp.Root.GetSubdirectory("db"));
                    foreach (var tableName in virtualDirectories.SelectMany(vd => vd.Subdirectories.Select(s => s.Name)).Distinct())
                    {
                        try
                        {
                            var table = Table(v.Id, tableName);
                            var text = Text(v.Id, tableName.Replace("_tables", ""));

                            var result = ConvertDataToString(GetPk(table), table, text);

                            File.WriteAllText($"{dir}/{tableName}.json", result);
                        } catch (Exception e)
                        {
                            Console.WriteLine($"Failed to export table {tableName}: {e.Message}");
                        }
                    }

                    // Export ALL the images in the ui directory and its subdirectories
                    virtualDirectories = _dataPack[v.Id].Select(dp => dp.Root.GetSubdirectory("ui"));
                    foreach(var vd in virtualDirectories)
                    {
                        exportImages(vd, dir);
                    }
                }
            }
        }

        private void exportImages(VirtualDirectory vd, String basePath)
        {
            var dir = $"{basePath}/{vd.Name}";
            if (vd.Files.Any())
            {
                Directory.CreateDirectory(dir);
                foreach(var file in vd.Files)
                {
                    if(file.FullPath.EndsWith(".png"))
                    {
                        File.WriteAllBytes($"{dir}/{file.Name}", file.Data);
                    }
                }
            }
            foreach(var sub in vd.Subdirectories)
            {
                exportImages(sub, dir);
            }
        }

        public IList<GameVersion> Versions()
        {
            return _versions;
        }

        public List<PackFile> Get(string version)
        {
            return _dataPack[version];
        }

        public DataTable Table(string version, string name)
        {
            var key = $"{version}_{name}";
            lock (_dataPack)
            {
                if (_loadedTables.ContainsKey(key))
                    return _loadedTables[key];

                var table_files = _dataPack[version].Select(dp => dp.Root.GetSubdirectory("db")?.GetSubdirectory(name)).Where(tf => tf != null);

                DataTable table = null;
                foreach (var file in table_files.SelectMany(tf => tf.Files))
                {
                    var editedFile = PackedFileDbCodec.Decode(file);
                    var table_piece = CreateTable(file, editedFile);

                    if (table == null)
                        table = table_piece;
                    else
                        table.Merge(table_piece);
                }

                _loadedTables[key] = table;

                return table;
            }
        }

        public Dictionary<string, string> Text(string version, string name)
        {
            var key = $"{version}_{name}";
            lock (_dataPack)
            {
                if (_loadedLocFiles.ContainsKey(key))
                    return _loadedLocFiles[key];

                var loc_files = _localPack[version].Select(p => p.Root.GetSubdirectory("text")?.GetSubdirectory("db"))
                    .Where(d => d != null)
                    .SelectMany(d => d.Files.Where(f => f.Name.StartsWith(name)));

                LocFile result = null;
                foreach (var file in loc_files)
                {
                    byte[] data = file.Data;
                    using (MemoryStream stream = new MemoryStream(data, 0, data.Length))
                    {
                        var locFile = LocCodec.Instance.Decode(stream);
                        if (result == null)
                            result = locFile;
                        else
                            result.Entries.AddRange(locFile.Entries);
                    }
                }

                var dict = result?.Entries.ToDictionary(e => e.Tag.Replace(name + "_", ""), e => e.Localised);

                _loadedLocFiles[key] = dict;

                return dict;
            }
        }


        /********************************************************************************************
         * This function constructs the System.Data.DataTable we use to not only store our data,    *
         * but to bind as our visuals data source.                                                  *
         ********************************************************************************************/
        private DataTable CreateTable(PackedFile currentPackedFile, DBFile table)
        {
            DataTable constructionTable = new DataTable(currentPackedFile.Name);

            DataColumn constructionColumn;
            List<DataColumn> keyList = new List<DataColumn>();
            constructionTable.BeginLoadData();

            foreach (FieldInfo columnInfo in table.CurrentType.Fields)
            {
                // Create the new column, using object as the data type for all columns, this way we avoid the WPF DataGrid's built in
                // data validation abilities in favor of our own implementation.
                constructionColumn = new DataColumn(columnInfo.Name, typeof(string));

                if (columnInfo.TypeCode == TypeCode.Int16 || columnInfo.TypeCode == TypeCode.Int32 || columnInfo.TypeCode == TypeCode.Single)
                {
                    constructionColumn = new DataColumn(columnInfo.Name, typeof(double));
                }

                constructionColumn.AllowDBNull = true;
                constructionColumn.Unique = false;
                constructionColumn.ReadOnly = true;

                // Save the FKey if it exists
                if (!String.IsNullOrEmpty(columnInfo.ForeignReference))
                {
                    constructionColumn.ExtendedProperties.Add("FKey", columnInfo.ForeignReference);
                }

                // If the column is a primary key, save it for later adding
                if (columnInfo.PrimaryKey)
                {
                    keyList.Add(constructionColumn);
                }

                constructionTable.Columns.Add(constructionColumn);
            }

            // If the table has primary keys, set them.
            if (keyList.Count > 0)
            {
                constructionTable.PrimaryKey = keyList.ToArray();
            }

            // Now that the DataTable schema is constructed, add in all the data.
            foreach (List<FieldInstance> rowentry in table.Entries)
            {
                constructionTable.Rows.Add(rowentry.Select(n => n.Value).ToArray<object>());
            }

            constructionTable.EndLoadData();
            constructionTable.AcceptChanges();

            return constructionTable;
        }

        // COPIED FROM TwwStatsControllerBase.cs
        private string ConvertDataToString(string pk, DataTable dt, Dictionary<string, string> texts)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    // Convert String to Boolean
                    if (dr[col].ToString().ToLower() == "false")
                        row.Add(col.ColumnName, false);
                    else if (dr[col].ToString().ToLower() == "true")
                        row.Add(col.ColumnName, true);
                    else
                        row.Add(col.ColumnName, dr[col]);

                    if (texts != null && col.ColumnName == pk)
                    {
                        var pkValue = dr[col].ToString();
                        texts.Where(p => p.Key.EndsWith(pkValue)).ToList().ForEach(p => row.Add(p.Key.Replace("_" + pkValue, ""), p.Value));
                    }
                }
                rows.Add(row);
            }
            return JsonConvert.SerializeObject(rows);
        }

        // COPIED FROM TablesController.cs
        private string GetPk(DataTable table)
        {
            // CHEESY: Avoid issues for tables with multiple PK like unit_variants_tables that has "faction" and "unit"
            var pk = table.PrimaryKey.Where(c => _pks.Contains(c.ColumnName)).FirstOrDefault()?.ColumnName;
            pk = pk ?? table.Columns.Cast<DataColumn>().Where(c => _pks.Contains(c.ColumnName)).FirstOrDefault().ColumnName;

            return pk;
        }
    }
}
