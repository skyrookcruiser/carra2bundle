using System.IO.Compression;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using SharpCompress;
using SharpCompress.Compressors.Xz;

namespace CarraRN
{
    internal class Program
    {
        public static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static string carraConvPath = Path.Combine(baseDir, "carra2bundle");
        public static string tempPath = Path.Combine(baseDir, "temp");

        [STAThread] //so openfiledialog works
        static void Main(string[] args)
        {
            if (Directory.Exists(carraConvPath)) Directory.Delete(carraConvPath, true);
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            Directory.CreateDirectory(carraConvPath);
            Directory.CreateDirectory(tempPath);
            Console.WriteLine("those who LML :skull: :mango:");

            OpenFileDialog ofd = new OpenFileDialog()
            {
                Title = "Select carra file(s)",
                Multiselect = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ofd.FileNames.ForEach(x => carra2bundle(x));
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static void carra2bundle(string filePath)
        {
            //extract zip
            DirectoryInfo zipOutputDir = Directory.CreateDirectory(Path.Combine(Program.tempPath, Path.GetFileName(filePath)));
            using (ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(Path.Combine(zipOutputDir.FullName));
            }

            foreach (var path in Directory.GetDirectories(zipOutputDir.FullName))
            {
                //get expected path of original bundle
                DirectoryInfo cur = Directory.CreateDirectory(path); //yes
                string expectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "..",
                                   "LocalLow",
                                   "Unity",
                                   "ProjectMoon_LimbusCompany",
                                    cur.Name,
                                    cur.GetDirectories()[0].Name,
                                    "__data"
                                    );
                Console.WriteLine(expectedPath);

                //for some strange reason, when you only use 1 AssetsManager it only returns the first one in foreach??
                //can someone explain why that is :xdskull:
                var manager = new AssetsManager();
                var bundleInst = manager.LoadBundleFile(expectedPath);
                var assetInst = manager.LoadAssetsFileFromBundle(bundleInst, 0, true);

                var asset = assetInst.file;
                var bundle = bundleInst.file;

                //get typetree stuff
                int i = 0;
                asset.Metadata.TypeTreeTypes.ForEach(x =>
                {
                    var b = manager.CreateValueBaseField(assetInst ,x.TypeId, x.ScriptTypeIndex); //unnecessary
                    Console.WriteLine($"found idx {i} typetree {b.TypeName}? scriptidx? {x.ScriptTypeIndex} typeid? {x.TypeId}");
                    i++;
                });
                
                //decompress xz file (each raw data is compressed to xz)
                foreach (string rawData in Directory.GetFiles(cur.FullName, "", SearchOption.AllDirectories))
                {
                    using (var xz = new XZStream(File.OpenRead(rawData)))
                    using (Stream toFile = new FileStream(rawData + ".raw_asset", FileMode.Create))
                    {
                        xz.CopyTo(toFile);
                    }

                    var bjgbgb = Path.GetFileName(rawData).Split('.');
                    long pathID = long.Parse(bjgbgb.First());
                    int treeID = int.Parse(bjgbgb.Last());
                    var treeInfo = asset.Metadata.TypeTreeTypes[treeID];
                    var scriptidx = treeInfo.ScriptTypeIndex;
                    var typeID = treeInfo.TypeId;

                    Console.WriteLine($"finding treeidx {treeID} scriptidx {scriptidx} for {pathID}");
                    var __new = AssetFileInfo.Create(asset, pathID, typeID, scriptidx);
                    //var __new__basefield = manager.GetBaseField(assetInst, __new); dont rlly need this
                    __new.SetNewData(File.ReadAllBytes(rawData+".raw_asset"));

                    var overwrite_exist = asset.GetAssetInfo(pathID);
                    if (overwrite_exist != null)
                    {
                        __new.SetNewData(File.ReadAllBytes(rawData + ".raw_asset")); 
                    }
                    else asset.Metadata.AddAssetInfo(__new);
                }

                //finally pack uncompressed bundle
                Console.WriteLine("Writing to file...");
                bundle.BlockAndDirInfo.DirectoryInfos[0].SetNewData(asset);
                var finalPath = Directory.CreateDirectory(Path.Combine(Program.carraConvPath, zipOutputDir.Name));
                using (AssetsFileWriter writer = new AssetsFileWriter(Path.Combine(finalPath.FullName, cur.Name + ".bundle"))) { bundle.Write(writer); }
            }

        }

        private static void OnApplicationQuit()
        {

        }

    }
}