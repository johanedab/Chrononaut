using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using PartToolsLib;


namespace Chrononaut
{
    // ChronoReader
    using PartToolsLib;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Rendering;
    using static GameDatabase;

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false /*once*/)]
    public class Chrononaut : MonoBehaviour
    {
        public int version = 1;

        /*
        private UrlDir.UrlFile GetFile(string model_name)
        {
            List<UrlDir.UrlFile> models = GameDatabase.Instance.databaseModelFiles;
            foreach (UrlDir.UrlFile model in models)
            {
                if(model.name = )
                Debug.Log("model: " + model.name + ", " + model.fullPath);
            }
        }
        */

        private AvailablePart GetAvailablePart(string partName)
        {
            Debug.Log("partName: " + partName);
            // Make sure to throw away any extra text thrown in at the end of the string
            partName = partName.Split(' ')[0];
            Debug.Log("partName: " + partName);

            List< AvailablePart> availableParts = PartLoader.LoadedPartsList;
            foreach (AvailablePart availablePart in availableParts)
                if (partName == availablePart.name)
                    return availablePart;

            return null;
        }

        string GetModelFileName_FromTransform(Part part)
        {
            // Get the transform of the part
            Transform tPart = part.transform;

            // Get the "model" transform that each part has
            Transform tModel = tPart.Find("model");

            // Get the empty game object of the model
            Transform tEmpty = tModel.GetChild(0);

            // Remove the "(clone)" part
            string modelName = tEmpty.name.Split('(')[0];

            return modelName;
        }

        string GetModelFileName_FromModel(Part part)
        {
            AvailablePart availablePart = GetAvailablePart(part.name);

            /*
            Debug.Log("AvailPart: " + availablePart.name);
            Debug.Log("model: " + availablePart.partConfig.GetValue("model"));
            Debug.Log("MODEL: " + availablePart.partConfig.GetNode("MODEL").GetValue("model"));
            */

            // Check for an entry in either the PART.model property or the PART.MODEL.model value
            string model = availablePart.partConfig.GetValue("model");
            if(model == null)
                model = availablePart.partConfig.GetNode("MODEL").GetValue("model");

            return model;
        }

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
        /*
        public class DatabaseLoaderModel_MU : DatabaseLoader<GameObject>
        {
            public override IEnumerator Load(UrlDir.UrlFile urlFile, FileInfo file)
            {
                GameObject gameObject = PartReader.Read(urlFile);
                if ((UnityEngine.Object)gameObject != (UnityEngine.Object)null)
                {
                    base.obj = gameObject;
                    base.successful = true;
                }
                else
                {
                    Debug.LogWarning("Model load error in '" + file.FullName + "'");
                    base.obj = null;
                    base.successful = false;
                }
                yield break;
            }
        }
        */

        public void Update()
        {
            version = 2;
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("*** Chrononaut ***");

                PrintTextures();

                Vessel vessel = FlightGlobals.ActiveVessel;
                Debug.Log("Name: " + vessel.name);
                List<Part> parts = vessel.parts;
                Debug.Log("Parts: " + parts.Count);

                foreach (Part part in parts)
                {
                    Debug.Log("Part: " + part.name);

                    //part.transform

                    string nameT = GetModelFileName_FromTransform(part);
                    string nameM = GetModelFileName_FromModel(part);

                    Debug.Log("nameT: " + nameT);
                    Debug.Log("nameM: " + nameM);

                    string fileName = "GameData/" + nameT + ".mu";
                    string directory = Path.GetDirectoryName(nameT);
                    FileInfo file = new FileInfo(fileName);

                    UrlDir root = UrlBuilder.CreateDir(directory);
                    UrlDir.UrlFile urlFile = new UrlDir.UrlFile(root, file);

                    /*
                    Texture2D texture;
                    texture = GameDatabase.Instance.GetTexture(
                        "RocketEmporium/Parts/Coupling/fairing",
                        false);
                    Debug.Log("tex1: " + texture);
                    */

                    /*
                    DatabaseLoader<GameObject> databaseLoader = new DatabaseLoader<GameObject>();
                    DatabaseLoaderModel_MU databaseLoader = new DatabaseLoaderModel_MU();
                    databaseLoader.Load(urlFile, file);
                    Debug.Log("success: " + (databaseLoader.successful ? "yes" : "no"));
                    GameObject obj = databaseLoader.obj;
                    */

                    GameObject obj = ChronoReader.Read(urlFile);

                    // Destroy the object directly so it doesn't stay visible in the scene.
                    // Seems it is still accessible tho and can be read
                    Destroy(obj);

                    Debug.Log("obj: " + obj);
                    Debug.Log("obj.name: " + obj.name);
                    MeshFilter[] meshFiltersSource = obj.GetComponentsInChildren<MeshFilter>();
                    
                    List<MeshFilter> meshFiltersDest = part.FindModelComponents<MeshFilter>();

                    Debug.Log("part.transform: ", part.transform);
                    Debug.Log("obj.transform: ", obj.transform);

                    //foreach (MeshFilter mesh_filter in meshFiltersSource)
                    //{

                    int i = 0;
                    foreach(MeshFilter meshFilterDest in meshFiltersDest)
                    {
                        MeshFilter meshFilter = meshFiltersSource[i++];
                        Mesh meshSource = meshFilter.mesh;
                        Debug.Log(" source: " + meshFilter.name);
                        Debug.Log(" dest was: " + meshFilterDest.name);

                        Destroy(meshFilterDest.mesh);
                        meshFilterDest.mesh = meshSource;
                        /*
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
                    Debug.Log(" src transform: " + meshFilter.transform.localScale);
                        Debug.Log(" dest transform: " + meshFilterDest.transform.localScale);
                        Debug.Log(" src parent: " + meshFilter.transform.parent);
                        Debug.Log(" dest parent: " + meshFilterDest.transform.parent);
                        meshFilterDest.transform.localEulerAngles = meshFilter.transform.localEulerAngles;
                        meshFilterDest.transform.localScale = meshFilter.transform.localScale;
                        meshFilterDest.transform.localRotation = meshFilter.transform.localRotation;
                        meshFilterDest.transform.localPosition = meshFilter.transform.localPosition;
                    }
                    return;
                }
            }
        }

        /// <summary>Handles vessel selection logic.</summary>
         /*
        void HandleVesselSelection()
        {
            // Highlight focused vessel.
            if (Mouse.HoveredPart)
            {
                SetHoveredVessel(Mouse.HoveredPart.vessel);
            }
            else
            {
                SetHoveredVessel(null);  // Cancel highlight.
            }

            // Select vessel if clicked.
            if (Mouse.GetAllMouseButtonsDown() == switchMouseButton
                && hoveredVessel != null && !hoveredVessel.isActiveVessel)
            {
                if (hoveredVessel.DiscoveryInfo.Level != DiscoveryLevels.Owned)
                {
                    // Cannot switch to unowned vessel. Invoke standard "soft" switch to have error message
                    // triggered.
                    FlightGlobals.SetActiveVessel(hoveredVessel);
                }
                else
                {
                    // Use forced version since "soft" switch blocks on many normal situations (e.g. "on
                    // ladder" or "in atmosphere").
                    var vesselToSelect = hoveredVessel;  // Save hovered vessel as it'll be reset on blur. 
                    SetHoveredVessel(null);
                    evsSwitchAction = true;
                    FlightGlobals.ForceSetActiveVessel(vesselToSelect);
                }
            }
        }
            */

    }

}