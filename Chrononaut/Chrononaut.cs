// #define CHRONO_DEBUG

/*
 * Tools used to make modding faster
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using PartToolsLib;

namespace Chrononaut
{
    using PartToolsLib;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.UI;
    using static AssemblyLoader;
    using static GameDatabase;

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false /*once*/)]
    public class Chrononaut : MonoBehaviour
    {
        bool configLoaded;
        private DateTime lastLoadTime;

        [Persistent] public string keyReloadVessel = "f8";

        private void LoadConfig()
        {
            Debug.Log("*** Chrononaut_v0.3.0:LoadConfig ***");

            // Load the settings file
            ConfigNode settings = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Chrononaut/Settings.cfg");
            if (settings == null)
            {
                Debug.LogError("Chrononaut:LoadConfig(): Failed to open settings file");
                return;
            }

            Debug.Log("settings: " + settings);

            // Load the settings into this class
            ConfigNode.LoadObjectFromConfig(this, settings);

            // Init the loader to reduce time later
            Loader.Init();

            // Try to use the key reference. If it doesn't work, an exception 
            // will be thrown and configLoaded will not be set
            Input.GetKeyDown(keyReloadVessel);
            configLoaded = true;
        }

        private void Awake()
        {
            lastLoadTime = DateTime.Now;

            LoadConfig();
        }

        // Get the AvailablePart from a partName, since it will give additional
        // info that is not available in the Part class.
        // Not needed since Part.partInfo points to the correct AvailablePart.
        private AvailablePart GetAvailablePart(string partName)
        {
            // Make sure to throw away any extra text thrown in at the end of 
            // the string, like "(Spacecraft name)"
            partName = partName.Split(' ')[0];

            // Loop through the PartLoader list to search for the part name
            List<AvailablePart> availableParts = PartLoader.LoadedPartsList;
            foreach (AvailablePart availablePart in availableParts)
                if (partName == availablePart.name)
                    return availablePart;

            // No part found
            return null;
        }

        // Get the model file name for a part by reading the cfg file contents
        UrlDir.UrlFile GetModelFile(Part part)
        {
            AvailablePart availablePart = part.partInfo;

            /*
             * TESTS
            Debug.Log("AvailPart: " + availablePart.name);
            Debug.Log("mesh: " + availablePart.partConfig.GetValue("mesh"));
            Debug.Log("MODEL: " + 
                   (availablePart.partConfig.GetNode("MODEL") != null ?
                   availablePart.partConfig.GetNode("MODEL").GetValue("model") :
                   "none"));
            */

            // Check for an entry in either the PART.model property or the PART.MODEL.model value
            string modelName = availablePart.partConfig.GetValue("mesh");
            if (modelName != null)
            {
                // The "url" is on the firmat "Parts/<PartType>/<ConfigFileName>/<PartName>
                // We need to strip away the ConfigFileName and PartName to get the real path
                string[] urlParts = availablePart.partUrl.Split('/');
                string url = "";
                for (int i = 0; i < urlParts.Count() - 2; i++)
                    url += urlParts[i] + "/";

                modelName = url + modelName;
            }
            else
                modelName = availablePart.partConfig.GetNode("MODEL").GetValue("model") + ".mu";

            // Calculate the proper URLs for the file
            string directory = Path.GetDirectoryName(modelName);
            FileInfo file = new FileInfo("GameData/" + modelName);

            // Generate the root directory
            UrlDir root = UrlBuilder.CreateDir(directory);

            UrlDir.UrlFile modelFile = new UrlDir.UrlFile(root, file);

            // Apperently, if the file is name "model.mu" the reader is supposed to just
            // pick the first file in the folder... =S
            if (!File.Exists("GameData/" + modelName))
            {
                IEnumerable<UrlDir.UrlFile> modelFiles = root.GetFiles(UrlDir.FileType.Model);

                foreach(UrlDir.UrlFile mFile in modelFiles)
                    Debug.Log("modelFiles: " + mFile.name);
            }

            // Create the full path and name of the model file
            return modelFile;
        }


        /*
                public void Load(UrlDir.UrlFile urlFile, FileInfo file)
                {
                    Debug.Log("Loading: " + urlFile.name + ", " + urlFile.fullPath);
                    GameObject gameObject = ChronoReader.Read(urlFile);
                    if ((UnityEngine.Object)gameObject != (UnityEngine.Object)null)
                    {
                        obj = gameObject;
                        successful = true;
                    }
                    else
                    {
                        Debug.LogWarning("Model load error in '" + file.FullName + "'");
                        obj = null;
                        successful = false;
                    }
                    //yield break;
                }
                */

        // Called frequently in the flight scene and the VAB editor
        public void Update()
        {
            // Make sure to bail out if the config is correctly loaded
            if (!configLoaded)
                return;

            // React to key presses from the user
            if (Input.GetKeyDown(keyReloadVessel))
            {
                Debug.Log("*** Chrononaut_v0.3.0:UpdateVessel ***");

                // GameDatabase.Instance.StartCoroutine("LoadObjects");

                Loader.ReloadTextures(lastLoadTime);
                lastLoadTime = DateTime.Now;

                UpdateParts();
            }
        }

        // Loop through all the parts to update them
        public void UpdateParts()
        {
            // Retrieve the active vessel and its parts
            Vessel vessel = FlightGlobals.ActiveVessel;
            List<Part> parts = null;

            if (vessel)
                parts = vessel.parts;
            else
                parts = EditorLogic.fetch.ship.parts;

            // Loop through all parts of the vessel to update them
            foreach (Part part in parts)
            {
                Debug.Log("Updating part: " + part.name);

                // Get the model name including the relative path to GameData
                UrlDir.UrlFile modelFile = GetModelFile(part);

                // Load the object from file
                GameObject loadedObject = Loader.LoadModelFile(modelFile);

                if (!loadedObject)
                    continue;

                // Debug.Log("  LoadedObject: " + loadedObject.name);

                // Perform the actual update of the part
                UpdatePart(part, loadedObject);
                // UpdateTextures(part);
            }
        }

        private void UpdatePart(Part part, GameObject loadedObject)
        {
            Transform model = part.transform.GetChild(0);

            // Check that we really found the expected model transform
            if (!model || model.Equals(null) || model.name!="model")
            {
                Debug.LogError(string.Format("Transform 'model' not found! ({0})", model ? model.name : "null"));
                return;
            }

#if CHRONO_DEBUG
            Debug.Log(ChronoDebug.DumpPartHierarchy(loadedObject));
            Debug.Log(ChronoDebug.DumpPartHierarchy(part.gameObject)); // transformDest.gameObject
#endif

            // Search for the transform containing the model
            //Transform previousTransform = null;
            for (int i = 0; i < model.transform.childCount; i++)
            {
                Transform child = model.transform.GetChild(i);

                // The "model" transform contains some special transforms apart from the actual model.
                switch (child.gameObject.name)
                {
                    case "Surface Attach Collider":
                        break;
                    default:
                        // Destroy all other objects in the model transform
                        // child.parent = null;
                        Destroy(child.gameObject);
                        //Debug.Log("Destroying object: " + child.gameObject.name);
                        break;
                }
            }

            // Set the parent of the new transform to the 'model' transform to make the former a child
            // of the latter
            Transform loadedTransform = loadedObject.transform;
            loadedTransform.parent = model;

            // Update the location and rotation the object, by copying them from the previous model
            loadedTransform.position = model.position;
            loadedTransform.rotation = model.rotation;

            // Figure out the scaling
            AvailablePart availablePart = part.partInfo;

            // Default scaling for parts that don't have a rescaleFactor
            float rescaleFactor = 1.25f;

            // Override default value if available in the config
            if (availablePart.partConfig.HasValue("rescaleFactor"))
                rescaleFactor = float.Parse(availablePart.partConfig.GetValue("rescaleFactor"));

            // Apply the rescaleFactor
            loadedTransform.localScale *= rescaleFactor;
        }

        // This function is deprecated, use CopyComplete
        public void CopyMeshes(Part part, GameObject loadedObject)
        {
            // Destroy the object directly so it doesn't stay visible in the scene.
            // It will still be accessible to read from during this function
            Destroy(loadedObject);

            // Get all the MeshFilters in the source and the destination
            MeshFilter[] meshFiltersSource = loadedObject.GetComponentsInChildren<MeshFilter>();
            List<MeshFilter> meshFiltersDest = part.FindModelComponents<MeshFilter>();

            // Loop through the destination meshes and replace them from the source.
            // This should be replaced by updating the complete structure instead.

            int i = 0;
            foreach (MeshFilter meshFilterDest in meshFiltersDest)
            {
                // Get the Mesh from the MeshFilter
                MeshFilter meshFilterSource = meshFiltersSource[i++];
                Mesh meshSource = meshFilterSource.mesh;
                // Debug.Log("Updating mesh: " + meshFilterDest.name);

                // Throw away the old mesh and assign the new
                Destroy(meshFilterDest.mesh);
                meshFilterDest.mesh = meshSource;

                // Update the location of the mesh
                meshFilterDest.transform.localEulerAngles = meshFilterSource.transform.localEulerAngles;
                meshFilterDest.transform.localScale = meshFilterSource.transform.localScale;
                meshFilterDest.transform.localRotation = meshFilterSource.transform.localRotation;
                meshFilterDest.transform.localPosition = meshFilterSource.transform.localPosition;
            }
        }

        // Loop through the textures in the part and make sure they are updated
        public void UpdateTextures(Part part)
        {
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material material = renderer.material;
                Texture2D texture = (Texture2D)material.mainTexture;
                //Debug.Log("    Renderer: " + renderer.name + ", " + (texture ? texture.name : "null"));

                if (!texture)
                    continue;

                // Kludge: there are some rendering effects that have a texture name which is not a full path.
                // If the name doesn't contain a full path, we must disregard it, or GetTexture will fail.
                if (texture.name.IndexOf("/") == -1)
                    continue;

                texture = GameDatabase.Instance.GetTexture(texture.name, false);
                material.mainTexture = texture;
                // Debug.Log("      texture: " + texture.name + " (" + texture.width + "*" + texture.height + ")");
            }
        }
    }
}