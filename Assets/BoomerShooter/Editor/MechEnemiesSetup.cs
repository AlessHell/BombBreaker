using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BoomerShooter.Combat;
using BoomerShooter.Enemies;
using Unity.FPS.AI;
using Unity.FPS.Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace BoomerShooter.EditorTools
{
    [InitializeOnLoad]
    public static class MechEnemiesSetup
    {
        const string k_DoneKey = "BombBreaker_MechEnemies_v1_Done";
        const string k_PackRoot = "Assets/BoomerShooter/Art/Enemies/MechPack";
        const string k_ModelsFolder = k_PackRoot + "/Models";
        const string k_TexturesFolder = k_PackRoot + "/Textures";
        const string k_AnimatorsFolder = k_PackRoot + "/Animators";
        const string k_MaterialsFolder = k_PackRoot + "/Materials";
        const string k_PrefabsFolder = "Assets/BoomerShooter/Prefabs/Enemies";
        const string k_BaseHoverBotPrefab = "Assets/FPS/Prefabs/Enemies/Enemy_HoverBot.prefab";
        const string k_ReportPath = "Logs/MechEnemiesSetupReport.txt";

        static readonly string[] k_MechNames = { "George", "Leela", "Mike", "Stan" };

        static readonly string[] k_GameplayScenes =
        {
            "Assets/FPS/Scenes/MainScene.unity",
            "Assets/FPS/Scenes/SecondaryScene.unity",
            "Assets/FPS/Scenes/ThirdScene.unity"
        };

        static MechEnemiesSetup()
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

            string checkPrefab = Path.Combine(k_PrefabsFolder, "Enemy_Leela_Melee.prefab");
            if (!EditorPrefs.GetBool(k_DoneKey, false) || !File.Exists(checkPrefab))
            {
                bool ok = ApplyAll(false);
                if (ok)
                {
                    EditorPrefs.SetBool(k_DoneKey, true);
                }
            }
        }

        [MenuItem("Bomb Breaker/Instalar Enemigos Mech y Cuerpo a Cuerpo (Todos los Niveles)")]
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
            report.AppendLine("Mech Enemies Setup Report - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            Directory.CreateDirectory(k_AnimatorsFolder);
            Directory.CreateDirectory(k_MaterialsFolder);
            Directory.CreateDirectory(k_PrefabsFolder);

            GameObject baseHoverBot = AssetDatabase.LoadAssetAtPath<GameObject>(k_BaseHoverBotPrefab);
            if (baseHoverBot == null)
            {
                string err = "Base prefab missing: " + k_BaseHoverBotPrefab;
                report.AppendLine("ERROR: " + err);
                WriteReport(report.ToString());
                Debug.LogError("[MechEnemiesSetup] " + err);
                return false;
            }

            // 1. Configure FBX & Texture Importers
            ConfigureImporters(report);
            AssetDatabase.Refresh();

            // 2. Create/Update URP Lit Materials
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                string err = "Could not find 'Universal Render Pipeline/Lit' shader.";
                report.AppendLine("ERROR: " + err);
                WriteReport(report.ToString());
                Debug.LogError("[MechEnemiesSetup] " + err);
                return false;
            }

            Dictionary<string, Material> mechMaterials = new Dictionary<string, Material>();
            foreach (string mechName in k_MechNames)
            {
                mechMaterials[mechName] = CreateOrUpdateMechMaterial(mechName, urpLit);
            }
            Material gibMat = CreateOrUpdateGibMaterial(urpLit);

            // 3. Generate 4 AnimatorControllers
            Dictionary<string, AnimatorController> controllers = new Dictionary<string, AnimatorController>
            {
                ["Leela"]  = BuildAnimatorController("Leela",  new[] { "Kick", "Punch", "SwordSlash", "Shoot" }),
                ["Mike"]   = BuildAnimatorController("Mike",   new[] { "Punch", "SwordSlash", "Kick", "Shoot" }),
                ["Stan"]   = BuildAnimatorController("Stan",   new[] { "Shoot", "Punch" }),
                ["George"] = BuildAnimatorController("George", new[] { "Shoot", "Punch" })
            };

            AssetDatabase.SaveAssets();

            // 4. Build 4 Enemy Prefabs
            GameObject leelaPrefab = BuildMechPrefab(
                "Enemy_Leela_Melee.prefab", "Leela", baseHoverBot, mechMaterials["Leela"], gibMat, controllers["Leela"],
                targetHeight: 2.0f, isMelee: true, maxHp: 85f, patrolSpeed: 4.2f, moveOrChaseSpeed: 9.5f,
                detRange: 32f, atkRange: 3.0f, meleeDmg: 18f, isAreaSlam: false, attackRateDelay: 0.75f, report: report);

            GameObject mikePrefab = BuildMechPrefab(
                "Enemy_Mike_Brute.prefab", "Mike", baseHoverBot, mechMaterials["Mike"], gibMat, controllers["Mike"],
                targetHeight: 2.6f, isMelee: true, maxHp: 190f, patrolSpeed: 3.5f, moveOrChaseSpeed: 5.4f,
                detRange: 36f, atkRange: 3.6f, meleeDmg: 30f, isAreaSlam: true, attackRateDelay: 1.0f, report: report);

            GameObject stanPrefab = BuildMechPrefab(
                "Enemy_Stan_Ranged.prefab", "Stan", baseHoverBot, mechMaterials["Stan"], gibMat, controllers["Stan"],
                targetHeight: 2.2f, isMelee: false, maxHp: 100f, patrolSpeed: 5.5f, moveOrChaseSpeed: 6.2f,
                detRange: 36f, atkRange: 22f, meleeDmg: 0f, isAreaSlam: false, attackRateDelay: 0.75f, report: report);

            GameObject georgePrefab = BuildMechPrefab(
                "Enemy_George_Sentinel.prefab", "George", baseHoverBot, mechMaterials["George"], gibMat, controllers["George"],
                targetHeight: 2.5f, isMelee: false, maxHp: 160f, patrolSpeed: 1.5f, moveOrChaseSpeed: 2.0f,
                detRange: 38f, atkRange: 32f, meleeDmg: 0f, isAreaSlam: false, attackRateDelay: 0.55f, report: report);

            AssetDatabase.SaveAssets();

            // 5. Replace enemies in all 3 gameplay scenes
            string originalScenePath = SceneManager.GetActiveScene().path;
            foreach (string scenePath in k_GameplayScenes)
            {
                if (!File.Exists(scenePath))
                {
                    report.AppendLine("SKIPPED missing scene: " + scenePath);
                    continue;
                }
                ReplaceEnemiesInScene(scenePath, leelaPrefab, mikePrefab, stanPrefab, georgePrefab, report);
            }

            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            WriteReport(report.ToString());
            if (verbose)
            {
                Debug.Log("[MechEnemiesSetup] Enemigos del Mech Pack instalados correctamente.\n" + report);
            }
            return true;
        }

        static void ConfigureImporters(StringBuilder report)
        {
            int reimportedModels = 0;
            int reimportedTextures = 0;

            foreach (string mechName in k_MechNames)
            {
                string texPath = $"{k_TexturesFolder}/{mechName}_Texture.png";
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.Default);
                TextureImporter tImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (tImp != null)
                {
                    bool tChanged = false;
                    if (!tImp.sRGBTexture) { tImp.sRGBTexture = true; tChanged = true; }
                    if (tImp.maxTextureSize != 2048) { tImp.maxTextureSize = 2048; tChanged = true; }
                    if (tChanged)
                    {
                        tImp.SaveAndReimport();
                        reimportedTextures++;
                    }
                }

                string fbxPath = $"{k_ModelsFolder}/{mechName}.fbx";
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.Default);
                ModelImporter mImp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (mImp != null)
                {
                    bool mChanged = false;
                    if (mImp.animationType != ModelImporterAnimationType.Generic)
                    {
                        mImp.animationType = ModelImporterAnimationType.Generic;
                        mChanged = true;
                    }
                    if (!mImp.importAnimation)
                    {
                        mImp.importAnimation = true;
                        mChanged = true;
                    }
                    if (mImp.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
                    {
                        mImp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                        mChanged = true;
                    }

                    ModelImporterClipAnimation[] clips = mImp.clipAnimations;
                    if (clips == null || clips.Length == 0)
                    {
                        clips = mImp.defaultClipAnimations;
                        mChanged = true;
                    }

                    for (int i = 0; i < clips.Length; i++)
                    {
                        string cName = clips[i].name;
                        bool shouldLoop =
                            cName.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            cName.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            cName.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (clips[i].loopTime != shouldLoop)
                        {
                            clips[i].loopTime = shouldLoop;
                            mChanged = true;
                        }
                    }

                    if (mChanged)
                    {
                        mImp.clipAnimations = clips;
                        mImp.SaveAndReimport();
                        reimportedModels++;
                    }
                }
            }

            report.AppendLine($"Configured Mech Importers: {reimportedModels} FBXs, {reimportedTextures} Textures reimported.");
        }

        static Material CreateOrUpdateMechMaterial(string mechName, Shader urpLit)
        {
            string matPath = $"{k_MaterialsFolder}/M_Mech_{mechName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{k_TexturesFolder}/{mechName}_Texture.png");
            mat.SetFloat("_Surface", 0f);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Metallic", 0.35f);
            mat.SetFloat("_Smoothness", 0.42f);
            mat.SetFloat("_Glossiness", 0.42f);
            mat.SetColor("_EmissionColor", Color.black);
            mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material CreateOrUpdateGibMaterial(Shader urpLit)
        {
            string matPath = $"{k_MaterialsFolder}/M_Mech_GibDebris.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            Color gibColor = new Color(0.23f, 0.27f, 0.30f, 1f);
            mat.SetColor("_BaseColor", gibColor);
            mat.SetColor("_Color", gibColor);
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.45f);
            mat.SetFloat("_Glossiness", 0.45f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController BuildAnimatorController(string mechName, string[] preferredAttackClips)
        {
            string ctrlPath = $"{k_AnimatorsFolder}/AC_{mechName}.controller";
            if (File.Exists(ctrlPath))
            {
                AssetDatabase.DeleteAsset(ctrlPath);
            }

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Alerted", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("OnDamaged", AnimatorControllerParameterType.Trigger);

            string fbxPath = $"{k_ModelsFolder}/{mechName}.fbx";
            List<AnimationClip> clips = new List<AnimationClip>();
            foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    clips.Add(clip);
                }
            }

            AnimationClip idleClip   = FindClip(clips, new[] { "Idle" });
            AnimationClip walkClip   = FindClip(clips, new[] { "Walk", "Walk_Holding", "Walk_Tall" });
            AnimationClip runClip    = FindClip(clips, new[] { "Run", "Run_Holding", "Run_Tall", "Walk" });
            AnimationClip attackClip = FindClip(clips, preferredAttackClips);
            AnimationClip hitClip    = FindClip(clips, new[] { "HitRecieve_1", "HitRecieve_2", "HitRecieve" });

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimatorState idleState   = sm.AddState("Idle",   new Vector3(260, 40, 0));
            AnimatorState walkState   = sm.AddState("Walk",   new Vector3(520, 40, 0));
            AnimatorState runState    = sm.AddState("Run",    new Vector3(520, 180, 0));
            AnimatorState attackState = sm.AddState("Attack", new Vector3(260, -100, 0));
            AnimatorState hitState    = sm.AddState("Hit",    new Vector3(40,  180, 0));

            idleState.motion   = idleClip;
            walkState.motion   = walkClip   != null ? walkClip   : idleClip;
            runState.motion    = runClip    != null ? runClip    : walkState.motion;
            attackState.motion = attackClip != null ? attackClip : idleClip;
            hitState.motion    = hitClip    != null ? hitClip    : idleClip;

            sm.defaultState = idleState;

            AddFloatTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.25f, 0.12f);
            AddFloatTransition(walkState, idleState, AnimatorConditionMode.Less,    0.20f, 0.12f);
            AddFloatTransition(walkState, runState,  AnimatorConditionMode.Greater, 5.50f, 0.12f);
            AddFloatTransition(runState,  walkState, AnimatorConditionMode.Less,    5.00f, 0.12f);
            AddFloatTransition(idleState, runState,  AnimatorConditionMode.Greater, 5.50f, 0.12f);
            AddFloatTransition(runState,  idleState, AnimatorConditionMode.Less,    0.20f, 0.12f);

            AnimatorStateTransition anyToAttack = sm.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.canTransitionToSelf = true;
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.06f;

            AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.88f;
            attackToIdle.duration = 0.12f;

            AnimatorStateTransition anyToHit = sm.AddAnyStateTransition(hitState);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "OnDamaged");
            anyToHit.canTransitionToSelf = false;
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0.05f;

            AnimatorStateTransition hitToIdle = hitState.AddTransition(idleState);
            hitToIdle.hasExitTime = true;
            hitToIdle.exitTime = 0.82f;
            hitToIdle.duration = 0.12f;

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void AddFloatTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, float duration)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.AddCondition(mode, threshold, "MoveSpeed");
            t.hasExitTime = false;
            t.duration = duration;
        }

        static AnimationClip FindClip(List<AnimationClip> clips, string[] candidates)
        {
            foreach (string cand in candidates)
            {
                // 1. Exact match or Blender/FBX Armature|Name suffix
                foreach (AnimationClip c in clips)
                {
                    if (string.Equals(c.name, cand, StringComparison.OrdinalIgnoreCase) ||
                        c.name.EndsWith("|" + cand, StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
            }

            foreach (string cand in candidates)
            {
                // 2. Partial contains fallback
                foreach (AnimationClip c in clips)
                {
                    if (c.name.IndexOf(cand, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return c;
                    }
                }
            }

            return clips.Count > 0 ? clips[0] : null;
        }

        static GameObject BuildMechPrefab(
            string prefabFileName,
            string fbxName,
            GameObject baseHoverBot,
            Material mechMat,
            Material gibMat,
            AnimatorController animCtrl,
            float targetHeight,
            bool isMelee,
            float maxHp,
            float patrolSpeed,
            float moveOrChaseSpeed,
            float detRange,
            float atkRange,
            float meleeDmg,
            bool isAreaSlam,
            float attackRateDelay,
            StringBuilder report)
        {
            string prefabPath = $"{k_PrefabsFolder}/{prefabFileName}";
            string fbxPath = $"{k_ModelsFolder}/{fbxName}.fbx";
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            GameObject tempRoot = (GameObject)PrefabUtility.InstantiatePrefab(baseHoverBot);
            PrefabUtility.UnpackPrefabInstance(tempRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            tempRoot.name = Path.GetFileNameWithoutExtension(prefabFileName);
            tempRoot.transform.position = Vector3.zero;
            tempRoot.transform.rotation = Quaternion.identity;
            tempRoot.transform.localScale = Vector3.one;

            // 1. Strip old robot MeshRenderers, SkinnedMeshRenderers, and old Animators (preserving UI Canvas & ParticleSystems)
            foreach (Renderer r in tempRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<Canvas>(true) != null) continue;

                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null) UnityEngine.Object.DestroyImmediate(mf);
                UnityEngine.Object.DestroyImmediate(r);
            }

            foreach (Animator oldAnim in tempRoot.GetComponentsInChildren<Animator>(true))
            {
                UnityEngine.Object.DestroyImmediate(oldAnim);
            }

            // 2. Instantiate Mech FBX as MechVisualRoot & assign material
            GameObject mechVis = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, tempRoot.transform);
            mechVis.name = "MechVisualRoot";
            mechVis.transform.localPosition = Vector3.zero;
            mechVis.transform.localRotation = Quaternion.identity;
            mechVis.transform.localScale = Vector3.one;

            foreach (Renderer r in mechVis.GetComponentsInChildren<Renderer>(true))
            {
                int matCount = Mathf.Max(1, r.sharedMaterials.Length);
                Material[] mats = new Material[matCount];
                for (int i = 0; i < matCount; i++) mats[i] = mechMat;
                r.sharedMaterials = mats;
            }

            // 3. Auto-normalize height and snap feet cleanly to local Y = 0
            Bounds rawBounds = ComputeVisualBounds(mechVis);
            float scaleFactor = targetHeight / Mathf.Max(rawBounds.size.y, 0.01f);
            mechVis.transform.localScale = Vector3.one * scaleFactor;
            Bounds scaledBounds = ComputeVisualBounds(mechVis);
            float minY = Mathf.Abs(scaledBounds.size.y - targetHeight) < 0.25f
                ? scaledBounds.min.y
                : rawBounds.min.y * scaleFactor;
            mechVis.transform.localPosition = new Vector3(0f, -minY, 0f);

            // 4. Wire Animator on MechVisualRoot
            Animator mechAnim = mechVis.GetComponent<Animator>();
            if (mechAnim == null) mechAnim = mechVis.AddComponent<Animator>();
            mechAnim.runtimeAnimatorController = animCtrl;
            mechAnim.applyRootMotion = false;

            // 5. Adjust HitBox, DetectionModule, WeaponRoot, HealthBarPivot
            Transform hitBoxTf = FindChildByName(tempRoot.transform, "HitBox");
            if (hitBoxTf != null)
            {
                hitBoxTf.localPosition = new Vector3(0f, targetHeight * 0.52f, 0f);
                SphereCollider sc = hitBoxTf.GetComponent<SphereCollider>();
                if (sc != null)
                {
                    sc.center = Vector3.zero;
                    sc.radius = targetHeight * 0.42f;
                }
                CapsuleCollider cc = hitBoxTf.GetComponent<CapsuleCollider>();
                if (cc != null)
                {
                    cc.center = Vector3.zero;
                    cc.height = targetHeight * 0.9f;
                    cc.radius = targetHeight * 0.35f;
                }
            }

            DetectionModule detMod = tempRoot.GetComponentInChildren<DetectionModule>(true);
            if (detMod != null)
            {
                detMod.transform.localPosition = new Vector3(0f, targetHeight * 0.72f, 0f);
                detMod.DetectionRange = detRange;
                detMod.AttackRange = atkRange;
                detMod.Animator = mechAnim;
            }

            Transform weaponRootTf = FindChildByName(tempRoot.transform, "WeaponRoot");
            if (weaponRootTf != null)
            {
                weaponRootTf.localPosition = new Vector3(0f, targetHeight * 0.62f, targetHeight * 0.25f);
            }

            Transform hpPivotTf = FindChildByName(tempRoot.transform, "HealthBarPivot");
            if (hpPivotTf != null)
            {
                hpPivotTf.localPosition = new Vector3(0f, targetHeight + 0.35f, 0f);
            }

            // 6. NavMeshAgent, NavigationModule, Health, EnemyController, GibSpawner
            NavMeshAgent agent = tempRoot.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.height = targetHeight;
                agent.radius = Mathf.Clamp(targetHeight * 0.28f, 0.45f, 1.1f);
                agent.speed = moveOrChaseSpeed;
                agent.acceleration = moveOrChaseSpeed * 2.5f;
                agent.angularSpeed = 360f;
            }

            NavigationModule navMod = tempRoot.GetComponentInChildren<NavigationModule>(true);
            if (navMod != null)
            {
                navMod.MoveSpeed = moveOrChaseSpeed;
                navMod.AngularSpeed = 360f;
                navMod.Acceleration = moveOrChaseSpeed * 2.5f;
            }

            Health health = tempRoot.GetComponent<Health>();
            if (health != null)
            {
                health.MaxHealth = maxHp;
                health.CurrentHealth = maxHp;
            }

            EnemyController ec = tempRoot.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.BodyMaterial = mechMat;
                ec.EyeColorMaterial = null;
                ec.SelfDestructYHeight = -25f;
            }

            GibSpawner gibs = tempRoot.GetComponent<GibSpawner>();
            if (gibs != null)
            {
                gibs.GibMaterial = gibMat;
                gibs.GibCount = isAreaSlam ? 12 : 8;
            }

            // 7. Wire AI: Melee vs Ranged
            EnemyMobile oldMobile = tempRoot.GetComponent<EnemyMobile>();
            if (isMelee)
            {
                ParticleSystem[] sparks = oldMobile != null ? oldMobile.RandomHitSparks : null;
                AudioClip detectSfx = oldMobile != null ? oldMobile.OnDetectSfx : null;

                if (oldMobile != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldMobile);
                }

                BoomerMeleeEnemy melee = tempRoot.AddComponent<BoomerMeleeEnemy>();
                melee.Animator = mechAnim;
                melee.PatrolSpeed = patrolSpeed;
                melee.ChaseSpeed = moveOrChaseSpeed;
                melee.MeleeTriggerRange = isAreaSlam ? 3.2f : 2.6f;
                melee.MeleeHitRange = isAreaSlam ? 3.8f : 3.2f;
                melee.MeleeDamage = meleeDmg;
                melee.WindupDuration = isAreaSlam ? 0.38f : 0.22f;
                melee.AttackCooldown = isAreaSlam ? 1.35f : 0.90f;
                melee.LungeSpeed = isAreaSlam ? 5.0f : 11.0f;
                melee.KnockbackForce = isAreaSlam ? 13.0f : 7.5f;
                melee.IsAreaSlam = isAreaSlam;
                melee.RandomHitSparks = sparks;
                melee.OnDetectSfx = detectSfx;
            }
            else
            {
                if (oldMobile != null)
                {
                    oldMobile.Animator = mechAnim;
                    oldMobile.AttackStopDistanceRatio = (fbxName == "George") ? 0.92f : 0.55f;
                }

                WeaponController wc = tempRoot.GetComponentInChildren<WeaponController>(true);
                if (wc != null)
                {
                    wc.DelayBetweenShots = attackRateDelay;
                }
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath, out bool success);
            UnityEngine.Object.DestroyImmediate(tempRoot);
            report.AppendLine($"Built prefab {prefabPath} (Success={success}, Model={fbxName}, Height={targetHeight:F2}m, Melee={isMelee}, HP={maxHp})");
            return savedPrefab;
        }

        static Bounds ComputeVisualBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds combined = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Renderer r in renderers)
            {
                Bounds rb = r.bounds;
                if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                {
                    // Transform local mesh bounds if SMR world bounds appear empty in Edit Mode
                    if (rb.size.sqrMagnitude < 0.0001f)
                    {
                        Bounds lb = smr.sharedMesh.bounds;
                        Vector3 worldCenter = smr.transform.TransformPoint(lb.center);
                        Vector3 worldSize = Vector3.Scale(lb.size, smr.transform.lossyScale);
                        rb = new Bounds(worldCenter, new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)));
                    }
                }

                if (rb.size.sqrMagnitude < 0.00001f) continue;
                if (!hasBounds)
                {
                    combined = rb;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(rb);
                }
            }

            return hasBounds ? combined : new Bounds(new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 1f));
        }

        static Transform FindChildByName(Transform root, string targetName)
        {
            foreach (Transform tf in root.GetComponentsInChildren<Transform>(true))
            {
                if (tf.name == targetName) return tf;
            }
            return null;
        }

        static void ReplaceEnemiesInScene(
            string scenePath,
            GameObject leelaPrefab,
            GameObject mikePrefab,
            GameObject stanPrefab,
            GameObject georgePrefab,
            StringBuilder report)
        {
            Scene sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EnemyController[] existingEnemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
            PatrolPath[] patrolPaths = UnityEngine.Object.FindObjectsByType<PatrolPath>();

            bool isMain = scenePath.EndsWith("MainScene.unity", StringComparison.OrdinalIgnoreCase);
            bool isSecondary = scenePath.EndsWith("SecondaryScene.unity", StringComparison.OrdinalIgnoreCase);
            bool isThird = scenePath.EndsWith("ThirdScene.unity", StringComparison.OrdinalIgnoreCase);

            int replacedCount = 0;
            foreach (EnemyController oldEc in existingEnemies)
            {
                if (oldEc == null) continue;
                string oldName = oldEc.name;

                // Skip already-replaced mechs so initialization stays idempotent
                if (oldName.StartsWith("Mech_", StringComparison.OrdinalIgnoreCase) ||
                    oldName.StartsWith("Boss_George_Overlord", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GameObject chosenPrefab;
                string newName;
                bool isBoss = false;

                if (isMain)
                {
                    if (oldName.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = georgePrefab;
                        newName = "Mech_George_" + oldName;
                    }
                    else if (oldName.IndexOf("Z3_Upper", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = stanPrefab;
                        newName = "Mech_Stan_" + oldName;
                    }
                    else
                    {
                        chosenPrefab = leelaPrefab;
                        newName = "Mech_Leela_" + oldName;
                    }
                }
                else if (isSecondary)
                {
                    if (oldName.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = georgePrefab;
                        newName = "Mech_George_" + oldName;
                    }
                    else if (oldName.IndexOf("Bruiser", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = mikePrefab;
                        newName = "Mech_Mike_" + oldName;
                    }
                    else if (oldName.IndexOf("West_Entry", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = stanPrefab;
                        newName = "Mech_Stan_" + oldName;
                    }
                    else
                    {
                        chosenPrefab = leelaPrefab;
                        newName = "Mech_Leela_" + oldName;
                    }
                }
                else if (isThird)
                {
                    if (oldName.IndexOf("Boss_Archon", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = georgePrefab;
                        newName = "Boss_George_Overlord";
                        isBoss = true;
                    }
                    else if (oldName.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = georgePrefab;
                        newName = "Mech_George_" + oldName;
                    }
                    else if (oldName.IndexOf("Bruiser", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = mikePrefab;
                        newName = "Mech_Mike_" + oldName;
                    }
                    else if (oldName.IndexOf("VaultPatrol", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosenPrefab = leelaPrefab;
                        newName = "Mech_Leela_" + oldName;
                    }
                    else
                    {
                        chosenPrefab = stanPrefab;
                        newName = "Mech_Stan_" + oldName;
                    }
                }
                else
                {
                    chosenPrefab = stanPrefab;
                    newName = "Mech_Stan_" + oldName;
                }

                Transform parentTf = oldEc.transform.parent;
                Vector3 pos = oldEc.transform.position;
                Quaternion rot = oldEc.transform.rotation;

                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                }

                GameObject newGo = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, parentTf);
                newGo.name = newName;
                newGo.transform.position = pos;
                newGo.transform.rotation = rot;

                if (isBoss)
                {
                    newGo.transform.localScale = Vector3.one * 1.8f;
                    Health bh = newGo.GetComponent<Health>();
                    if (bh != null)
                    {
                        bh.MaxHealth = 350f;
                        bh.CurrentHealth = 350f;
                        EditorUtility.SetDirty(bh);
                    }
                    DetectionModule dm = newGo.GetComponentInChildren<DetectionModule>(true);
                    if (dm != null)
                    {
                        dm.DetectionRange = 48f;
                        dm.AttackRange = 42f;
                        EditorUtility.SetDirty(dm);
                    }
                    EnemyController bec = newGo.GetComponent<EnemyController>();
                    if (bec != null)
                    {
                        EditorUtility.SetDirty(bec);
                    }
                }

                EnemyController newEc = newGo.GetComponent<EnemyController>();
                foreach (PatrolPath pp in patrolPaths)
                {
                    if (pp == null || pp.EnemiesToAssign == null) continue;
                    for (int idx = 0; idx < pp.EnemiesToAssign.Count; idx++)
                    {
                        if (pp.EnemiesToAssign[idx] == oldEc)
                        {
                            pp.EnemiesToAssign[idx] = newEc;
                            EditorUtility.SetDirty(pp);
                        }
                    }
                }

                EditorUtility.SetDirty(newGo);
                UnityEngine.Object.DestroyImmediate(oldEc.gameObject);
                replacedCount++;
            }

            if (replacedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(sc);
                EditorSceneManager.SaveScene(sc);
            }

            EnemyController[] finalEnemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
            report.AppendLine($"Scene {sc.name}: Replaced={replacedCount}, FinalEnemyCount={finalEnemies.Length}");
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
                Debug.LogWarning("[MechEnemiesSetup] Could not write report: " + ex.Message);
            }
        }
    }
}
