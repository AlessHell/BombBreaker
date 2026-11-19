using System;
using System.IO;
using System.Text;
using BoomerShooter.Gameplay;
using BoomerShooter.LevelSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomerShooter.EditorTools
{
    [InitializeOnLoad]
    public static class BombBreakerAestheticsSetup
    {
        const string k_DoneKey = "BombBreaker_Aesthetics_v1_Done";
        const string k_TexFolder = "Assets/BoomerShooter/Art/Textures/BombBreaker";
        const string k_MatFolder = "Assets/BoomerShooter/Art/Materials";
        const string k_ReportPath = "Logs/BombBreakerAestheticsReport.txt";

        static readonly string[] k_GameplayScenes = new string[]
        {
            "Assets/FPS/Scenes/MainScene.unity",
            "Assets/FPS/Scenes/SecondaryScene.unity",
            "Assets/FPS/Scenes/ThirdScene.unity"
        };

        static BombBreakerAestheticsSetup()
        {
            EditorApplication.delayCall += TryAutoApplyOnce;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.delayCall += TryAutoApplyOnce;
                }
            };
        }

        static void TryAutoApplyOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Application.isBatchMode) return;

            bool skyboxExists = File.Exists(Path.Combine(k_MatFolder, "M_BombBreakerSkybox.mat"));
            if (!EditorPrefs.GetBool(k_DoneKey, false) || !skyboxExists)
            {
                bool ok = ApplyAll(false);
                if (ok)
                {
                    EditorPrefs.SetBool(k_DoneKey, true);
                }
            }
        }

        [MenuItem("Bomb Breaker/Aplicar Estetica de Portada (Todos los Niveles)")]
        public static void ApplyFromMenu()
        {
            bool ok = ApplyAll(true);
            if (ok)
            {
                EditorPrefs.SetBool(k_DoneKey, true);
            }
        }

        public static bool ApplyAll(bool verbose)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Bomb Breaker Aesthetics Setup Report - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            ConfigureTextureImporters(report);
            AssetDatabase.Refresh();

            Shader triplanarShader = Shader.Find("BoomerShooter/URP_TriplanarLit");
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader skyboxShader = Shader.Find("Skybox/Panoramic");

            if (triplanarShader == null || urpLitShader == null || skyboxShader == null)
            {
                string err = $"Missing required shaders: Triplanar={triplanarShader != null}, URPLit={urpLitShader != null}, Skybox={skyboxShader != null}";
                report.AppendLine("ERROR: " + err);
                WriteReport(report.ToString());
                Debug.LogError("[BombBreakerAesthetics] " + err);
                return false;
            }

            // Load generated textures
            Texture2D mossFlagAlb = LoadTex("T_BB_MossFlagstone_Albedo.png");
            Texture2D mossFlagNrm = LoadTex("T_BB_MossFlagstone_Normal.png");
            Texture2D mossFlagMsk = LoadTex("T_BB_MossFlagstone_Mask.png");

            Texture2D stormStoneAlb = LoadTex("T_BB_StormStone_Albedo.png");
            Texture2D stormStoneNrm = LoadTex("T_BB_StormStone_Normal.png");
            Texture2D stormStoneMsk = LoadTex("T_BB_StormStone_Mask.png");

            Texture2D highMossAlb = LoadTex("T_BB_HighlandMoss_Albedo.png");
            Texture2D highMossNrm = LoadTex("T_BB_HighlandMoss_Normal.png");
            Texture2D highMossMsk = LoadTex("T_BB_HighlandMoss_Mask.png");

            Texture2D steelAlb = LoadTex("T_BB_TacticalSteel_Albedo.png");
            Texture2D steelNrm = LoadTex("T_BB_TacticalSteel_Normal.png");
            Texture2D steelMsk = LoadTex("T_BB_TacticalSteel_Mask.png");

            Texture2D skyTex = LoadTex("T_BB_StormyHighlandSkybox.png");

            Texture2D doorAlb = LoadTex("T_BB_BlastDoor_Albedo.png");
            Texture2D doorNrm = LoadTex("T_BB_BlastDoor_Normal.png");
            Texture2D doorEm = LoadTex("T_BB_BlastDoor_Emission.png");

            Texture2D jpAlb = LoadTex("T_BB_JumpPad_Albedo.png");
            Texture2D jpNrm = LoadTex("T_BB_JumpPad_Normal.png");
            Texture2D jpEm = LoadTex("T_BB_JumpPad_Emission.png");

            Texture2D portalAlb = LoadTex("T_BB_PortalSwirl_Albedo.png");
            Texture2D portalEm = LoadTex("T_BB_PortalSwirl_Emission.png");

            // 1. Update shared materials IN PLACE (preserving GUIDs across all 3 scenes)
            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(k_MatFolder, "M_CosmicObsidian.mat"));
            if (matFloor != null)
            {
                matFloor.shader = triplanarShader;
                matFloor.SetTexture("_BaseMap", mossFlagAlb);
                matFloor.SetTexture("_BumpMap", mossFlagNrm);
                matFloor.SetTexture("_MaskMap", mossFlagMsk);
                matFloor.SetColor("_BaseColor", new Color(0.95f, 0.98f, 0.95f, 1f));
                matFloor.SetFloat("_TileScale", 4.0f);
                matFloor.SetFloat("_TriplanarSharpness", 6.0f);
                matFloor.SetFloat("_BumpScale", 1.15f);
                matFloor.SetFloat("_Metallic", 0.05f);
                matFloor.SetFloat("_Smoothness", 0.34f);
                matFloor.SetFloat("_OcclusionStrength", 0.85f);
                matFloor.SetFloat("_TopBlend", 0.0f);
                EditorUtility.SetDirty(matFloor);
                report.AppendLine("Updated M_CosmicObsidian.mat -> URP_TriplanarLit + T_BB_MossFlagstone");
            }

            Material matWall = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(k_MatFolder, "M_GothicStone.mat"));
            if (matWall != null)
            {
                matWall.shader = triplanarShader;
                matWall.SetTexture("_BaseMap", stormStoneAlb);
                matWall.SetTexture("_BumpMap", stormStoneNrm);
                matWall.SetTexture("_MaskMap", stormStoneMsk);
                matWall.SetColor("_BaseColor", new Color(0.95f, 0.98f, 0.98f, 1f));
                matWall.SetFloat("_TileScale", 3.5f);
                matWall.SetFloat("_TriplanarSharpness", 6.0f);
                matWall.SetFloat("_BumpScale", 1.25f);
                matWall.SetFloat("_Metallic", 0.05f);
                matWall.SetFloat("_Smoothness", 0.28f);
                matWall.SetFloat("_OcclusionStrength", 0.85f);
                matWall.SetTexture("_TopMap", highMossAlb);
                matWall.SetTexture("_TopNormalMap", highMossNrm);
                matWall.SetColor("_TopTint", new Color(0.92f, 1.0f, 0.88f, 1f));
                matWall.SetFloat("_TopTileScale", 4.0f);
                matWall.SetFloat("_TopBlend", 0.65f);
                matWall.SetFloat("_TopThreshold", 0.55f);
                EditorUtility.SetDirty(matWall);
                report.AppendLine("Updated M_GothicStone.mat -> URP_TriplanarLit + T_BB_StormStone + Top HighlandMoss");
            }

            Material matTrim = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(k_MatFolder, "M_VoidMetal.mat"));
            if (matTrim != null)
            {
                matTrim.shader = triplanarShader;
                matTrim.SetTexture("_BaseMap", steelAlb);
                matTrim.SetTexture("_BumpMap", steelNrm);
                matTrim.SetTexture("_MaskMap", steelMsk);
                matTrim.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1.0f, 1f));
                matTrim.SetFloat("_TileScale", 2.6f);
                matTrim.SetFloat("_TriplanarSharpness", 6.0f);
                matTrim.SetFloat("_BumpScale", 1.1f);
                matTrim.SetFloat("_Metallic", 0.58f);
                matTrim.SetFloat("_Smoothness", 0.48f);
                matTrim.SetFloat("_OcclusionStrength", 0.85f);
                matTrim.SetFloat("_TopBlend", 0.0f);
                EditorUtility.SetDirty(matTrim);
                report.AppendLine("Updated M_VoidMetal.mat -> URP_TriplanarLit + T_BB_TacticalSteel");
            }

            // 2. Specialized Materials
            Material matSkybox = LoadOrCreateMaterial(Path.Combine(k_MatFolder, "M_BombBreakerSkybox.mat"), skyboxShader);
            matSkybox.SetTexture("_MainTex", skyTex);
            matSkybox.SetFloat("_Mapping", 1f);
            matSkybox.SetFloat("_ImageType", 0f);
            matSkybox.SetFloat("_Exposure", 1.15f);
            matSkybox.SetColor("_Tint", new Color(0.55f, 0.58f, 0.58f, 1f));
            EditorUtility.SetDirty(matSkybox);

            Material matDoorSilver = ConfigureBlastDoorMaterial("M_BB_BlastDoor_Silver.mat", urpLitShader, doorAlb, doorNrm, doorEm, new Color(0.75f, 0.92f, 1.0f) * 2.2f);
            Material matDoorGold   = ConfigureBlastDoorMaterial("M_BB_BlastDoor_Gold.mat",   urpLitShader, doorAlb, doorNrm, doorEm, new Color(1.0f, 0.86f, 0.24f) * 2.4f);
            Material matDoorCosmic = ConfigureBlastDoorMaterial("M_BB_BlastDoor_Cosmic.mat", urpLitShader, doorAlb, doorNrm, doorEm, new Color(0.35f, 0.95f, 1.0f) * 2.5f);

            Material matJumpPad = LoadOrCreateMaterial(Path.Combine(k_MatFolder, "M_BB_JumpPad.mat"), urpLitShader);
            ConfigureUrpLitTextures(matJumpPad, jpAlb, jpNrm, jpEm, new Color(0.28f, 0.92f, 1.0f) * 2.6f, 0.55f, 0.55f);

            Material matPortal = LoadOrCreateMaterial(Path.Combine(k_MatFolder, "M_BB_PortalCore.mat"), urpLitShader);
            ConfigureUrpLitTextures(matPortal, portalAlb, null, portalEm, new Color(0.45f, 0.96f, 0.85f) * 2.3f, 0.1f, 0.65f);

            Material matValley = LoadOrCreateMaterial(Path.Combine(k_MatFolder, "M_BB_ValleyBasin.mat"), triplanarShader);
            matValley.SetTexture("_BaseMap", highMossAlb);
            matValley.SetTexture("_BumpMap", highMossNrm);
            matValley.SetTexture("_MaskMap", highMossMsk);
            matValley.SetColor("_BaseColor", new Color(0.75f, 0.82f, 0.72f, 1f));
            matValley.SetFloat("_TileScale", 5.0f);
            matValley.SetFloat("_BumpScale", 1.0f);
            matValley.SetFloat("_Metallic", 0.02f);
            matValley.SetFloat("_Smoothness", 0.22f);
            matValley.SetFloat("_OcclusionStrength", 0.85f);
            matValley.SetFloat("_TopBlend", 0.0f);
            EditorUtility.SetDirty(matValley);

            AssetDatabase.SaveAssets();
            report.AppendLine("Saved specialized materials: Skybox, Silver/Gold/Cosmic BlastDoors, JumpPad, PortalCore, ValleyBasin");

            // 3. Apply across all 3 gameplay scenes
            string originalScenePath = SceneManager.GetActiveScene().path;
            foreach (string scenePath in k_GameplayScenes)
            {
                if (!File.Exists(scenePath))
                {
                    report.AppendLine("SKIPPED missing scene: " + scenePath);
                    continue;
                }

                Scene sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool isThird = scenePath.EndsWith("ThirdScene.unity", StringComparison.OrdinalIgnoreCase);

                RenderSettings.skybox = matSkybox;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.56f, 0.72f, 0.71f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.36f, 0.47f, 0.46f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.20f, 0.27f, 0.18f, 1f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.27f, 0.39f, 0.39f, 1f);
                RenderSettings.fogDensity = isThird ? 0.0062f : 0.0085f;
                LightmapSettings.lightmaps = new LightmapData[0];

                GameObject dirLightObj = GameObject.Find("Directional Light");
                if (dirLightObj != null)
                {
                    dirLightObj.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                    Light l = dirLightObj.GetComponent<Light>();
                    if (l != null)
                    {
                        l.intensity = 1.45f;
                        l.color = new Color(0.86f, 0.94f, 0.93f, 1f);
                        l.shadows = LightShadows.Soft;
                        l.shadowStrength = 0.55f;
                        EditorUtility.SetDirty(l);
                    }
                    EditorUtility.SetDirty(dirLightObj.transform);
                }

                int doorsUpdated = 0;
                LockedRunicDoor[] doors = UnityEngine.Object.FindObjectsByType<LockedRunicDoor>();
                foreach (LockedRunicDoor door in doors)
                {
                    Transform targetTf = door.DoorTransform != null ? door.DoorTransform : door.transform;
                    Renderer[] rends = targetTf.GetComponentsInChildren<Renderer>(true);
                    Material doorMat = door.RequiredKey == RuneKeyType.Gold
                        ? matDoorGold
                        : (door.RequiredKey == RuneKeyType.Cosmic ? matDoorCosmic : matDoorSilver);
                    foreach (Renderer r in rends)
                    {
                        r.sharedMaterial = doorMat;
                        EditorUtility.SetDirty(r);
                        doorsUpdated++;
                    }
                }

                int padsUpdated = 0;
                JumpPad[] pads = UnityEngine.Object.FindObjectsByType<JumpPad>();
                foreach (JumpPad pad in pads)
                {
                    Renderer[] rends = pad.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer r in rends)
                    {
                        r.sharedMaterial = matJumpPad;
                        EditorUtility.SetDirty(r);
                        padsUpdated++;
                    }
                }

                int portalsUpdated = 0;
                ExitPortal[] portals = UnityEngine.Object.FindObjectsByType<ExitPortal>();
                foreach (ExitPortal portal in portals)
                {
                    if (portal.VortexVisual != null)
                    {
                        foreach (Renderer r in portal.VortexVisual.GetComponentsInChildren<Renderer>(true))
                        {
                            r.sharedMaterial = matPortal;
                            EditorUtility.SetDirty(r);
                            portalsUpdated++;
                        }
                    }
                    else
                    {
                        foreach (Renderer r in portal.GetComponentsInChildren<Renderer>(true))
                        {
                            string n = r.gameObject.name;
                            if (n.IndexOf("Arch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                n.IndexOf("Pillar", StringComparison.OrdinalIgnoreCase) >= 0)
                                continue;
                            r.sharedMaterial = matPortal;
                            EditorUtility.SetDirty(r);
                            portalsUpdated++;
                        }
                    }
                }

                if (isThird)
                {
                    GameObject valleyFloor = GameObject.Find("Sec1_SafetyNet_Floor");
                    if (valleyFloor != null)
                    {
                        Renderer vr = valleyFloor.GetComponent<Renderer>();
                        if (vr != null)
                        {
                            vr.sharedMaterial = matValley;
                            EditorUtility.SetDirty(vr);
                        }
                    }
                }

                BoomerShooterLevelManager lvlMgr = UnityEngine.Object.FindAnyObjectByType<BoomerShooterLevelManager>();
                if (lvlMgr != null)
                {
                    lvlMgr.UseBrightWorkLighting = false;
                    EditorUtility.SetDirty(lvlMgr);
                }

                // Check for any broken/null shaders in the scene
                int brokenRenderers = 0;
                foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>())
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null && m.shader != null && m.shader.name == "Hidden/InternalErrorShader")
                        {
                            brokenRenderers++;
                        }
                    }
                }

                EditorSceneManager.MarkSceneDirty(sc);
                EditorSceneManager.SaveScene(sc);
                report.AppendLine($"Scene {sc.name}: Skybox={matSkybox.name}, Doors={doorsUpdated}, JumpPads={padsUpdated}, Portals={portalsUpdated}, BrokenShaders={brokenRenderers}");
            }

            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            WriteReport(report.ToString());
            if (verbose)
            {
                Debug.Log("[BombBreakerAesthetics] Estetica de portada aplicada en los 3 niveles correctamente.\n" + report);
            }
            return true;
        }

        static void ConfigureTextureImporters(StringBuilder report)
        {
            if (!Directory.Exists(k_TexFolder)) return;
            string[] files = Directory.GetFiles(k_TexFolder, "*.png", SearchOption.TopDirectoryOnly);
            int reimported = 0;

            foreach (string file in files)
            {
                string assetPath = file.Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
                TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (imp == null) continue;

                string fileName = Path.GetFileName(assetPath);
                bool changed = false;

                if (fileName.Contains("Skybox"))
                {
                    if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; changed = true; }
                    if (!imp.sRGBTexture) { imp.sRGBTexture = true; changed = true; }
                    if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; changed = true; }
                    if (imp.mipmapEnabled) { imp.mipmapEnabled = false; changed = true; }
                    if (imp.npotScale != TextureImporterNPOTScale.None) { imp.npotScale = TextureImporterNPOTScale.None; changed = true; }
                    if (imp.maxTextureSize != 2048) { imp.maxTextureSize = 2048; changed = true; }
                }
                else if (fileName.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase))
                {
                    if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; changed = true; }
                    if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; changed = true; }
                    if (imp.anisoLevel != 8) { imp.anisoLevel = 8; changed = true; }
                    if (imp.maxTextureSize != 1024) { imp.maxTextureSize = 1024; changed = true; }
                }
                else if (fileName.EndsWith("_Mask.png", StringComparison.OrdinalIgnoreCase))
                {
                    if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; changed = true; }
                    if (imp.sRGBTexture) { imp.sRGBTexture = false; changed = true; }
                    if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; changed = true; }
                    if (imp.anisoLevel != 8) { imp.anisoLevel = 8; changed = true; }
                    if (imp.maxTextureSize != 1024) { imp.maxTextureSize = 1024; changed = true; }
                }
                else
                {
                    if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; changed = true; }
                    if (!imp.sRGBTexture) { imp.sRGBTexture = true; changed = true; }
                    if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; changed = true; }
                    if (imp.anisoLevel != 8) { imp.anisoLevel = 8; changed = true; }
                    if (imp.maxTextureSize != 1024) { imp.maxTextureSize = 1024; changed = true; }
                }

                if (changed)
                {
                    imp.SaveAndReimport();
                    reimported++;
                }
            }

            report.AppendLine($"Configured {files.Length} textures ({reimported} reimported).");
        }

        static Texture2D LoadTex(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(k_TexFolder, fileName));
        }

        static Material LoadOrCreateMaterial(string assetPath, Shader shader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        static Material ConfigureBlastDoorMaterial(string fileName, Shader shader, Texture2D albedo, Texture2D normal, Texture2D emission, Color emissionColor)
        {
            Material mat = LoadOrCreateMaterial(Path.Combine(k_MatFolder, fileName), shader);
            ConfigureUrpLitTextures(mat, albedo, normal, emission, emissionColor, 0.6f, 0.45f);
            return mat;
        }

        static void ConfigureUrpLitTextures(Material mat, Texture2D albedo, Texture2D normal, Texture2D emission, Color emissionColor, float metallic, float smoothness)
        {
            mat.SetFloat("_Surface", 0f);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Glossiness", smoothness);

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.15f);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.DisableKeyword("_NORMALMAP");
            }

            if (emission != null)
            {
                mat.SetTexture("_EmissionMap", emission);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
        }

        static void WriteReport(string content)
        {
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(k_ReportPath, content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[BombBreakerAesthetics] Could not write report: " + ex.Message);
            }
        }
    }
}
