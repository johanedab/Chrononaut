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
    using static GameDatabase;

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false /*once*/)]
    public class Chrononaut : MonoBehaviour
    {
        public int version = 1;

        // Get the AvailablePart from a partName, since it will give additional
        // info that is not available in the Part class.
        // Not needed since Part.partInfo points to the correct AvailablePart.
        private AvailablePart GetAvailablePart(string partName)
        {
            // Make sure to throw away any extra text thrown in at the end of 
            // the string, like "(Spacecraft name)"
            partName = partName.Split(' ')[0];

            // Loop through the PartLoader list to search for the part name
            List< AvailablePart> availableParts = PartLoader.LoadedPartsList;
            foreach (AvailablePart availablePart in availableParts)
                if (partName == availablePart.name)
                    return availablePart;

            // No part found
            return null;
        }

        // Get the model file name for a part by reading the cfg file contents
        string GetModelFileName(Part part)
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
            string model = availablePart.partConfig.GetValue("mesh");
            if (model != null)
            {
                // The "url" is on the firmat "Parts/<PartType>/<ConfigFileName>/<PartName>
                // We need to strip away the ConfigFileName and PartName to get the real path
                string[] urlParts = availablePart.partUrl.Split('/');
                string url = "";
                for (int i = 0; i < urlParts.Count() - 2; i++)
                    url += urlParts[i] + "/";

                model = url + model;
            }
            else
                model = availablePart.partConfig.GetNode("MODEL").GetValue("model") + ".mu";
            
            return model;
        }

        // Print the list of textures for debugging purposes
        public void PrintTextures()
        {
            TextureInfo textureInfo;
            GameDatabase gdb = GameDatabase.Instance;
            Debug.Log("count: " + gdb.databaseTexture.Count);
            for(int i=0;i< gdb.databaseTexture.Count;i++)
            {
                textureInfo = gdb.databaseTexture[i];
                Debug.Log("tex: " + textureInfo.name + ", " + textureInfo.file);
            }
        }

        public void Update()
        {
            version = 2;
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("*** Chrononaut ***");

                // Retrieve the active vessel and its parts
                Vessel vessel = FlightGlobals.ActiveVessel;
                List<Part> parts = vessel.parts;

                // Loop through all parts of the vessel to update them
                foreach (Part part in parts)
                    UpdatePart(part);
            }
        }

        // Use reflection to get a method of a potentially hidden class
        object RunMethod(string assemblyName, string className, string methodName, object[] parameters)
        {
            // Start by getting the assembly.
            Assembly assembly = Assembly.LoadFile(assemblyName);

            // Get the assembly from the already loaded list instead of reading it from file
            /*
             * Not possible since assembly-csharp is not in the LoadedAssemblyList
            Assembly assembly = null;
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                Debug.Log("ass: " + loadedAssembly.name);
                if (loadedAssembly.name == assemblyName || loadedAssembly.name == "KSP")
                    assembly = loadedAssembly.assembly;
            }
            */

            if (assembly == null)
                return null;

            Type type = assembly.GetType(className);
            //Debug.LogError("type: " + type);

            MethodInfo methodInfo = type.GetMethod(methodName);
            //Debug.LogError("methodInfo: " + methodInfo);

            // Run the method
            return methodInfo.Invoke(type, parameters);
        }

        private void UpdatePart(Part part)
        {
            Debug.Log("Part: " + part.name);

            // Get the model name including the relative path to GameData
            string modelName = GetModelFileName(part);
            Debug.Log("  Model: " + modelName);

            // Calculate the proper URLs for the file
            string directory = Path.GetDirectoryName(modelName);
            FileInfo file = new FileInfo("GameData/" + modelName);

            // Generate the root directory
            UrlDir root = UrlBuilder.CreateDir(directory);

            // Create the full path and name of the model file
            UrlDir.UrlFile urlFile = new UrlDir.UrlFile(root, file);

            /*
             * TESTS
            // Reference test of how GetTexture works
            Texture2D texture;
            texture = GameDatabase.Instance.GetTexture(
                "RocketEmporium/Parts/Coupling/fairing",
                false);
            Debug.Log("tex1: " + texture);
            */

            /*
             * TESTS
             * Can we use the database loader instead of the part reader directly?
            DatabaseLoaderModel_MU databaseLoader = new DatabaseLoaderModel_MU();
            databaseLoader.Load(urlFile, file);
            Debug.Log("success: " + (databaseLoader.successful ? "yes" : "no"));                    
            GameObject obj = databaseLoader.obj;
            */

            // Access the internal PartReader.Read method by reflection.
            GameObject obj = (GameObject) RunMethod(
                //"Assembly-CSharp",
                "KSP_x64_Data/Managed/Assembly-CSharp.dll",
                "PartReader",
                "Read",
                new object[] { urlFile });

            // Reference implementation for being able to add debug messages
            //GameObject obj = ChronoReader.Read(urlFile);

            if (obj == null)
            {
                Debug.LogError("Chrononaut:UpdatePart(): Failed to read object");
                return;
            }

            // Destroy the object directly so it doesn't stay visible in the scene.
            // Seems it is still accessible tho and can be read.
            Destroy(obj);

            Debug.Log("  obj.name: " + obj.name);

            // Get all the MeshFilters in the source and the destination
            MeshFilter[] meshFiltersSource = obj.GetComponentsInChildren<MeshFilter>();
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

                /*
                 * TESTS
                Renderer[] componentsInChildren = meshFilterDest.GetComponentsInChildren<Renderer>();
                int k = 0;
                for (int num = componentsInChildren.Length; k < num; k++)
                {
                    Renderer renderer = componentsInChildren[k];
                    int l = 0;
                    for (int num2 = renderer.sharedMaterials.Length; l < num2; l++)
                        renderer.enabled = true;
                }
                */

                /*
                 * TESTS
                // Testing how we get textures in an object
                Texture[] textures = meshFilterDest.GetComponentsInChildren<Texture>();
                int k = 0;
                for (int num = textures.Length; k < num; k++)
                {
                    Texture texture = textures[k];
                    int l = 0;
                    Debug.Log("    Texture: " + texture.name);
                }
                */
            }
        }
    }
}