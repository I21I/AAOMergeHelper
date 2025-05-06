/*
AAO Merge Helper
Copyright (c) 2024 二十一世紀症候群
All rights reserved.

AvatarOptimizerのMergeSkinnedMesh,MergePhysBoneを簡単に設定するためのツール
- Unity 2022.3.22f1
- VRCSDK3 3.8.1-beta.1
- Avatar Optimizer 1.8.10
動作確認済み
*/


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;

public class AAOMergeHelper : EditorWindow
{
    private GameObject targetObject;
    private Tab currentTab = Tab.SkinnedMesh;
    private bool isAvatarOptimizerInstalled = false;

    private MergeSkinnedMeshHandler skinnedMeshHandler;
    private MergePhysBoneHandler physBoneHandler;

    private enum Tab
    {
        SkinnedMesh,
        PhysBone
    }

    [MenuItem("21CSX/AAO Merge Helper")]
    public static void ShowWindow()
    {
        var window = GetWindow<AAOMergeHelper>("AAO Merge Helper");
        window.Init();
    }

    private void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        if (skinnedMeshHandler == null)
            skinnedMeshHandler = new MergeSkinnedMeshHandler();
        
        if (physBoneHandler == null)
            physBoneHandler = new MergePhysBoneHandler();

        CheckAvatarOptimizerInstallation();

        if (targetObject != null)
        {
            skinnedMeshHandler.SetTarget(targetObject);
            physBoneHandler.SetTarget(targetObject);
        }
    }

    private void CheckAvatarOptimizerInstallation()
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var avatarOptimizerAssembly = assemblies.FirstOrDefault(a => 
            a.GetName().Name == "com.anatawa12.avatar-optimizer.runtime" ||
            a.GetName().Name == "anatawa.avatar-optimizer.runtime");
        
        isAvatarOptimizerInstalled = (avatarOptimizerAssembly != null);
    }

    private void OnGUI()
    {
        if (!isAvatarOptimizerInstalled)
        {
            EditorGUILayout.Space(20);
            
            GUIStyle warningStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("AvatarOptimizerがインストールされていません", warningStyle, GUILayout.Height(30));
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("このツールを使用するには、AvatarOptimizerをインストールしてください。", 
                new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });
            
            EditorGUILayout.Space(20);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("AvatarOptimizerをインストールする", GUILayout.Height(40), GUILayout.Width(300)))
            {
                if (EditorUtility.DisplayDialog(
                    "AvatarOptimizer インストール",
                    "AvatarOptimizerのインストールページをブラウザで開きます。\n\nVCCやALCOMで既にインストール済みの場合は「いいえ」を選択し、プロジェクトに追加してください。",
                    "はい", "いいえ"))
                {
                    Application.OpenURL("https://vpm.anatawa12.com/avatar-optimizer/ja/#installation");
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            return;
        }
        
        DrawHeader();
        DrawTabs();
        DrawContent();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        
        using (new EditorGUILayout.VerticalScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("AvatarOptimizer用 マージ補助ツール", EditorStyles.boldLabel);
                
                if (targetObject != null)
                {
                    var avatarDescriptor = targetObject.GetComponent<VRCAvatarDescriptor>();
                    using (new EditorGUI.DisabledScope(avatarDescriptor == null))
                    {
                        var content = new GUIContent(
                            "TraceAndOptimizeを設定", 
                            "アバタールートにTraceAndOptimizeを追加します。VRCAvatarDescriptorがあるオブジェクトのみ設定できます。"
                        );
                        
                        if (GUILayout.Button(content, GUILayout.Width(160)))
                        {
                            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                            var type = assemblies.SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.FullName == "Anatawa12.AvatarOptimizer.TraceAndOptimize");

                            if (type != null && avatarDescriptor != null)
                            {
                                var component = Undo.AddComponent(targetObject, type);
                                EditorUtility.SetDirty(targetObject);
                                Selection.activeGameObject = targetObject;
                                EditorGUIUtility.PingObject(targetObject);
                            }
                        }
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUILayout.Button("TraceAndOptimizeを設定", GUILayout.Width(160));
                    }
                }
            }
            
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newTarget = EditorGUILayout.ObjectField(
                    "アバター/衣装", 
                    targetObject, 
                    typeof(GameObject), 
                    true
                ) as GameObject;

                if (check.changed && newTarget != targetObject)
                {
                    targetObject = newTarget;
                    skinnedMeshHandler.SetTarget(targetObject);
                    physBoneHandler.SetTarget(targetObject);
                }
            }
        }
    }

    private void DrawTabs()
    {
        EditorGUILayout.Space(5);

        using (new EditorGUILayout.HorizontalScope())
        {
            float totalWidth = position.width + 10f;
            float tabWidth = totalWidth / 2;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Width(totalWidth)))
            {
                GUILayout.Space(-5f);
                if (GUILayout.Toggle(currentTab == Tab.SkinnedMesh, "MergeSkinnedMesh", EditorStyles.toolbarButton, GUILayout.Width(tabWidth)))
                    currentTab = Tab.SkinnedMesh;
                
                if (GUILayout.Toggle(currentTab == Tab.PhysBone, "MergePhysBone", EditorStyles.toolbarButton, GUILayout.Width(tabWidth)))
                    currentTab = Tab.PhysBone;
                GUILayout.Space(-5f);
            }
        }
    }

    private void DrawContent()
    {
        EditorGUILayout.Space(5);

        if (targetObject == null)
        {
            EditorGUILayout.HelpBox("アバターまたは衣装を選択してください", MessageType.Info);
            return;
        }

        switch (currentTab)
        {
            case Tab.SkinnedMesh:
                skinnedMeshHandler.OnGUI();
                break;
            case Tab.PhysBone:
                physBoneHandler.OnGUI();
                break;
        }
    }

    private static string GetUniqueGroupName(Transform parent, string baseName)
    {
        var existingObjects = parent.Cast<Transform>()
            .Select(t => t.name)
            .Where(name => name.StartsWith(baseName))
            .ToList();

        if (!existingObjects.Contains(baseName))
            return baseName;

        int maxNumber = existingObjects
            .Select(name =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(name, @"\((\d+)\)$");
                return match.Success ? int.Parse(match.Groups[1].Value) : 0;
            })
            .Max();

        return $"{baseName} ({maxNumber + 1})";
    }

    public static string GetUniqueName(Transform parent, string baseName)
    {
        return GetUniqueGroupName(parent, baseName);
    }
}
