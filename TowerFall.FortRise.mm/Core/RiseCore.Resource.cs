using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Monocle;
using TowerFall;

namespace FortRise;

public partial class RiseCore 
{
    public sealed class ResourceTypeFile {}
    public sealed class ResourceTypeFolder {}

    public sealed class ResourceTypeAssembly {}
    public sealed class ResourceTypeXml {}
    public sealed class ResourceTypeJson {}
    public sealed class ResourceTypeHJson {}
    public sealed class ResourceTypeOel {}
    public sealed class ResourceTypeQuestTowerFolder {}
    public sealed class ResourceTypeDarkWorldTowerFolder {}
    public sealed class ResourceTypeVersusTowerFolder {}
    public sealed class ResourceTypeTrialsTowerFolder {}
    public sealed class ResourceTypeAtlas {}
    public sealed class ResourceTypeSpriteData {}
    public sealed class ResourceTypeGameData {}
    public sealed class ResourceTypeWaveBank {}
    public sealed class ResourceTypeSoundBank {}
    public sealed class ResourceTypeWavFile {}
    public sealed class ResourceTypeOggFile {}
    public sealed class ResourceTypeAudioEngine {}
    public sealed class ResourceTypeEffects {}

    internal static HashSet<string> BlacklistedExtension = new() {
        ".csproj", ".cs", ".md", ".toml", ".aseprite", ".ase", ".xap"
    };

    internal static HashSet<string> BlacklistedRootFolders = new() {
        "bin", "obj"
    };

    internal static HashSet<string> BlacklistedCommonFolders = new() {
        "packer/"
    };

    private static char[] SplitSeparator = new char[1] { '/' };

    /// <summary>
    /// A class that contains a path and stream to your resource works both on folder and zip. 
    /// </summary>
    public abstract class Resource 
    {
        public string FullPath;
        public string Path;
        public string Root;
        public string RootPath => Root + Path;
        public List<Resource> Childrens = new();
        public ModResource Source;
        public Type ResourceType;

        public abstract Stream Stream { get; }

        public Resource(ModResource resource, string path, string fullPath) 
        {
            FullPath = fullPath;
            Path = path;
            Source = resource;
        }

        public virtual void AssignType() 
        {
            var path = Path;
            var filename = System.IO.Path.GetFileName(path);

            if (path.StartsWith("Content/Atlas") && filename.EndsWith(".png")) 
            {
                foreach (var ext in AtlasReader.InternalReaders.Keys) 
                {
                    if (ResourceTree.IsExist(this, path.Replace(".png", ext))) 
                    {
                        ResourceType = typeof(ResourceTypeAtlas);
                    }
                }
            }
            else if (path.EndsWith(".dll")) 
            {
                ResourceType = typeof(ResourceTypeAssembly);
            }
            else if (path.StartsWith("Content/Atlas/GameData") && filename.EndsWith(".xml")) 
            {
                ResourceType = typeof(ResourceTypeGameData);
            }
            else if (path.StartsWith("Content/Atlas/SpriteData") && filename.EndsWith(".xml")) 
            {
                ResourceType = typeof(ResourceTypeSpriteData);
            }
            else if (path.StartsWith("Content/Levels/DarkWorld")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeDarkWorldTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Content/Levels/Versus")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeVersusTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Content/Levels/Quest")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeQuestTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Content/Levels/Trials")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeTrialsTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Content/Music")) 
            {
                if (path.EndsWith(".xgs")) 
                {
                    ResourceType = typeof(ResourceTypeAudioEngine);
                }
                else if (path.EndsWith(".xsb")) 
                {
                    ResourceType = typeof(ResourceTypeSoundBank);
                }
                else if (path.EndsWith("xwb")) 
                {
                    ResourceType = typeof(ResourceTypeWaveBank);
                }
                else if (path.EndsWith(".wav")) 
                {
                    ResourceType = typeof(ResourceTypeWavFile);
                }
                else if (path.EndsWith(".ogg")) 
                {
                    ResourceType = typeof(ResourceTypeOggFile);
                }
                // FIXME fix normal file
                else 
                {
                    ResourceType = typeof(ResourceTypeFile);
                }
            }
            else if (path.StartsWith("Content/SFX")) 
            {
                if (path.EndsWith(".wav")) 
                {
                    ResourceType = typeof(ResourceTypeWavFile);
                }
                else if (path.EndsWith(".ogg")) 
                {
                    ResourceType = typeof(ResourceTypeOggFile);
                }
                // FIXME fix normal file
                else 
                {
                    ResourceType = typeof(ResourceTypeFile);
                }
            }
            else if (path.EndsWith(".xml")) 
            {
                ResourceType = typeof(ResourceTypeXml);
            }
            else if (path.EndsWith(".fxb")) 
            {
                ResourceType = typeof(ResourceTypeEffects);
            }
            else if (Childrens.Count != 0) 
            {
                ResourceType = typeof(ResourceTypeFolder);
            }
            else 
            {
                ResourceType = typeof(ResourceTypeFile);
            }

            void AssignLevelFile() 
            {
                if (path.EndsWith(".json")) 
                {
                    ResourceType = typeof(ResourceTypeJson);
                }
                else if (path.EndsWith(".hjson")) 
                {
                    ResourceType = typeof(ResourceTypeHJson);
                }
                else if (path.EndsWith(".oel")) 
                {
                    ResourceType = typeof(ResourceTypeOel);
                }
            }
        }
    }

    public class FileResource : Resource
    {
        public FileResource(ModResource resource, string path, string fullPath) : base(resource, path, fullPath)
        {
        }

        public override Stream Stream 
        {
            get 
            {
                if (!File.Exists(FullPath))
                    return null;
                return File.OpenRead(FullPath);
            }
        }
    }

    public class ZipResource : Resource
    {
        public ZipEntry Entry;

        public ZipResource(ModResource resource, string path, string fullPath, ZipEntry entry) : base(resource, path, fullPath)
        {
            Entry = entry;
        }

        public override Stream Stream 
        {
            get => Entry.ExtractStream();
        }
    }

    public class GlobalLevelResource : FileResource
    {
        public GlobalLevelResource(ModResource resource, string path, string fullPath) : base(resource, path, fullPath)
        {
        }


        public override void AssignType()
        {
            var path = Path;
            var filename = System.IO.Path.GetFileName(path);
            
            if (path.StartsWith("DarkWorld")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeDarkWorldTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Versus")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeVersusTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Quest")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeQuestTowerFolder);
                }
                else AssignLevelFile();
            }
            else if (path.StartsWith("Trials")) 
            {
                if (ResourceTree.IsExist(this, path + "/tower.xml")) 
                {
                    ResourceType = typeof(ResourceTypeTrialsTowerFolder);
                }
                else AssignLevelFile();
            }

            else if (path.EndsWith(".xml")) 
            {
                ResourceType = typeof(ResourceTypeXml);
            }
            else if (Childrens.Count != 0) 
            {
                ResourceType = typeof(ResourceTypeFolder);
            }
            else 
            {
                ResourceType = typeof(ResourceTypeFile);
            }

            void AssignLevelFile() 
            {
                if (path.EndsWith(".json")) 
                {
                    ResourceType = typeof(ResourceTypeJson);
                }
                else if (path.EndsWith(".oel")) 
                {
                    ResourceType = typeof(ResourceTypeOel);
                }
            }
        }
    }

    public abstract class ModResource : IDisposable
    {
        public ModuleMetadata Metadata;
        public FortContent Content;
        public Dictionary<string, Resource> Resources = new();
        private bool disposedValue;

        public ModResource(ModuleMetadata metadata) 
        {
            Metadata = metadata;
            Content = new FortContent(Metadata, this);
        }

        public ModResource(string path) 
        {
            Content = new FortContent(path, this);
        }


        public void Add(string path, Resource resource) 
        {
            var rootName = (Metadata is not null ? Metadata.Name : "::global::");
            var rootPath = resource.Root = $"mod:{rootName}/";

            Logger.Verbose("[RESOURCE] Loaded On:" + rootPath);
            Logger.Verbose("[RESOURCE] Loaded:" + path);
            if (Resources.ContainsKey(path)) 
                return;

            
            Resources.Add(path, resource);
            RiseCore.ResourceTree.TreeMap.Add($"{rootPath}{path}", resource);
        }


        public abstract void Lookup(string prefix);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeInternal();
                }

                disposedValue = true;
            }
        }

        internal virtual void DisposeInternal() {}

        ~ModResource()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class ZipModResource : ModResource
    {
        private ZipFile zipFile;


        public ZipModResource(ModuleMetadata metadata) : base(metadata)
        {
            zipFile = new ZipFile(metadata.PathZip);
        }

        internal override void DisposeInternal()
        {
            zipFile.Dispose();
        }


        public override void Lookup(string prefix) 
        {
            var folders = new Dictionary<string, ZipResource>();
            foreach (var entry in zipFile.Entries) 
            {
                var fileName = entry.FileName.Replace('\\', '/');

                if (entry.IsDirectory)  
                {
                    if (BlacklistedCommonFolders.Contains(fileName)) 
                        continue;
                    var file = fileName.Remove(fileName.Length - 1);
                    var zipResource = new ZipResource(this, file, prefix + file, entry);
                    Add(file, zipResource);
                    folders.Add(file, zipResource);
                    var split = file.Split(SplitSeparator);
                    Array.Resize(ref split, split.Length - 1);
                    var newPath = CombineAllPath(split);
                    if (folders.TryGetValue(newPath, out var resource)) 
                    {
                        resource.Childrens.Add(zipResource);
                    }
                }
                else 
                {
                    if (BlacklistedExtension.Contains(Path.GetExtension(fileName))) 
                        continue;
                    var zipResource = new ZipResource(this, fileName, prefix + fileName, entry);
                    Add(fileName, zipResource);
                    if (folders.TryGetValue(Path.GetDirectoryName(fileName).Replace('\\', '/'), out var resource)) 
                    {
                        resource.Childrens.Add(zipResource);
                    }
                }
            }
        }

        private static string CombineAllPath(string[] paths) 
        {
            var sb = new StringBuilder();
            for (int i = 0; i < paths.Length; i++) 
            {
                var path = paths[i];
                sb.Append(path);
                if (i != paths.Length - 1)
                    sb.Append('/');
            }
            return sb.ToString();
        }
    }

    public class AdventureGlobalLevelResource : FolderModResource
    {
        public AdventureGlobalLevelResource() : base(Path.Combine(Calc.LOADPATH, "Mod", "Adventure"))
        {
        }

        public override void Lookup(string prefix)
        {
            if (!Directory.Exists(FolderDirectory))
                Directory.CreateDirectory(FolderDirectory);
            var files = Directory.GetFiles(FolderDirectory);
            for (int i = 0; i < files.Length; i++) 
            {
                var filePath = files[i].Replace('\\', '/');
                if (BlacklistedExtension.Contains(Path.GetExtension(filePath))) 
                    continue;
                var simplifiedPath = filePath.Replace(FolderDirectory + '/', "");
                var fileResource = new GlobalLevelResource(this, simplifiedPath, filePath);
                Add(simplifiedPath, fileResource);
            }
            var folders = Directory.EnumerateDirectories(FolderDirectory).ToList();
            foreach (var folder in folders) 
            {
                var fixedFolder = folder.Replace('\\', '/');
                var simpliPath = fixedFolder.Replace(FolderDirectory + '/', "");
                var newFolderResource = new GlobalLevelResource(this, simpliPath, fixedFolder);
                Lookup(prefix, folder, FolderDirectory, newFolderResource);
                Add(fixedFolder, newFolderResource);
            }
        }

        public void Lookup(string prefix, string path, string modDirectory, GlobalLevelResource folderResource) 
        {
            var files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++) 
            {
                var filePath = files[i].Replace('\\', '/');
                if (BlacklistedExtension.Contains(Path.GetExtension(filePath))) 
                    continue;
                var simplifiedPath = filePath.Replace(modDirectory + '/', "");
                var fileResource = new GlobalLevelResource(this, simplifiedPath, filePath);
                Add(simplifiedPath, fileResource);
                folderResource.Childrens.Add(fileResource);
            }
            var folders = Directory.EnumerateDirectories(path).ToList();
            foreach (var folder in folders) 
            {
                var fixedFolder = folder.Replace('\\', '/');
                var simpliPath = fixedFolder.Replace(modDirectory + '/', "");
                var newFolderResource = new GlobalLevelResource(this, simpliPath, prefix + simpliPath);
                Lookup(prefix, folder, modDirectory, newFolderResource);
                Add(simpliPath, newFolderResource);
                folderResource.Childrens.Add(newFolderResource);
            }
        }
    }

    public class FolderModResource : ModResource
    {
        public string FolderDirectory;
        public FolderModResource(ModuleMetadata metadata) : base(metadata)
        {
            FolderDirectory = metadata.PathDirectory.Replace('\\', '/');
        }

        public FolderModResource(string path) : base(path)
        {
            FolderDirectory = path.Replace('\\', '/');
        }

        public override void Lookup(string prefix)
        {
            var files = Directory.GetFiles(FolderDirectory);
            for (int i = 0; i < files.Length; i++) 
            {
                var filePath = files[i].Replace('\\', '/');
                if (BlacklistedExtension.Contains(Path.GetExtension(filePath))) 
                    continue;
                var simplifiedPath = filePath.Replace(FolderDirectory + '/', "");
                var fileResource = new FileResource(this, simplifiedPath, filePath);
                Add(simplifiedPath, fileResource);
            }
            var folders = Directory.EnumerateDirectories(FolderDirectory).ToList();
            foreach (var folder in folders) 
            {
                var fixedFolder = folder.Replace('\\', '/');
                var simpliPath = fixedFolder.Replace(FolderDirectory + '/', "");
                if (BlacklistedCommonFolders.Contains(simpliPath)) 
                    continue;
                var newFolderResource = new FileResource(this, simpliPath, fixedFolder);
                Lookup(prefix, folder, FolderDirectory, newFolderResource);
                Add(fixedFolder, newFolderResource);
            }
        }

        public void Lookup(string prefix, string path, string modDirectory, FileResource folderResource) 
        {
            var files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++) 
            {
                var filePath = files[i].Replace('\\', '/');
                if (BlacklistedExtension.Contains(Path.GetExtension(filePath))) 
                    continue;
                var simplifiedPath = filePath.Replace(modDirectory + '/', "");
                var fileResource = new FileResource(this, simplifiedPath, filePath);
                Add(simplifiedPath, fileResource);
                folderResource.Childrens.Add(fileResource);
            }
            var folders = Directory.EnumerateDirectories(path).ToList();
            foreach (var folder in folders) 
            {
                var fixedFolder = folder.Replace('\\', '/');
                var simpliPath = fixedFolder.Replace(modDirectory + '/', "");
                if (BlacklistedCommonFolders.Contains(simpliPath)) 
                    continue;
                var newFolderResource = new FileResource(this, simpliPath, prefix + simpliPath);
                Lookup(prefix, folder, modDirectory, newFolderResource);
                Add(simpliPath, newFolderResource);
                folderResource.Childrens.Add(newFolderResource);
            }
        }
    }

    public static class ResourceTree 
    {
        public static Dictionary<string, Resource> TreeMap = new();
        public static List<ModResource> ModResources = new();


        public static void AddMod(ModuleMetadata metadata, ModResource resource) 
        {
            var name = (metadata is not null ? metadata.Name : "::global::");
            var prefixPath = $"mod:{name}/";

            if (TreeMap.ContainsKey(prefixPath)) 
            {
                Logger.Warning($"[RESOURCE] Conflicting mod asset name found: {prefixPath}");
                return;
            }
            resource.Lookup(prefixPath);

            Logger.Info($"[RESOURCE] Initializing {resource.Metadata} resources...");
            Initialize(resource);
            ModResources.Add(resource);
        }

        internal static void Initialize(ModResource resource) 
        {
            foreach (var res in resource.Resources.Values) 
            {
                res.AssignType();
            }
        }

        public static bool TryGetValue(string path, out RiseCore.Resource res) 
        {
            return TreeMap.TryGetValue(path, out res);
        }

        public static bool IsExist(string path) 
        {
            return TreeMap.ContainsKey(path);
        }

        public static bool IsExist(Resource resource, string path) 
        {
            return TreeMap.ContainsKey(resource.Root + path);
        }

        public static void LoopThroughModsContent(Action<FortContent> modsAction) 
        {
            foreach (var mod in ModResources) 
            {
                if (mod.Content == null)
                    continue;
                modsAction(mod.Content);
            }
        }

        public static async Task DumpAll() 
        {
            try 
            {
                if (!Directory.Exists("DUMP"))
                    Directory.CreateDirectory("DUMP");
                using var file = File.Create("DUMP/resourcedump.txt");
                using TextWriter tw = new StreamWriter(file);

                tw.WriteLine("FORTRISE RESOURCE DUMP");
                tw.WriteLine("VERSION 4.7.0.0");
                tw.WriteLine("==============================");
                foreach (var globalResource in TreeMap) 
                {
                    await tw.WriteLineAsync("Global File Path: " + globalResource.Key);
                    await tw.WriteLineAsync("Source: ");
                    await tw.WriteLineAsync("\t FullPath: " + globalResource.Value.FullPath);
                    await tw.WriteLineAsync("\t Path: " + globalResource.Value.Path);
                    await tw.WriteLineAsync("\t Root: " + globalResource.Value.Root);
                    await tw.WriteLineAsync("\t Type: " + globalResource.Value.ResourceType?.Name ?? "EmptyType");
                    await tw.WriteLineAsync("\t Childrens: ");
                    foreach (var child in globalResource.Value.Childrens) 
                    {
                        await DumpResource(child, "\t");
                    }
                }

                async Task DumpResource(Resource childResource, string line) 
                {
                    await tw.WriteLineAsync(line + "\t FullPath: " + childResource.FullPath);
                    await tw.WriteLineAsync(line + "\t Path: " + childResource.Path);
                    await tw.WriteLineAsync(line + "\t Root: " + childResource.Root);
                    await tw.WriteLineAsync(line + "\t Type: " + childResource.ResourceType?.Name ?? "EmptyType");
                    await tw.WriteLineAsync(line + "\t Childrens: ");
                    foreach (var resource in childResource.Childrens) 
                    {
                        await DumpResource(resource, line + "\t");
                    }
                }


                // Dump Atlases
                DumpAtlas("Atlas", TFGame.Atlas);
                DumpAtlas("MenuAtlas", TFGame.MenuAtlas);
                DumpAtlas("BGAtlas", TFGame.BGAtlas);
                if (GameData.DarkWorldDLC)
                    DumpAtlas("BossAtlas", TFGame.BossAtlas);
                }
            catch (Exception e) 
            {
                Logger.Error("[DUMPRESOURCE]" + e.ToString());
                throw;
            }
        }

        private static void DumpAtlas(string name, Atlas atlas) 
        {
            foreach (KeyValuePair<string, Subtexture> texturePair in atlas.SubTextures) 
            {
                string key = texturePair.Key;
                Subtexture value = texturePair.Value;

                using var texture = value.GetTexture2DFromSubtexture();
                var path = $"DUMP/{name}/{key}.png";
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                using var file = File.Create(path);
                texture.SaveAsPng(file, texture.Width, texture.Height);
            }
        }
    }
}