using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static GameDatabase;

namespace Chrononaut
{
    static class Loader 
    {
        private static List<DatabaseLoader<GameDatabase.TextureInfo>> textureLoaders;
        private static List<UrlDir.ConfigFileType> textureTypes;

        public static void Init()
        {
            if (textureLoaders == null)
                textureLoaders = GetTextureLoaders();

            if (textureTypes == null)
                textureTypes = GetTextureTypes();
        }

        // Print the list of textures for debugging purposes
        public static void PrintTextures(bool includeSquad)
        {
            TextureInfo textureInfo;
            GameDatabase gdb = GameDatabase.Instance;
            Debug.Log("Textures: " + gdb.databaseTexture.Count);
            for (int i = 0; i < gdb.databaseTexture.Count; i++)
            {
                textureInfo = gdb.databaseTexture[i];
                if(!includeSquad)
                    if (textureInfo.name.IndexOf("Squad") != -1)
                        continue;

                Debug.Log("+tex: " + textureInfo.name);
                Debug.Log("  tex.isReadable: " + textureInfo.isReadable);
                Debug.Log("  tex.texture: " + (textureInfo.texture ? textureInfo.texture.name : "null"));
                if (textureInfo.texture)
                {
                    Debug.Log("  tex.GetPixel(0,0): " + textureInfo.texture.GetPixel(0, 0));
                    Debug.Log("  tex.size: " + textureInfo.texture.width + "*" + textureInfo.texture.height);
                }
            }
        }

        public static List<DatabaseLoader<GameDatabase.TextureInfo>> GetTextureLoaders()
        {
            List<DatabaseLoader<GameDatabase.TextureInfo>> loaders = new List<DatabaseLoader<GameDatabase.TextureInfo>>();
            //loadersAudio = new List<DatabaseLoader<AudioClip>>();
            //loadersModel = new List<DatabaseLoader<GameObject>>();

            AssemblyLoader.loadedAssemblies.TypeOperation(delegate (Type t)
            {
                if (t.IsSubclassOf(typeof(DatabaseLoader<GameDatabase.TextureInfo>)))
                {
                    loaders.Add((DatabaseLoader<GameDatabase.TextureInfo>)Activator.CreateInstance(t));
                    return;
                }
                /*
                if (t.IsSubclassOf(typeof(DatabaseLoader<AudioClip>)))
                {
                    this.loadersAudio.Add((DatabaseLoader<AudioClip>)Activator.CreateInstance(t));
                    return;
                }
                if (t.IsSubclassOf(typeof(DatabaseLoader<GameObject>)))
                {
                    this.loadersModel.Add((DatabaseLoader<GameObject>)Activator.CreateInstance(t));
                    return;
                }
                */
            });

            return loaders;
        }

        public static List<UrlDir.ConfigFileType> GetTextureTypes()
        {
            if (textureLoaders == null)
            {
                Debug.LogError("Loader.GetTextureTypes: Call Loader.Init first");
                return null;
            }
            
            List<UrlDir.ConfigFileType> list = new List<UrlDir.ConfigFileType>();

            UrlDir.ConfigFileType textureFiles = new UrlDir.ConfigFileType(UrlDir.FileType.Texture);
            list.Add(textureFiles);
            foreach (DatabaseLoader<GameDatabase.TextureInfo> loader in textureLoaders)
                textureFiles.extensions.AddRange(loader.extensions);

            /*
            UrlDir.ConfigFileType configFileType = new UrlDir.ConfigFileType(UrlDir.FileType.Assembly);
            list.Add(configFileType);
            configFileType.extensions.Add("dll");
            UrlDir.ConfigFileType configFileType2 = new UrlDir.ConfigFileType(UrlDir.FileType.Audio);
            list.Add(configFileType2);
            foreach (DatabaseLoader<AudioClip> databaseLoader in this.loadersAudio)
            {
                configFileType2.extensions.AddRange(databaseLoader.extensions);
            }
            UrlDir.ConfigFileType configFileType4 = new UrlDir.ConfigFileType(UrlDir.FileType.Model);
            list.Add(configFileType4);
            foreach (DatabaseLoader<GameObject> databaseLoader3 in this.loadersModel)
            {
                configFileType4.extensions.AddRange(databaseLoader3.extensions);
            }
            */

            return list;
        }

        public static UrlDir GetRoot()
        {
            if (textureLoaders == null)
            {
                Debug.LogError("Loader.GetRoot: Call Loader.Init first");
                return null;
            }

            List<UrlDir.ConfigDirectory> urlConfig = new List<UrlDir.ConfigDirectory>();
            urlConfig.Add(new UrlDir.ConfigDirectory("", "GameData", UrlDir.DirectoryType.GameData));

            return new UrlDir(urlConfig.ToArray(), textureTypes.ToArray());
        }

        /*
        public static Texture GetTexture(string url, bool asNormalMap)
        {
            List<TextureInfo> databaseTexture = GameDatabase.Instance.databaseTexture;

            foreach (TextureInfo textureInfo in databaseTexture)
            {
                //if (textureInfo.file.fullPath.IndexOf("Squad") == -1)
                //    Debug.Log("- textureInfo: " + textureInfo.name);

                if (textureInfo != null && string.Equals(textureInfo.name, url, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (asNormalMap)
                    return textureInfo.normalMap;
                else
                    return textureInfo.texture;
            }

            return null;
        }
        */

        public static void ReloadTextures(DateTime lastLoadTime)
        {
            if (textureLoaders == null)
            {
                Debug.LogError("Loader.ReloadTextures: Call Loader.Init first");
                return;
            }

            /*
            PartLoader.Instance.Recompile = true;
            AssemblyLoader.ClearPlugins();
            foreach (UrlDir.UrlFile urlFile in GameDatabase.Instance.root.GetFiles(UrlDir.FileType.Assembly))
            {
                UnityEngine.Debug.Log("Load(Assembly): " + urlFile.url);
                ConfigNode assemblyNode = null;
                List<UrlDir.UrlConfig> list = new List<UrlDir.UrlConfig>(urlFile.parent.GetConfigs("ASSEMBLY", urlFile.name, false));
                if (list.Count > 0)
                {
                    assemblyNode = list[0].config;
                }
                AssemblyLoader.LoadPlugin(new FileInfo(urlFile.fullPath), urlFile.parent.url, assemblyNode);
            }
            AssemblyLoader.LoadAssemblies();
            VesselModuleManager.CompileModules();

            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (!string.IsNullOrEmpty(loadedAssembly.assembly.Location))
                {
                    AssemblyName name = loadedAssembly.assembly.GetName();
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(loadedAssembly.assembly.Location);
                    string text2 = string.Concat(new object[]
                    {
                name.Name,
                " v",
                name.Version,
                (!(versionInfo.ProductVersion != " ") || !(versionInfo.ProductVersion != name.Version.ToString())) ? string.Empty : (" / v" + versionInfo.ProductVersion),
                (!(versionInfo.FileVersion != " ") || !(versionInfo.FileVersion != name.Version.ToString()) || !(versionInfo.FileVersion != versionInfo.ProductVersion)) ? string.Empty : (" / v" + versionInfo.FileVersion)
                    });
                    if (this.assemblyWhitelist.Contains(name.Name))
                    {
                        text2 = "Stock assembly: " + text2;
                    }
                    else
                    {
                        GameDatabase.modded = true;
                        GameDatabase.loaderInfo.Add(text2);
                        GameDatabase.loadedModsInfo.Add(text2);
                    }
                    modlist = modlist + text2 + "\n";
                }
            }
            AddonLoader.Instance.StartAddons(KSPAddon.Startup.Instantly);
            */
            /*
            foreach (UrlDir.UrlFile file in root.GetFiles(UrlDir.FileType.Audio))
            {
                if (db.ExistsAudioClip(file.url))
                {
                    UnityEngine.Debug.Log("Load(Audio): " + file.url + " OUT OF DATE");
                    PartLoader.Instance.Recompile = true;
                }
                UnityEngine.Debug.Log("Load(Audio): " + file.url);
                foreach (DatabaseLoader<AudioClip> loader in this.loadersAudio)
                {
                    if (loader.extensions.Contains(file.fileExtension))
                    {
                        if (loadAudio)
                        {
                            yield return base.StartCoroutine(loader.Load(file, new FileInfo(file.fullPath)));
                        }
                        if (loader.successful)
                        {
                            loader.obj.name = file.url;
                            this.databaseAudio.Add(loader.obj);
                            this.databaseAudioFiles.Add(file);
                        }
                    }
                    if (Time.realtimeSinceStartup > nextFrameTime)
                    {
                        nextFrameTime = Time.realtimeSinceStartup + LoadingScreen.minFrameTime;
                        yield return null;
                    }
                }
                this.progressFraction += delta;
            }
            */

            GameDatabase db = GameDatabase.Instance;

            UrlDir root = GetRoot();

            List<TextureInfo> textures = db.databaseTexture;

            // PrintTextures(false);

            foreach (UrlDir.UrlFile textureFile in root.GetFiles(UrlDir.FileType.Texture))
            {
                if (textureFile.fullPath.IndexOf("Squad") != -1)
                    continue;

                if (textureFile.fileTime < lastLoadTime)
                    continue;

                /*
                if (!db.ExistsTexture(textureFile.url))
                {
                    Debug.LogError("Load texture: " + textureFile.url + " doesn's exist");
                    continue;
                }
                */
                // Find the correct texture loader for this texture
                foreach (DatabaseLoader<GameDatabase.TextureInfo> textureLoader in textureLoaders)
                {
                    if (!textureLoader.extensions.Contains(textureFile.fileExtension))
                        continue;

                    Debug.Log("  Loading texture " + textureFile.url + " using loader " + textureLoader.ToString());

                    // Load the texture and wait for the coroutine to finish. 
                    db.StartCoroutine(textureLoader.Load(textureFile, new FileInfo(textureFile.fullPath)));

                    if (textureLoader.successful)
                    {
                        //textureLoader.obj.name = textureFile.url;
                        textureLoader.obj.texture.name = textureFile.url;

                        foreach(TextureInfo existingTexture in textures)
                        {
                            //if (existingTexture.file.fullPath.IndexOf("Squad") != -1)
                            //    continue;

                            // Update an existing texture
                            if (existingTexture.file.url == textureFile.url)
                                existingTexture.texture = textureLoader.obj.texture;
                        }
                    }
                    else
                        Debug.LogError("Failed to load texture: " + textureFile.name);
                }
            }

            /*
            foreach (UrlDir.UrlFile file3 in root.GetFiles(UrlDir.FileType.Model))
            {
                if (db.ExistsModel(file3.url))
                {
                    if (!(file3.fileTime > this.lastLoadTime))
                    {
                        continue;
                    }
                    UnityEngine.Debug.Log("Load(Model): " + file3.url + " OUT OF DATE");
                    db.RemoveModel(file3.url);
                }
                UnityEngine.Debug.Log("Load(Model): " + file3.url);
                foreach (DatabaseLoader<GameObject> loader3 in db.loadersModel)
                {
                    if (loader3.extensions.Contains(file3.fileExtension))
                    {
                        if (loadParts)
                        {
                            yield return base.StartCoroutine(loader3.Load(file3, new FileInfo(file3.fullPath)));
                        }
                        if (loader3.successful)
                        {
                            GameObject obj = loader3.obj;
                            obj.transform.name = file3.url;
                            obj.transform.parent = base.transform;
                            obj.transform.localPosition = Vector3.zero;
                            obj.transform.localRotation = Quaternion.identity;
                            obj.SetActive(false);
                            this.databaseModel.Add(obj);
                            this.databaseModelFiles.Add(file3);
                        }
                    }
                    if (Time.realtimeSinceStartup > nextFrameTime)
                    {
                        nextFrameTime = Time.realtimeSinceStartup + LoadingScreen.minFrameTime;
                        yield return null;
                    }
                }
            }
            */
        }

        // Load the model file requested from file and return it as a GameObject.
        public static GameObject LoadModelFile(UrlDir.UrlFile modelFile)
        {
            GameObject loadedObject = null;
            GameDatabase db = GameDatabase.Instance;

            /*
            // Method 1: Access the internal PartReader.Read method by reflection to read it.
            loadedObject = (GameObject)RunMethod(
                "KSP_x64_Data/Managed/Assembly-CSharp.dll",
                "PartReader",
                "Read",
                new object[] { modelFile });

            // Method 2: Alternative reference implementation of PartReader for being able to add debug messages
            loadedObject = ChronoReader.Read(urlFile);
            */

            // Method 3: Use the database loader instead of the part reader directly
            DatabaseLoaderModel_MU databaseLoader = new DatabaseLoaderModel_MU();
            db.StartCoroutine(databaseLoader.Load(modelFile, null));
            loadedObject = databaseLoader.obj;

            if (loadedObject == null)
            {
                Debug.LogError("Chrononaut:LoadModelFile(): Failed to read file: " + modelFile.name);
                return null;
            }
            return loadedObject;
        }

    }
}
