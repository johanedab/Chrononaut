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

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false /*once*/)]
    public class TextureReloader : MonoBehaviour
    {
        /*
        public static GameObject Read(UrlDir.UrlFile file)
        {
            BinaryReader binaryReader = new BinaryReader(File.Open(file.fullPath, FileMode.Open));
            if (binaryReader == null)
            {
                Debug.Log("File error");
                return null;
            }
            //List<MaterialDummy> matDummies = new List<MaterialDummy>();
            //ChronoReader.boneDummies = new List<BonesDummy>();
            //ChronoReader.textureDummies = new TextureDummyList();
            FileType fileType = (FileType)binaryReader.ReadInt32();
            int fileVersion = binaryReader.ReadInt32();
            string str = binaryReader.ReadString();
            str += string.Empty;
            if (fileType != FileType.ModelBinary)
            {
                Debug.LogWarning("File '" + file.fullPath + "' is an incorrect type.");
                binaryReader.Close();
                return null;
            }
            GameObject gameObject = null;
            try
            {
                gameObject = ChronoReader.ReadChild(binaryReader, null);
                if (ChronoReader.boneDummies != null && ChronoReader.boneDummies.Count > 0)
                {
                    int i = 0;
                    for (int count = ChronoReader.boneDummies.Count; i < count; i++)
                    {
                        Transform[] array = new Transform[ChronoReader.boneDummies[i].bones.Count];
                        int j = 0;
                        for (int count2 = ChronoReader.boneDummies[i].bones.Count; j < count2; j++)
                        {
                            array[j] = ChronoReader.FindChildByName(gameObject.transform, ChronoReader.boneDummies[i].bones[j]);
                        }
                        ChronoReader.boneDummies[i].smr.bones = array;
                    }
                }
                if (ChronoReader.shaderFallback)
                {
                    Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
                    int k = 0;
                    for (int num = componentsInChildren.Length; k < num; k++)
                    {
                        Renderer renderer = componentsInChildren[k];
                        int l = 0;
                        for (int num2 = renderer.sharedMaterials.Length; l < num2; l++)
                        {
                            Material material = renderer.sharedMaterials[l];
                            material.shader = Shader.Find("KSP/Diffuse");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("File error:\n" + ex.Message + "\n" + ex.StackTrace);
            }
            ChronoReader.boneDummies = null;
            ChronoReader.matDummies = null;
            ChronoReader.textureDummies = null;
            binaryReader.Close();
            return gameObject;
        }
        */
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("F8");

                //obj.transform = new Vector3(1, 2, 3);


                UrlDir root = new UrlDir(new UrlDir.ConfigDirectory[0], new UrlDir.ConfigFileType[0]);

                //string url = "GameData/RocketEmporium/Parts/Resources/Coupling/decoupler_interstage_base_0_625";
                /*
                GameDatabase gdb = GameDatabase.Instance;
                int num = 0;
                GameObject obj = null;
                while (num < gdb.databaseModel.Count)
                {
                    obj = gdb.databaseModel[num];
                    FileInfo file = new FileInfo("GameData/" + obj.name + ".mu");
                    Debug.Log("prefab: " + file.FullName);
                    UrlDir.UrlFile urlFile = new UrlDir.UrlFile(root, file);
                    ChronoReader.Read(obj, urlFile);
                    num++;
                }

                */

                Debug.Log("FileInfo");
                FileInfo file = new FileInfo("GameData/RocketEmporium/Parts/Coupling/decoupler_interstage_base_0_625.mu");
                Debug.Log("FileInfo: " + file);
                Debug.Log("UrlFile");
                UrlDir.UrlFile urlFile = new UrlDir.UrlFile(root, file);
                Debug.Log("UrlFile:" + urlFile);
                Debug.Log("Read");
                GameObject obj = ChronoReader.Read(urlFile);

                Debug.Log("obj: " + obj);
                Debug.Log("obj.name: " + obj.name);

                Vessel vessel = FlightGlobals.ActiveVessel;
                Debug.Log("Name: " + vessel.name);
                List<Part> parts = vessel.parts;
                Debug.Log("Parts: " + parts.Count);

                MeshFilter[] meshFiltersSource = obj.GetComponentsInChildren<MeshFilter>();

                Debug.Log("  Mesh filter name: " + meshFiltersSource[0].name);
                Mesh mesh = meshFiltersSource[0].mesh;
                Debug.Log("  Mesh name: " + mesh.name);

                /*
                List<AvailablePart> availableParts = ChronoReader.LoadedPartsList;

                foreach (AvailablePart availablePart in availableParts)
                {
                    Debug.Log("availablePart: " + availablePart.name);
                }
                */
                Destroy(obj);
                foreach (Part part in parts)
                {
                    Debug.Log("Part: " + part.name);

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
                        Debug.Log(" source: " + meshSource.name);


                        Debug.Log(" dest was: " + meshFilterDest.name);
                        Debug.Log(" dest was: " + meshFilterDest.sharedMesh.name);
                        Debug.Log(" dest was: " + meshFilterDest.mesh.name);
                         

                        // List<MeshFilter> mesh_filters = obj.getcom <MeshFilter>();
                        //Debug.Log("  Mesh count: " + mesh.vertexCount);
                        // meshFilterDest.sharedMesh = meshSource;
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

                        // meshFilterDest.transform = ;
                        Debug.Log(" dest is: " + meshFilterDest.sharedMesh.name);
                        //mesh_filter = mesh;
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