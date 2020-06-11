using Common;
using Filetypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using twwstats_dataexporter.Services;

namespace twwstats_dataexporter
{
    class LocEntryComparer : IEqualityComparer<LocEntry>
    {
        public bool Equals(LocEntry b1, LocEntry b2)
        {
            return b1.Tag.Equals(b2.Tag);
        }

        public int GetHashCode(LocEntry bx)
        {
            return bx.Tag.GetHashCode();
        }
    }

    class TwwstatsDataExporter
    {
        private Dictionary<string, List<PackFile>> _dataPack;
        private Dictionary<string, List<PackFile>> _localPack;
        private Dictionary<string, DataTable> _loadedTables;

        public void Export(IList<GameVersion> versions)
        {
            // master_schema.xml is managed in the solution and copied to the bin root 
            // so use "" as the basepath
            DBTypeMap.Instance.InitializeTypeMap("");

            var codec = new PackFileCodec();

            _dataPack = new Dictionary<string, List<PackFile>>();
            _localPack = new Dictionary<string, List<PackFile>>();
            _loadedTables = new Dictionary<string, DataTable>();

            foreach (var v in versions)
            {
                var exportDir = $"{Common.DataPath}/Export/{v.Id}";
                if (Directory.Exists(exportDir)) {
                    Console.WriteLine($"[SKIP] Version {v.Name} already exists");
                    continue; 
                }

                Console.WriteLine($"[EXPORT] Version {v.Name}");

                bool isTWW = (v.Game == Game.TWW || v.Game == Game.TWW2);

                // For Mods, first load the latest version packs
                if (!isTWW)
                {
                    var latestVersion = versions.First(ver => ver.Game == Game.TWW2);
                    foreach (var packFile in Directory.GetFiles(latestVersion.Data, "data*.pack"))
                    {
                        if (_dataPack.ContainsKey(v.Id)) _dataPack[v.Id].Add(codec.Open(packFile));
                        else _dataPack[v.Id] = new List<PackFile> { codec.Open(packFile) };
                    }
                    foreach (var packFile in Directory.GetFiles(latestVersion.Data, "local*.pack"))
                    {
                        if (_localPack.ContainsKey(v.Id)) _localPack[v.Id].Add(codec.Open(packFile));
                        else _localPack[v.Id] = new List<PackFile> { codec.Open(packFile) };
                    }
                }

                var dataPackPattern = v.Game != Game.TWW2 ? "*.pack" : "data*.pack";
                var localPackPattern = v.Game != Game.TWW2 ? "*.pack" : "local*.pack";
                foreach (var packFile in Directory.GetFiles(v.Data, dataPackPattern))
                {
                    if (_dataPack.ContainsKey(v.Id)) _dataPack[v.Id].Add(codec.Open(packFile));
                    else _dataPack[v.Id] = new List<PackFile> { codec.Open(packFile) };
                }
                foreach (var packFile in Directory.GetFiles(v.Data, localPackPattern))
                {
                    if (_localPack.ContainsKey(v.Id)) _localPack[v.Id].Add(codec.Open(packFile));
                    else _localPack[v.Id] = new List<PackFile> { codec.Open(packFile) };
                }

                // DEBUGING PURPOSES
                //foreach (var p in _localPack[v.Id].Select(p => p.Root))
                //{
                //    foreach (var f in p.AllFiles)
                //    {
                //        byte[] data = f.Data;
                //        using (MemoryStream stream = new MemoryStream(data, 0, data.Length))
                //        {
                //            try
                //            {
                //                var locFile = LocCodec.Instance.Decode(stream);
                //                var found = locFile.Entries.FirstOrDefault(e => e.Localised.Contains("the minimum percentage of armour roll that can be applied"));
                //                if (found != null)
                //                {
                //                    Console.WriteLine($"FOUND IT: {p.Name} - {f.Name} - {found.Tag}");
                //                }
                //            }
                //            catch (Exception) { }
                //        }
                //    }
                //}

                
                Directory.CreateDirectory($"{exportDir}");

                // Export all xml files from the asssembly_kit if available
                var xmlFiles = Directory.GetFiles(v.Data, "*.xml", SearchOption.AllDirectories).ToList();
                xmlFiles.ForEach(f =>
                {
                    var xml = System.IO.File.ReadAllText(f);

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);

                    var tableName = Path.GetFileNameWithoutExtension(f);
                    var entries = doc.GetElementsByTagName(tableName).Cast<XmlNode>().Select(o =>
                    {
                        o.Attributes.RemoveAll();
                        foreach(XmlNode child in o.ChildNodes)
                        {
                            child.Attributes.RemoveAll();
                        }
                        return JsonConvert.SerializeXmlNode(o, Newtonsoft.Json.Formatting.None, true);
                    });

                    string json = $"[{string.Join(',', entries)}]";

                    File.WriteAllText($"{exportDir}/{tableName}.json", json);
                });

                // Export ALL the tables of the db subdirectories
                // Only do this as a fallback if we don't have xml files present (i.e. no assembly kit data)
                if (!xmlFiles.Any())
                {
                    // Load all texts at once because Mods don't name their tables to match the original tables
                    var text = Text(v.Id);

                    _dataPack[v.Id].Select(dp => dp.Root.GetSubdirectory("db")).SelectMany(vd => vd.Subdirectories.Select(s => s.Name)).Distinct().ToList().ForEach(packTableName =>
                    {
                        var realTableName = packTableName.Replace("_tables", "");

                        // DEBUG
                        // if (realTableName != "battle_entities") { return; }

                        // Console.WriteLine($"=== {realTableName} ===");
                        try
                        {
                            var table = Table(v.Id, packTableName);

                            var result = ConvertDataToString(table, text, realTableName);

                            File.WriteAllText($"{exportDir}/{realTableName}.json", result);
                        }
                        catch (Exception e) {
                            Console.WriteLine($"ERROR: Could not process {realTableName}: {e}");
                        }
                    });
                }

                // Export ALL the images in the ui directory and its subdirectories
                _dataPack[v.Id].Select(dp => dp.Root.GetSubdirectory("ui")).ToList().ForEach(vd =>
                {
                    exportImages(vd, exportDir);
                });
            }
        }

        private void searchKey(VirtualDirectory vd, string key)
        {
            if (vd.Files.Any())
            {
                foreach (var file in vd.Files)
                {
                    byte[] data = file.Data;
                    using (MemoryStream stream = new MemoryStream(data, 0, data.Length))
                    {
                        var locFile = LocCodec.Instance.Decode(stream);
                        var match = locFile.Entries.Where(e => e.Tag.Contains(key)).FirstOrDefault();
                        if (match != null)
                        {
                            var i = 0;
                        }
                    }

                }
            }
            foreach (var sub in vd.Subdirectories)
            {
                searchKey(vd, key);
            }
        }

        private void exportImages(VirtualDirectory vd, String basePath)
        {
            var dir = $"{basePath}/{vd.Name}";
            if (vd.Files.Any())
            {
                Directory.CreateDirectory(dir);
                foreach (var file in vd.Files)
                {
                    if (file.FullPath.EndsWith(".png"))
                    {
                        File.WriteAllBytes($"{dir}/{file.Name}", file.Data);
                    }
                }
            }
            foreach (var sub in vd.Subdirectories)
            {
                exportImages(sub, dir);
            }
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
                    if(editedFile != null)
                    {
                        var table_piece = CreateTable(file, editedFile);

                        if (table == null)
                            table = table_piece;
                        else
                            table.Merge(table_piece);
                    }
                }

                _loadedTables[key] = table;

                return table;
            }
        }

        public Dictionary<string, string> Text(string version)
        {
            lock (_dataPack)
            {
                var loc_files = _localPack[version].Select(p => p.Root.GetSubdirectory("text")?.GetSubdirectory("db"))
                    .Where(d => d != null)
                    .SelectMany(d => d.Files);

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

                // Reverse so older entries have priority so Mod overhauls take effect (I think... :P)
                result?.Entries.Reverse();
                var dict = result?.Entries.Distinct(new LocEntryComparer()).ToDictionary(e => e.Tag, e => e.Localised);

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
                if(constructionTable.PrimaryKey.Any())
                {
                    var key = new List<object>();
                    foreach (var pk in constructionTable.PrimaryKey)
                    {
                        var value = rowentry.Find(f => f.Name == pk.ColumnName).Value;
                        key.Add(value);
                    }
                    var keyArray = key.ToArray();

                    // Remove existing entry to avoid conflicts
                    if(constructionTable.Rows.Contains(keyArray))
                    {
                        var existing = constructionTable.Rows.Find(keyArray);
                        constructionTable.Rows.Remove(existing);
                    }
                }
                constructionTable.Rows.Add(rowentry.Select(n => n.Value).ToArray<object>());   
            }

            constructionTable.EndLoadData();
            constructionTable.AcceptChanges();

            return constructionTable;
        }

        // COPIED FROM TwwStatsControllerBase.cs
        private string ConvertDataToString(DataTable dt, Dictionary<string, string> texts, String realTableName)
        {
            var tableTexts = texts?.Where(p => p.Key.StartsWith(realTableName)).ToList();

            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            foreach (DataRow dr in dt.Rows)
            {
                var row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    var colName = col.ColumnName.Replace('-', '_');

                    // Convert String to Boolean
                    if (dr[col].ToString().ToLower() == "false")
                        row.Add(colName, false);
                    else if (dr[col].ToString().ToLower() == "true")
                        row.Add(colName, true);
                    else
                        row.Add(colName, dr[col]);
                }

                if(tableTexts != null && tableTexts.Any())
                {
                    var rowKeys = computePotentialKeys(dt.PrimaryKey.Select(pk => dr[pk.ColumnName].ToString()).ToList());
                    foreach(var rowKey in rowKeys)
                    {
                        tableTexts.Where(p => p.Key.EndsWith(rowKey)).ToList().ForEach(p => {
                                var key = p.Key.Replace($"_{rowKey}", "").Replace($"{realTableName}_", "");
                            if (row.ContainsKey(key)) { row[key] = p.Value; }
                            else { row.Add(key, p.Value); }
                        });
                    }
                }

                rows.Add(row);
            }
            return JsonConvert.SerializeObject(rows);
        }

        // From https://stackoverflow.com/questions/7802822/all-possible-combinations-of-a-list-of-values
        private List<String> computePotentialKeys(List<String> pks)
        {
            formPermut test = new formPermut();
            return test.prnPermut(pks.ToArray(), 0, pks.Count() - 1);
        }
    }
}

class formPermut
{
    public void swapTwoStrings(ref string a, ref string b)
    {
        string temp = a;
        a = b;
        b = temp;
    }
    public List<String> prnPermut(string[] list, int k, int m)
    {
        var results = new List<String>();
        int i;
        if (k == m)
        {
            var res = "";
            for (i = 0; i <= m; i++)
                res += list[i];
            results.Add(res);
        }
        else
        {
            for (i = k; i <= m; i++)
            {
                swapTwoStrings(ref list[k], ref list[i]);
                results.AddRange(prnPermut(list, k + 1, m));
                swapTwoStrings(ref list[k], ref list[i]);
            }
        }
            
        return results.Distinct().ToList();
    }
}
