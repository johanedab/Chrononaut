using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PartToolsLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chrononaut
{
    // Token: 0x02000963 RID: 2403
    public class ChronoReader
    {
        // Token: 0x06004AE1 RID: 19169 RVA: 0x001E1FA4 File Offset: 0x001E01A4
        public static GameObject Read(UrlDir.UrlFile file)
        {
            ChronoReader.file = file;
            BinaryReader binaryReader = new BinaryReader(File.Open(file.fullPath, FileMode.Open));
            if (binaryReader == null)
            {
                Debug.Log("File error");
                return null;
            }
            ChronoReader.matDummies = new List<ChronoReader.MaterialDummy>();
            ChronoReader.boneDummies = new List<ChronoReader.BonesDummy>();
            ChronoReader.textureDummies = new ChronoReader.TextureDummyList();
            FileType fileType = (FileType)binaryReader.ReadInt32();
            ChronoReader.fileVersion = binaryReader.ReadInt32();
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
                    int count = ChronoReader.boneDummies.Count;
                    while (i < count)
                    {
                        Transform[] array = new Transform[ChronoReader.boneDummies[i].bones.Count];
                        int j = 0;
                        int count2 = ChronoReader.boneDummies[i].bones.Count;
                        while (j < count2)
                        {
                            array[j] = ChronoReader.FindChildByName(gameObject.transform, ChronoReader.boneDummies[i].bones[j]);
                            j++;
                        }
                        ChronoReader.boneDummies[i].smr.bones = array;
                        i++;
                    }
                }
                if (ChronoReader.shaderFallback)
                {
                    Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
                    int k = 0;
                    int num = componentsInChildren.Length;
                    while (k < num)
                    {
                        Renderer renderer = componentsInChildren[k];
                        int l = 0;
                        int num2 = renderer.sharedMaterials.Length;
                        while (l < num2)
                        {
                            Material material = renderer.sharedMaterials[l];
                            material.shader = Shader.Find("KSP/Diffuse");
                            l++;
                        }
                        k++;
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

        // Token: 0x06004AE2 RID: 19170 RVA: 0x001E21D8 File Offset: 0x001E03D8
        private static GameObject ReadChild(BinaryReader br, Transform parent)
        {
            GameObject gameObject = new GameObject(br.ReadString());
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            gameObject.transform.localRotation = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            gameObject.transform.localScale = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            while (br.PeekChar() != -1)
            {
                switch (br.ReadInt32())
                {
                    case 0:
                        ChronoReader.ReadChild(br, gameObject.transform);
                        break;
                    case 1:
                        return gameObject;
                    case 2:
                        if (ChronoReader.fileVersion >= 3)
                        {
                            ChronoReader.ReadAnimation(br, gameObject);
                        }
                        else
                        {
                            ChronoReader.ReadAnimation(br, gameObject);
                        }
                        break;
                    case 3:
                        {
                            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                            meshCollider.convex = br.ReadBoolean();
                            if (!meshCollider.convex)
                            {
                                meshCollider.convex = true;
                            }
                            meshCollider.sharedMesh = ChronoReader.ReadMesh(br);
                            break;
                        }
                    case 4:
                        {
                            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                            sphereCollider.radius = br.ReadSingle();
                            sphereCollider.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            break;
                        }
                    case 5:
                        {
                            CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                            capsuleCollider.radius = br.ReadSingle();
                            capsuleCollider.direction = br.ReadInt32();
                            capsuleCollider.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            break;
                        }
                    case 6:
                        {
                            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                            boxCollider.size = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            boxCollider.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            break;
                        }
                    case 7:
                        {
                            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                            meshFilter.sharedMesh = ChronoReader.ReadMesh(br);
                            break;
                        }
                    case 8:
                        ChronoReader.ReadMeshRenderer(br, gameObject);
                        break;
                    case 9:
                        ChronoReader.ReadSkinnedMeshRenderer(br, gameObject);
                        break;
                    case 10:
                        {
                            int num = br.ReadInt32();
                            for (int i = 0; i < num; i++)
                            {
                                ChronoReader.MaterialDummy materialDummy = ChronoReader.matDummies[i];
                                Material material;
                                if (ChronoReader.fileVersion >= 4)
                                {
                                    material = ChronoReader.ReadMaterial4(br);
                                }
                                else
                                {
                                    material = ChronoReader.ReadMaterial(br);
                                }
                                int count = materialDummy.renderers.Count;
                                while (count-- > 0)
                                {
                                    materialDummy.renderers[count].sharedMaterial = material;
                                }
                                int j = 0;
                                int count2 = materialDummy.particleEmitters.Count;
                                while (j < count2)
                                {
                                    materialDummy.particleEmitters[j].material = material;
                                    j++;
                                }
                            }
                            break;
                        }
                    case 12:
                        ChronoReader.ReadTextures(br, gameObject);
                        break;
                    case 23:
                        ChronoReader.ReadLight(br, gameObject);
                        break;
                    case 24:
                        ChronoReader.ReadTagAndLayer(br, gameObject);
                        break;
                    case 25:
                        {
                            MeshCollider meshCollider2 = gameObject.AddComponent<MeshCollider>();
                            bool isTrigger = br.ReadBoolean();
                            meshCollider2.convex = br.ReadBoolean();
                            meshCollider2.isTrigger = isTrigger;
                            if (!meshCollider2.convex)
                            {
                                meshCollider2.convex = true;
                            }
                            meshCollider2.sharedMesh = ChronoReader.ReadMesh(br);
                            break;
                        }
                    case 26:
                        {
                            SphereCollider sphereCollider2 = gameObject.AddComponent<SphereCollider>();
                            sphereCollider2.isTrigger = br.ReadBoolean();
                            sphereCollider2.radius = br.ReadSingle();
                            sphereCollider2.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            break;
                        }
                    case 27:
                        {
                            CapsuleCollider capsuleCollider2 = gameObject.AddComponent<CapsuleCollider>();
                            capsuleCollider2.isTrigger = br.ReadBoolean();
                            capsuleCollider2.radius = br.ReadSingle();
                            capsuleCollider2.height = br.ReadSingle();
                            capsuleCollider2.direction = br.ReadInt32();
                            capsuleCollider2.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            break;
                        }
                    case 28:
                        {
                            BoxCollider boxCollider2 = gameObject.AddComponent<BoxCollider>();
                            boxCollider2.isTrigger = br.ReadBoolean();
                            boxCollider2.size = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            boxCollider2.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            break;
                        }
                    case 29:
                        {
                            WheelCollider wheelCollider = gameObject.AddComponent<WheelCollider>();
                            wheelCollider.mass = br.ReadSingle();
                            wheelCollider.radius = br.ReadSingle();
                            wheelCollider.suspensionDistance = br.ReadSingle();
                            wheelCollider.center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            wheelCollider.suspensionSpring = new JointSpring
                            {
                                spring = br.ReadSingle(),
                                damper = br.ReadSingle(),
                                targetPosition = br.ReadSingle()
                            };
                            wheelCollider.forwardFriction = new WheelFrictionCurve
                            {
                                extremumSlip = br.ReadSingle(),
                                extremumValue = br.ReadSingle(),
                                asymptoteSlip = br.ReadSingle(),
                                asymptoteValue = br.ReadSingle(),
                                stiffness = br.ReadSingle()
                            };
                            wheelCollider.sidewaysFriction = new WheelFrictionCurve
                            {
                                extremumSlip = br.ReadSingle(),
                                extremumValue = br.ReadSingle(),
                                asymptoteSlip = br.ReadSingle(),
                                asymptoteValue = br.ReadSingle(),
                                stiffness = br.ReadSingle()
                            };
                            wheelCollider.enabled = false;
                            break;
                        }
                    case 30:
                        ChronoReader.ReadCamera(br, gameObject);
                        break;
                    case 31:
                        ChronoReader.ReadParticles(br, gameObject);
                        break;
                }
            }
            return gameObject;
        }

        // Token: 0x06004AE3 RID: 19171 RVA: 0x001E27C8 File Offset: 0x001E09C8
        private static void ReadTextures(BinaryReader br, GameObject o)
        {
            int num = br.ReadInt32();
            if (num != ChronoReader.textureDummies.Count)
            {
                Debug.LogError(string.Concat(new object[]
                {
                "TextureError: ",
                num,
                " ",
                ChronoReader.textureDummies.Count
                }));
                return;
            }
            for (int i = 0; i < num; i++)
            {
                string path = br.ReadString();
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                TextureType textureType = (TextureType)br.ReadInt32();
                string url = ChronoReader.file.parent.url + "/" + fileNameWithoutExtension;
                Texture2D texture = GameDatabase.Instance.GetTexture(url, textureType == TextureType.NormalMap);
                if (texture == null)
                {
                    Debug.LogError(string.Concat(new string[]
                    {
                    "Texture '",
                    ChronoReader.file.parent.url,
                    "/",
                    fileNameWithoutExtension,
                    "' not found!"
                    }));
                }
                else
                {
                    int j = 0;
                    int count = ChronoReader.textureDummies[i].Count;
                    while (j < count)
                    {
                        ChronoReader.TextureMaterialDummy textureMaterialDummy = ChronoReader.textureDummies[i][j];
                        int k = 0;
                        int count2 = textureMaterialDummy.shaderName.Count;
                        while (k < count2)
                        {
                            string name = textureMaterialDummy.shaderName[k];
                            textureMaterialDummy.material.SetTexture(name, texture);
                            k++;
                        }
                        j++;
                    }
                }
            }
        }

        // Token: 0x06004AE4 RID: 19172 RVA: 0x001E293C File Offset: 0x001E0B3C
        public static Texture2D NormalMapToUnityNormalMap(Texture2D tex)
        {
            int width = tex.width;
            int height = tex.height;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, true);
            texture2D.wrapMode = TextureWrapMode.Repeat;
            Color color = new Color(1f, 1f, 1f, 1f);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color pixel = tex.GetPixel(i, j);
                    color.r = pixel.g;
                    color.g = pixel.g;
                    color.b = pixel.g;
                    color.a = pixel.r;
                    texture2D.SetPixel(i, j, color);
                }
            }
            texture2D.Apply(true, true);
            return texture2D;
        }

        // Token: 0x06004AE5 RID: 19173 RVA: 0x001E29FC File Offset: 0x001E0BFC
        private static void ReadMeshRenderer(BinaryReader br, GameObject o)
        {
            MeshRenderer meshRenderer = o.AddComponent<MeshRenderer>();
            if (ChronoReader.fileVersion >= 1)
            {
                if (br.ReadBoolean())
                {
                    meshRenderer.shadowCastingMode = ShadowCastingMode.On;
                }
                else
                {
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                meshRenderer.receiveShadows = br.ReadBoolean();
            }
            int num = br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                int j = br.ReadInt32();
                while (j >= ChronoReader.matDummies.Count)
                {
                    ChronoReader.matDummies.Add(new ChronoReader.MaterialDummy());
                }
                ChronoReader.matDummies[j].renderers.Add(meshRenderer);
            }
        }

        // Token: 0x06004AE6 RID: 19174 RVA: 0x001E2A8C File Offset: 0x001E0C8C
        private static void ReadSkinnedMeshRenderer(BinaryReader br, GameObject o)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = o.AddComponent<SkinnedMeshRenderer>();
            int num = br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                int j = br.ReadInt32();
                while (j >= ChronoReader.matDummies.Count)
                {
                    ChronoReader.matDummies.Add(new ChronoReader.MaterialDummy());
                }
                ChronoReader.matDummies[j].renderers.Add(skinnedMeshRenderer);
            }
            skinnedMeshRenderer.localBounds = new Bounds(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()), new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
            skinnedMeshRenderer.quality = (SkinQuality)br.ReadInt32();
            skinnedMeshRenderer.updateWhenOffscreen = br.ReadBoolean();
            int num2 = br.ReadInt32();
            ChronoReader.BonesDummy bonesDummy = new ChronoReader.BonesDummy();
            bonesDummy.smr = skinnedMeshRenderer;
            for (int k = 0; k < num2; k++)
            {
                bonesDummy.bones.Add(br.ReadString());
            }
            ChronoReader.boneDummies.Add(bonesDummy);
            skinnedMeshRenderer.sharedMesh = ChronoReader.ReadMesh(br);
        }

        // Token: 0x06004AE7 RID: 19175 RVA: 0x001E2B94 File Offset: 0x001E0D94
        private static Mesh ReadMesh(BinaryReader br)
        {
            Mesh mesh = new Mesh();
            EntryType entryType = (EntryType)br.ReadInt32();
            if (entryType != EntryType.MeshStart)
            {
                Debug.LogError("Mesh Error");
                return null;
            }
            int num = br.ReadInt32();
            int num2 = br.ReadInt32();
            int num3 = 0;
            while ((entryType = (EntryType)br.ReadInt32()) != EntryType.MeshEnd)
            {
                switch (entryType)
                {
                    case EntryType.MeshVerts:
                        {
                            Vector3[] array = new Vector3[num];
                            for (int i = 0; i < num; i++)
                            {
                                array[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            }
                            mesh.vertices = array;
                            break;
                        }
                    case EntryType.MeshUV:
                        {
                            Vector2[] array2 = new Vector2[num];
                            for (int j = 0; j < num; j++)
                            {
                                array2[j] = new Vector2(br.ReadSingle(), br.ReadSingle());
                            }
                            mesh.uv = array2;
                            break;
                        }
                    case EntryType.MeshUV2:
                        {
                            Vector2[] array3 = new Vector2[num];
                            for (int k = 0; k < num; k++)
                            {
                                array3[k] = new Vector2(br.ReadSingle(), br.ReadSingle());
                            }
                            mesh.uv2 = array3;
                            break;
                        }
                    case EntryType.MeshNormals:
                        {
                            Vector3[] array4 = new Vector3[num];
                            for (int l = 0; l < num; l++)
                            {
                                array4[l] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            }
                            mesh.normals = array4;
                            break;
                        }
                    case EntryType.MeshTangents:
                        {
                            Vector4[] array5 = new Vector4[num];
                            for (int m = 0; m < num; m++)
                            {
                                array5[m] = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            }
                            mesh.tangents = array5;
                            break;
                        }
                    case EntryType.MeshTriangles:
                        {
                            int num4 = br.ReadInt32();
                            int[] array6 = new int[num4];
                            for (int n = 0; n < num4; n++)
                            {
                                array6[n] = br.ReadInt32();
                            }
                            if (mesh.subMeshCount == num3)
                            {
                                mesh.subMeshCount++;
                            }
                            mesh.SetTriangles(array6, num3);
                            num3++;
                            break;
                        }
                    case EntryType.MeshBoneWeights:
                        {
                            BoneWeight[] array7 = new BoneWeight[num];
                            for (int num5 = 0; num5 < num; num5++)
                            {
                                array7[num5] = default(BoneWeight);
                                array7[num5].boneIndex0 = br.ReadInt32();
                                array7[num5].weight0 = br.ReadSingle();
                                array7[num5].boneIndex1 = br.ReadInt32();
                                array7[num5].weight1 = br.ReadSingle();
                                array7[num5].boneIndex2 = br.ReadInt32();
                                array7[num5].weight2 = br.ReadSingle();
                                array7[num5].boneIndex3 = br.ReadInt32();
                                array7[num5].weight3 = br.ReadSingle();
                            }
                            mesh.boneWeights = array7;
                            break;
                        }
                    case EntryType.MeshBindPoses:
                        {
                            int num6 = br.ReadInt32();
                            Matrix4x4[] array8 = new Matrix4x4[num6];
                            for (int num7 = 0; num7 < num6; num7++)
                            {
                                array8[num7] = default(Matrix4x4);
                                array8[num7].m00 = br.ReadSingle();
                                array8[num7].m01 = br.ReadSingle();
                                array8[num7].m02 = br.ReadSingle();
                                array8[num7].m03 = br.ReadSingle();
                                array8[num7].m10 = br.ReadSingle();
                                array8[num7].m11 = br.ReadSingle();
                                array8[num7].m12 = br.ReadSingle();
                                array8[num7].m13 = br.ReadSingle();
                                array8[num7].m20 = br.ReadSingle();
                                array8[num7].m21 = br.ReadSingle();
                                array8[num7].m22 = br.ReadSingle();
                                array8[num7].m23 = br.ReadSingle();
                                array8[num7].m30 = br.ReadSingle();
                                array8[num7].m31 = br.ReadSingle();
                                array8[num7].m32 = br.ReadSingle();
                                array8[num7].m33 = br.ReadSingle();
                            }
                            mesh.bindposes = array8;
                            break;
                        }
                    default:
                        if (entryType == EntryType.MeshVertexColors)
                        {
                            Color32[] array9 = new Color32[num];
                            for (int num8 = 0; num8 < num; num8++)
                            {
                                array9[num8] = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());
                            }
                            mesh.colors32 = array9;
                        }
                        break;
                }
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        // Token: 0x06004AE8 RID: 19176 RVA: 0x001E30BC File Offset: 0x001E12BC
        private static Material ReadMaterial(BinaryReader br)
        {
            string name = br.ReadString();
            Material material;
            switch (br.ReadInt32())
            {
                default:
                    material = new Material(Shader.Find("KSP/Diffuse"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    break;
                case 2:
                    material = new Material(Shader.Find("KSP/Specular"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetColor("_SpecColor", ChronoReader.ReadColor(br));
                    material.SetFloat("_Shininess", br.ReadSingle());
                    break;
                case 3:
                    material = new Material(Shader.Find("KSP/Bumped"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    ChronoReader.ReadMaterialTexture(br, material, "_BumpMap");
                    break;
                case 4:
                    material = new Material(Shader.Find("KSP/Bumped Specular"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    ChronoReader.ReadMaterialTexture(br, material, "_BumpMap");
                    material.SetColor("_SpecColor", ChronoReader.ReadColor(br));
                    material.SetFloat("_Shininess", br.ReadSingle());
                    break;
                case 5:
                    material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    ChronoReader.ReadMaterialTexture(br, material, "_Emissive");
                    material.SetColor(PropertyIDs._EmissiveColor, ChronoReader.ReadColor(br));
                    break;
                case 6:
                    material = new Material(Shader.Find("KSP/Emissive/Specular"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetColor("_SpecColor", ChronoReader.ReadColor(br));
                    material.SetFloat("_Shininess", br.ReadSingle());
                    ChronoReader.ReadMaterialTexture(br, material, "_Emissive");
                    material.SetColor(PropertyIDs._EmissiveColor, ChronoReader.ReadColor(br));
                    break;
                case 7:
                    material = new Material(Shader.Find("KSP/Emissive/Bumped Specular"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    ChronoReader.ReadMaterialTexture(br, material, "_BumpMap");
                    material.SetColor("_SpecColor", ChronoReader.ReadColor(br));
                    material.SetFloat("_Shininess", br.ReadSingle());
                    ChronoReader.ReadMaterialTexture(br, material, "_Emissive");
                    material.SetColor(PropertyIDs._EmissiveColor, ChronoReader.ReadColor(br));
                    break;
                case 8:
                    material = new Material(Shader.Find("KSP/Alpha/Cutoff"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetFloat("_Cutoff", br.ReadSingle());
                    break;
                case 9:
                    material = new Material(Shader.Find("KSP/Alpha/Cutoff Bumped"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    ChronoReader.ReadMaterialTexture(br, material, "_BumpMap");
                    material.SetFloat("_Cutoff", br.ReadSingle());
                    break;
                case 10:
                    material = new Material(Shader.Find("KSP/Alpha/Translucent"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    break;
                case 11:
                    material = new Material(Shader.Find("KSP/Alpha/Translucent Specular"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetFloat("_Gloss", br.ReadSingle());
                    material.SetColor("_SpecColor", ChronoReader.ReadColor(br));
                    material.SetFloat("_Shininess", br.ReadSingle());
                    break;
                case 12:
                    material = new Material(Shader.Find("KSP/Alpha/Unlit Transparent"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetColor("_Color", ChronoReader.ReadColor(br));
                    break;
                case 13:
                    material = new Material(Shader.Find("KSP/Unlit"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetColor("_Color", ChronoReader.ReadColor(br));
                    break;
                case 14:
                    material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetColor("_Color", ChronoReader.ReadColor(br));
                    material.SetFloat("_InvFade", br.ReadSingle());
                    break;
                case 15:
                    material = new Material(Shader.Find("KSP/Particles/Additive"));
                    ChronoReader.ReadMaterialTexture(br, material, "_MainTex");
                    material.SetColor("_Color", ChronoReader.ReadColor(br));
                    material.SetFloat("_InvFade", br.ReadSingle());
                    break;
            }
            if (material != null)
            {
                material.name = name;
            }
            return material;
        }

        // Token: 0x06004AE9 RID: 19177 RVA: 0x001E34DC File Offset: 0x001E16DC
        private static Material ReadMaterial4(BinaryReader br)
        {
            string name = br.ReadString();
            string name2 = br.ReadString();
            int num = br.ReadInt32();
            Shader shader = Shader.Find(name2);
            Material material = new Material(shader);
            material.name = name;
            for (int i = 0; i < num; i++)
            {
                string text = br.ReadString();
                switch (br.ReadInt32())
                {
                    case 0:
                        material.SetColor(text, ChronoReader.ReadColor(br));
                        break;
                    case 1:
                        material.SetVector(text, ChronoReader.ReadVector4(br));
                        break;
                    case 2:
                        material.SetFloat(text, br.ReadSingle());
                        break;
                    case 3:
                        material.SetFloat(text, br.ReadSingle());
                        break;
                    case 4:
                        ChronoReader.ReadMaterialTexture(br, material, text);
                        break;
                }
            }
            return material;
        }

        // Token: 0x06004AEA RID: 19178 RVA: 0x001E35A8 File Offset: 0x001E17A8
        private static void ReadMaterialTexture(BinaryReader br, Material mat, string textureName)
        {
            ChronoReader.textureDummies.AddTextureDummy(br.ReadInt32(), mat, textureName);
            Vector2 value = Vector3.zero;
            value.x = br.ReadSingle();
            value.y = br.ReadSingle();
            mat.SetTextureScale(textureName, value);
            value.x = br.ReadSingle();
            value.y = br.ReadSingle();
            mat.SetTextureOffset(textureName, value);
        }

        // Token: 0x06004AEB RID: 19179 RVA: 0x001E3618 File Offset: 0x001E1818
        private static void ReadAnimation(BinaryReader br, GameObject o)
        {
            Animation animation = o.AddComponent<Animation>();
            int num = br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                AnimationClip animationClip = new AnimationClip();
                animationClip.legacy = true;
                string newName = br.ReadString();
                animationClip.localBounds = new Bounds(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()), new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                animationClip.wrapMode = (WrapMode)br.ReadInt32();
                int num2 = br.ReadInt32();
                for (int j = 0; j < num2; j++)
                {
                    string text = br.ReadString();
                    string text2 = br.ReadString();
                    Type type = null;
                    switch (br.ReadInt32())
                    {
                        case 0:
                            type = typeof(Transform);
                            break;
                        case 1:
                            type = typeof(Material);
                            break;
                        case 2:
                            type = typeof(Light);
                            break;
                        case 3:
                            type = typeof(AudioSource);
                            break;
                    }
                    WrapMode preWrapMode = (WrapMode)br.ReadInt32();
                    WrapMode postWrapMode = (WrapMode)br.ReadInt32();
                    int num3 = br.ReadInt32();
                    Keyframe[] array = new Keyframe[num3];
                    for (int k = 0; k < num3; k++)
                    {
                        array[k] = default(Keyframe);
                        array[k].time = br.ReadSingle();
                        array[k].value = br.ReadSingle();
                        array[k].inTangent = br.ReadSingle();
                        array[k].outTangent = br.ReadSingle();
                        array[k].tangentMode = br.ReadInt32();
                    }
                    AnimationCurve animationCurve = new AnimationCurve(array);
                    animationCurve.preWrapMode = preWrapMode;
                    animationCurve.postWrapMode = postWrapMode;
                    if (text == null || type == null || text2 == null || animationCurve == null)
                    {
                        Debug.Log(string.Concat(new object[]
                        {
                        text,
                        ", ",
                        type,
                        ", ",
                        text2,
                        ", ",
                        animationCurve
                        }));
                    }
                    animationClip.SetCurve(text, type, text2, animationCurve);
                }
                animation.AddClip(animationClip, newName);
            }
            string text3 = br.ReadString();
            if (text3 != string.Empty)
            {
                animation.clip = animation.GetClip(text3);
            }
            animation.playAutomatically = br.ReadBoolean();
        }

        // Token: 0x06004AEC RID: 19180 RVA: 0x001E388C File Offset: 0x001E1A8C
        private static void ReadAnimationEvents(BinaryReader br, GameObject o)
        {
            Animation animation = o.AddComponent<Animation>();
            int num = br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                AnimationClip animationClip = new AnimationClip();
                string newName = br.ReadString();
                animationClip.localBounds = new Bounds(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()), new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                animationClip.wrapMode = (WrapMode)br.ReadInt32();
                int num2 = br.ReadInt32();
                for (int j = 0; j < num2; j++)
                {
                    string relativePath = br.ReadString();
                    string propertyName = br.ReadString();
                    Type type = null;
                    switch (br.ReadInt32())
                    {
                        case 0:
                            type = typeof(Transform);
                            break;
                        case 1:
                            type = typeof(Material);
                            break;
                        case 2:
                            type = typeof(Light);
                            break;
                        case 3:
                            type = typeof(AudioSource);
                            break;
                    }
                    WrapMode preWrapMode = (WrapMode)br.ReadInt32();
                    WrapMode postWrapMode = (WrapMode)br.ReadInt32();
                    int num3 = br.ReadInt32();
                    Keyframe[] array = new Keyframe[num3];
                    for (int k = 0; k < num3; k++)
                    {
                        array[k] = default(Keyframe);
                        array[k].time = br.ReadSingle();
                        array[k].value = br.ReadSingle();
                        array[k].inTangent = br.ReadSingle();
                        array[k].outTangent = br.ReadSingle();
                        array[k].tangentMode = br.ReadInt32();
                    }
                    animationClip.SetCurve(relativePath, type, propertyName, new AnimationCurve(array)
                    {
                        preWrapMode = preWrapMode,
                        postWrapMode = postWrapMode
                    });
                    int num4 = br.ReadInt32();
                    Debug.Log("EVTS: " + num4);
                    for (int l = 0; l < num4; l++)
                    {
                        AnimationEvent animationEvent = new AnimationEvent();
                        animationEvent.time = br.ReadSingle();
                        animationEvent.functionName = br.ReadString();
                        animationEvent.stringParameter = br.ReadString();
                        animationEvent.intParameter = br.ReadInt32();
                        animationEvent.floatParameter = br.ReadSingle();
                        animationEvent.messageOptions = (SendMessageOptions)br.ReadInt32();
                        Debug.Log(animationEvent.time);
                        Debug.Log(animationEvent.functionName);
                        Debug.Log(animationEvent.stringParameter);
                        Debug.Log(animationEvent.intParameter);
                        Debug.Log(animationEvent.floatParameter);
                        Debug.Log(animationEvent.messageOptions);
                        animationClip.AddEvent(animationEvent);
                    }
                }
                animation.AddClip(animationClip, newName);
            }
            string text = br.ReadString();
            if (text != string.Empty)
            {
                animation.clip = animation.GetClip(text);
            }
            animation.playAutomatically = br.ReadBoolean();
        }

        // Token: 0x06004AED RID: 19181 RVA: 0x001E3B9C File Offset: 0x001E1D9C
        private static void ReadLight(BinaryReader br, GameObject o)
        {
            Light light = o.AddComponent<Light>();
            light.type = (LightType)br.ReadInt32();
            light.intensity = br.ReadSingle();
            light.range = br.ReadSingle();
            light.color = ChronoReader.ReadColor(br);
            light.cullingMask = br.ReadInt32();
            if (ChronoReader.fileVersion > 1)
            {
                light.spotAngle = br.ReadSingle();
            }
        }

        // Token: 0x06004AEE RID: 19182 RVA: 0x00038C69 File Offset: 0x00036E69
        private static void ReadTagAndLayer(BinaryReader br, GameObject o)
        {
            o.tag = br.ReadString();
            o.layer = br.ReadInt32();
        }

        // Token: 0x06004AEF RID: 19183 RVA: 0x001E3C00 File Offset: 0x001E1E00
        private static void ReadCamera(BinaryReader br, GameObject o)
        {
            Camera camera = o.AddComponent<Camera>();
            camera.clearFlags = (CameraClearFlags)br.ReadInt32();
            camera.backgroundColor = ChronoReader.ReadColor(br);
            camera.cullingMask = br.ReadInt32();
            camera.orthographic = br.ReadBoolean();
            camera.fieldOfView = br.ReadSingle();
            camera.nearClipPlane = br.ReadSingle();
            camera.farClipPlane = br.ReadSingle();
            camera.depth = br.ReadSingle();
            camera.enabled = false;
        }

        // Token: 0x06004AF0 RID: 19184 RVA: 0x001E3C7C File Offset: 0x001E1E7C
        private static void ReadParticles(BinaryReader br, GameObject o)
        {
            KSPParticleEmitter kspparticleEmitter = o.AddComponent<KSPParticleEmitter>();
            kspparticleEmitter.emit = br.ReadBoolean();
            kspparticleEmitter.shape = (KSPParticleEmitter.EmissionShape)br.ReadInt32();
            kspparticleEmitter.shape3D.x = br.ReadSingle();
            kspparticleEmitter.shape3D.y = br.ReadSingle();
            kspparticleEmitter.shape3D.z = br.ReadSingle();
            kspparticleEmitter.shape2D.x = br.ReadSingle();
            kspparticleEmitter.shape2D.y = br.ReadSingle();
            kspparticleEmitter.shape1D = br.ReadSingle();
            kspparticleEmitter.color = ChronoReader.ReadColor(br);
            kspparticleEmitter.useWorldSpace = br.ReadBoolean();
            kspparticleEmitter.minSize = br.ReadSingle();
            kspparticleEmitter.maxSize = br.ReadSingle();
            kspparticleEmitter.minEnergy = br.ReadSingle();
            kspparticleEmitter.maxEnergy = br.ReadSingle();
            kspparticleEmitter.minEmission = br.ReadInt32();
            kspparticleEmitter.maxEmission = br.ReadInt32();
            kspparticleEmitter.worldVelocity.x = br.ReadSingle();
            kspparticleEmitter.worldVelocity.y = br.ReadSingle();
            kspparticleEmitter.worldVelocity.z = br.ReadSingle();
            kspparticleEmitter.localVelocity.x = br.ReadSingle();
            kspparticleEmitter.localVelocity.y = br.ReadSingle();
            kspparticleEmitter.localVelocity.z = br.ReadSingle();
            kspparticleEmitter.rndVelocity.x = br.ReadSingle();
            kspparticleEmitter.rndVelocity.y = br.ReadSingle();
            kspparticleEmitter.rndVelocity.z = br.ReadSingle();
            kspparticleEmitter.emitterVelocityScale = br.ReadSingle();
            kspparticleEmitter.angularVelocity = br.ReadSingle();
            kspparticleEmitter.rndAngularVelocity = br.ReadSingle();
            kspparticleEmitter.rndRotation = br.ReadBoolean();
            kspparticleEmitter.doesAnimateColor = br.ReadBoolean();
            kspparticleEmitter.colorAnimation = new Color[5];
            for (int i = 0; i < 5; i++)
            {
                kspparticleEmitter.colorAnimation[i] = ChronoReader.ReadColor(br);
            }
            kspparticleEmitter.worldRotationAxis.x = br.ReadSingle();
            kspparticleEmitter.worldRotationAxis.y = br.ReadSingle();
            kspparticleEmitter.worldRotationAxis.z = br.ReadSingle();
            kspparticleEmitter.localRotationAxis.x = br.ReadSingle();
            kspparticleEmitter.localRotationAxis.y = br.ReadSingle();
            kspparticleEmitter.localRotationAxis.z = br.ReadSingle();
            kspparticleEmitter.sizeGrow = br.ReadSingle();
            kspparticleEmitter.rndForce.x = br.ReadSingle();
            kspparticleEmitter.rndForce.y = br.ReadSingle();
            kspparticleEmitter.rndForce.z = br.ReadSingle();
            kspparticleEmitter.force.x = br.ReadSingle();
            kspparticleEmitter.force.y = br.ReadSingle();
            kspparticleEmitter.force.z = br.ReadSingle();
            kspparticleEmitter.damping = br.ReadSingle();
            kspparticleEmitter.castShadows = br.ReadBoolean();
            kspparticleEmitter.recieveShadows = br.ReadBoolean();
            kspparticleEmitter.lengthScale = br.ReadSingle();
            kspparticleEmitter.velocityScale = br.ReadSingle();
            kspparticleEmitter.maxParticleSize = br.ReadSingle();
            switch (br.ReadInt32())
            {
                default:
                    kspparticleEmitter.particleRenderMode = ParticleSystemRenderMode.Billboard;
                    break;
                case 3:
                    kspparticleEmitter.particleRenderMode = ParticleSystemRenderMode.Stretch;
                    break;
                case 4:
                    kspparticleEmitter.particleRenderMode = ParticleSystemRenderMode.HorizontalBillboard;
                    break;
                case 5:
                    kspparticleEmitter.particleRenderMode = ParticleSystemRenderMode.VerticalBillboard;
                    break;
            }
            kspparticleEmitter.uvAnimationXTile = br.ReadInt32();
            kspparticleEmitter.uvAnimationYTile = br.ReadInt32();
            kspparticleEmitter.uvAnimationCycles = br.ReadInt32();
            int j = br.ReadInt32();
            while (j >= ChronoReader.matDummies.Count)
            {
                ChronoReader.matDummies.Add(new ChronoReader.MaterialDummy());
            }
            ChronoReader.matDummies[j].particleEmitters.Add(kspparticleEmitter);
        }

        // Token: 0x06004AF1 RID: 19185 RVA: 0x001E4034 File Offset: 0x001E2234
        public static Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }
            IEnumerator enumerator = parent.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    object obj = enumerator.Current;
                    Transform parent2 = (Transform)obj;
                    Transform transform = ChronoReader.FindChildByName(parent2, name);
                    if (transform != null)
                    {
                        return transform;
                    }
                }
            }
            finally
            {
                IDisposable disposable;
                if ((disposable = (enumerator as IDisposable)) != null)
                {
                    disposable.Dispose();
                }
            }
            return null;
        }

        // Token: 0x06004AF2 RID: 19186 RVA: 0x00038C83 File Offset: 0x00036E83
        private static Color ReadColor(BinaryReader br)
        {
            return new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        // Token: 0x06004AF3 RID: 19187 RVA: 0x00038CA2 File Offset: 0x00036EA2
        private static Vector4 ReadVector2(BinaryReader br)
        {
            return new Vector2(br.ReadSingle(), br.ReadSingle());
        }

        // Token: 0x06004AF4 RID: 19188 RVA: 0x00038CBA File Offset: 0x00036EBA
        private static Vector4 ReadVector3(BinaryReader br)
        {
            return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        // Token: 0x06004AF5 RID: 19189 RVA: 0x00038CD8 File Offset: 0x00036ED8
        private static Vector4 ReadVector4(BinaryReader br)
        {
            return new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        // Token: 0x04004486 RID: 17542
        private static int fileVersion;

        // Token: 0x04004487 RID: 17543
        private static UrlDir.UrlFile file;

        // Token: 0x04004488 RID: 17544
        private static List<ChronoReader.MaterialDummy> matDummies;

        // Token: 0x04004489 RID: 17545
        private static List<ChronoReader.BonesDummy> boneDummies;

        // Token: 0x0400448A RID: 17546
        private static ChronoReader.TextureDummyList textureDummies;

        // Token: 0x0400448B RID: 17547
        private static Shader shaderUnlit;

        // Token: 0x0400448C RID: 17548
        private static Shader shaderDiffuse;

        // Token: 0x0400448D RID: 17549
        private static Shader shaderSpecular;

        // Token: 0x0400448E RID: 17550
        public static bool shaderFallback;

        // Token: 0x02000964 RID: 2404
        public enum ShaderPropertyType
        {
            // Token: 0x04004490 RID: 17552
            Color,
            // Token: 0x04004491 RID: 17553
            Vector,
            // Token: 0x04004492 RID: 17554
            Float,
            // Token: 0x04004493 RID: 17555
            Range,
            // Token: 0x04004494 RID: 17556
            TexEnv
        }

        // Token: 0x02000965 RID: 2405
        private class MaterialDummy
        {
            // Token: 0x06004AF6 RID: 19190 RVA: 0x00038CF7 File Offset: 0x00036EF7
            public MaterialDummy()
            {
                this.renderers = new List<Renderer>();
                this.particleEmitters = new List<KSPParticleEmitter>();
            }

            // Token: 0x04004495 RID: 17557
            public List<Renderer> renderers;

            // Token: 0x04004496 RID: 17558
            public List<KSPParticleEmitter> particleEmitters;
        }

        // Token: 0x02000966 RID: 2406
        private class BonesDummy
        {
            // Token: 0x06004AF7 RID: 19191 RVA: 0x00038D15 File Offset: 0x00036F15
            public BonesDummy()
            {
                this.bones = new List<string>();
            }

            // Token: 0x04004497 RID: 17559
            public SkinnedMeshRenderer smr;

            // Token: 0x04004498 RID: 17560
            public List<string> bones;
        }

        // Token: 0x02000967 RID: 2407
        private class TextureMaterialDummy
        {
            // Token: 0x06004AF8 RID: 19192 RVA: 0x00038D28 File Offset: 0x00036F28
            public TextureMaterialDummy(Material material)
            {
                this.material = material;
                this.shaderName = new List<string>();
            }

            // Token: 0x04004499 RID: 17561
            public Material material;

            // Token: 0x0400449A RID: 17562
            public List<string> shaderName;
        }

        // Token: 0x02000968 RID: 2408
        private class TextureDummy : List<ChronoReader.TextureMaterialDummy>
        {
            // Token: 0x06004AFA RID: 19194 RVA: 0x001E40AC File Offset: 0x001E22AC
            public bool Contains(Material material)
            {
                int count = base.Count;
                while (count-- > 0)
                {
                    if (base[count].material == material)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Token: 0x06004AFB RID: 19195 RVA: 0x001E40E4 File Offset: 0x001E22E4
            public ChronoReader.TextureMaterialDummy Get(Material material)
            {
                int i = 0;
                int count = base.Count;
                while (i < count)
                {
                    if (base[i].material == material)
                    {
                        return base[i];
                    }
                    i++;
                }
                return null;
            }

            // Token: 0x06004AFC RID: 19196 RVA: 0x001E4124 File Offset: 0x001E2324
            public void AddMaterialDummy(Material material, string shaderName)
            {
                ChronoReader.TextureMaterialDummy textureMaterialDummy = this.Get(material);
                if (textureMaterialDummy == null)
                {
                    base.Add(textureMaterialDummy = new ChronoReader.TextureMaterialDummy(material));
                }
                if (!textureMaterialDummy.shaderName.Contains(shaderName))
                {
                    textureMaterialDummy.shaderName.Add(shaderName);
                }
            }
        }

        // Token: 0x02000969 RID: 2409
        private class TextureDummyList : List<ChronoReader.TextureDummy>
        {
            // Token: 0x06004AFE RID: 19198 RVA: 0x00038D52 File Offset: 0x00036F52
            public void AddTextureDummy(int textureID, Material material, string shaderName)
            {
                if (textureID == -1)
                {
                    return;
                }
                while (textureID >= base.Count)
                {
                    base.Add(new ChronoReader.TextureDummy());
                }
                base[textureID].AddMaterialDummy(material, shaderName);
            }
        }
    }
}
