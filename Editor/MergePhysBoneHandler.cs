/*
AAO Merge Helper - MergePhysBoneHandler
Copyright (c) 2024 二十一世紀症候群
All rights reserved.

MergePhysBone設定処理
*/


using UnityEngine;
using UnityEditor;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;
using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;

public class MergePhysBoneHandler
{
    private GameObject targetObject;
    private Vector2 scrollPosition;
    private List<PhysBoneGroup> PhysBoneGroups = new List<PhysBoneGroup>();
    private System.Type mergePhysBoneType;

    private bool excludeSimilarGroups = true;
    private bool excludeNonIgnoreMultiChild = true;
    private bool excludeDifferentRootParent = true;
    private bool excludeDifferentColliders = true;

    public class PhysBoneGroup
    {
        public List<VRCPhysBone> PhysBones = new List<VRCPhysBone>();
        public bool isSelected = true;
        public bool isFoldout = true;
        public string groupDescription;
    }

    public MergePhysBoneHandler()
    {
        InitializeAvatarOptimizerTypes();
    }

    private bool InitializeAvatarOptimizerTypes()
    {
        if (mergePhysBoneType != null) return true;

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var avatarOptimizerAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "com.anatawa12.avatar-optimizer.runtime");
        
        if (avatarOptimizerAssembly == null)
        {
            return false;
        }

        mergePhysBoneType = avatarOptimizerAssembly.GetType("Anatawa12.AvatarOptimizer.MergePhysBone");
        if (mergePhysBoneType == null)
        {
            return false;
        }
        
        return true;
    }

    public void SetTarget(GameObject target)
    {
        targetObject = target;
        PhysBoneGroups.Clear();
    }

    public void OnGUI()
    {
        if (mergePhysBoneType == null && !InitializeAvatarOptimizerTypes())
        {
            return;
        }
        
        DrawExclusionOptions();
        DrawSearchButton();

        if (PhysBoneGroups.Count > 0)
        {
            DrawPhysBoneGroupList();
        }
    }

    private void DrawExclusionOptions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUIStyle headerStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };

                GUILayout.Label("設定の異なるもの", headerStyle);
                EditorGUILayout.Space(2);

                DrawToggleWithTooltip(
                    ref excludeDifferentRootParent,
                    "Root Transformの親が異なるものを除外",
                    "PhysBoneのRoot Transformの親階層が異なるものを除外します。親が異なる場合、適切にマージできません"
                );

                DrawToggleWithTooltip(
                    ref excludeDifferentColliders,
                    "Collidersが異なるものを除外",
                    "PhysBoneのコライダー設定が異なるものを除外します"
                );
            }

            GUILayout.Space(5);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUIStyle headerStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };

                GUILayout.Label("特殊な条件", headerStyle);
                EditorGUILayout.Space(2);

                DrawToggleWithTooltip(
                    ref excludeSimilarGroups,
                    "階層が異なる類似項目を除外",
                    "異なる階層にある似た設定のPhysBoneを除外します。異なる階層を統合するには新しい親オブジェクトが必要です。"
                );

                DrawToggleWithTooltip(
                    ref excludeNonIgnoreMultiChild,
                    "Ignore以外のMultiChildTypeを除外",
                    "Ignore以外のMultiChildType設定を持つPhysBoneを除外します"
                );
            }
        }
    }

    private void DrawToggleWithTooltip(ref bool value, string label, string tooltip)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            value = EditorGUILayout.Toggle(value, GUILayout.Width(15));
            var content = new GUIContent(label, tooltip);
            GUILayout.Label(content, GUILayout.ExpandWidth(true));
        }
    }

    private void DrawSearchButton()
    {
        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(targetObject == null))
        {
            if (GUILayout.Button("マージ可能なPhysBoneを検索"))
            {
                CollectMergablePhysBones();
            }
        }
    }

    private void DrawPhysBoneGroupList()
    {
        if (PhysBoneGroups.Count == 0) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("マージ可能なPhysBoneグループ", EditorStyles.boldLabel); 
        
        if (GUILayout.Button("全て選択", GUILayout.Width(100)))
        {
            SelectAllGroups(true);
        }
        if (GUILayout.Button("全て解除", GUILayout.Width(100)))
        {
            SelectAllGroups(false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(3);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var group in PhysBoneGroups)
        {
            DrawPhysBoneGroup(group, PhysBoneGroups.IndexOf(group));
        }

        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(3);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(2);
        
        if (GUILayout.Button("選択したグループをMergePhysBone"))
        {
            SetupMergePhysBone();
        }
    }

    private void DrawPhysBoneGroup(PhysBoneGroup group, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        group.isSelected = EditorGUILayout.Toggle(group.isSelected, GUILayout.Width(20));
        
        group.isFoldout = EditorGUILayout.Foldout(
            group.isFoldout, 
            $"- {group.groupDescription} -", 
            true
        );

        if (GUILayout.Button("グループを表示", GUILayout.Width(100)))
        {
            List<Object> objectsToSelect = group.PhysBones.Select(pb => pb.gameObject).Cast<Object>().ToList();
            Selection.objects = objectsToSelect.ToArray();
            EditorGUIUtility.PingObject(group.PhysBones[0]);
        }
        EditorGUILayout.EndHorizontal();

        if (group.isFoldout)
        {
            EditorGUI.indentLevel++;
            var firstRootTransform = group.PhysBones[0].rootTransform;
            var firstColliders = group.PhysBones[0].colliders;
            bool hasDifferentRootParent = group.PhysBones.Any(pb => !AreRootTransformParentsEqual(pb.rootTransform, firstRootTransform));
            bool hasDifferentColliders = group.PhysBones.Any(pb => !ArePhysBoneCollidersEqual(pb.colliders, firstColliders));

            foreach (var PhysBone in group.PhysBones)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(
                    PhysBone, 
                    typeof(VRCPhysBone), 
                    true
                );

                if (PhysBone.multiChildType != VRC.Dynamics.VRCPhysBoneBase.MultiChildType.Ignore)
                {
                    EditorGUILayout.LabelField($"MultiChildType: {PhysBone.multiChildType}", GUILayout.Width(160));
                }

                if (!AreRootTransformParentsEqual(PhysBone.rootTransform, firstRootTransform))
                {
                    string parentName = "None";
                    if (PhysBone.rootTransform != null && PhysBone.rootTransform.parent != null)
                        parentName = PhysBone.rootTransform.parent.name;
                        
                    EditorGUILayout.LabelField($"Root親: {parentName}", GUILayout.Width(200));
                }

                if (!ArePhysBoneCollidersEqual(PhysBone.colliders, firstColliders))
                {
                    int validCount = GetValidPhysBoneColliderCount(PhysBone.colliders);
                    string colliderInfo = validCount > 0 
                        ? $"Colliders: {validCount}"
                        : "No Colliders";
                    EditorGUILayout.LabelField(colliderInfo, GUILayout.Width(100));
                }

                if (GUILayout.Button("表示", GUILayout.Width(80)))
                {
                    Selection.activeGameObject = PhysBone.gameObject;
                    EditorGUIUtility.PingObject(PhysBone.gameObject);
                    Selection.activeObject = PhysBone;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void CollectMergablePhysBones()
    {
        if (!InitializeAvatarOptimizerTypes())
        {
            return;
        }
        
        PhysBoneGroups.Clear();
        var allPhysBones = targetObject.GetComponentsInChildren<VRCPhysBone>();
        var physBones = allPhysBones.Where(pb => !ShouldSkipPhysBone(pb)).ToArray();
        var processedPhysBones = new HashSet<VRCPhysBone>();

        CreatePhysBoneGroups(physBones, processedPhysBones);

        if (!excludeSimilarGroups)
        {
            CreateSimilarPhysBoneGroups(physBones, processedPhysBones);
        }
        
        if (PhysBoneGroups.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "検索結果", 
                "マージ可能なPhysBoneが見つかりませんでした。\n以下をご確認ください：\n\n- 除外設定を確認する\n- 類似設定のPhysBoneが存在するか確認する", 
                "OK");
        }
    }

    private bool ShouldSkipPhysBone(VRCPhysBone physBone)
    {
        if (physBone == null) return true;
        
        var obj = physBone.gameObject;
        
        if (obj == targetObject)
            return true;
            
        if (!obj.activeInHierarchy)
            return true;
            
        if (obj.GetComponent(mergePhysBoneType) != null)
            return true;
        
        Transform current = obj.transform;
        while (current != null && current != targetObject.transform)
        {
            if (current.gameObject.CompareTag("EditorOnly"))
                return true;
            current = current.parent;
        }
        
        return false;
    }

    private void CreatePhysBoneGroups(VRCPhysBone[] physBones, HashSet<VRCPhysBone> processedPhysBones)
    {
        var parentGroups = physBones
            .GroupBy(pb => pb.transform.parent)
            .Where(g => g.Key != null);

        foreach (var parentGroup in parentGroups)
        {
            var pbList = parentGroup.ToList();
            if (pbList.Count < 2) continue;

            CheckAndCreateGroups(pbList, processedPhysBones, true);
        }
    }

    private void CreateSimilarPhysBoneGroups(VRCPhysBone[] physBones, HashSet<VRCPhysBone> processedPhysBones)
    {
        CheckAndCreateGroups(physBones.ToList(), processedPhysBones, false);
    }

    private void CheckAndCreateGroups(List<VRCPhysBone> physBones, HashSet<VRCPhysBone> processedPhysBones, bool checkParent)
    {
        for (int i = 0; i < physBones.Count; i++)
        {
            if (processedPhysBones.Contains(physBones[i])) continue;

            if (excludeNonIgnoreMultiChild && 
                physBones[i].multiChildType != VRC.Dynamics.VRCPhysBoneBase.MultiChildType.Ignore)
                continue;

            var currentGroup = new PhysBoneGroup();
            currentGroup.PhysBones.Add(physBones[i]);
            processedPhysBones.Add(physBones[i]);

            for (int j = i + 1; j < physBones.Count; j++)
            {
                if (processedPhysBones.Contains(physBones[j])) continue;

                if (excludeNonIgnoreMultiChild && 
                    physBones[j].multiChildType != VRC.Dynamics.VRCPhysBoneBase.MultiChildType.Ignore)
                    continue;

                if (!AreRootTransformParentsEqual(physBones[i].rootTransform, physBones[j].rootTransform) && excludeDifferentRootParent)
                    continue;

                if (ArePhysBonesEqual(physBones[i], physBones[j]))
                {
                    currentGroup.PhysBones.Add(physBones[j]);
                    processedPhysBones.Add(physBones[j]);
                }
            }

            if (currentGroup.PhysBones.Count > 1)
            {
                CreatePhysBoneGroupIfValid(currentGroup);
            }
        }
    }

    private void CreatePhysBoneGroupIfValid(PhysBoneGroup group)
    {
        var firstRootTransform = group.PhysBones[0].rootTransform;
        bool hasDifferentRootParent = group.PhysBones.Any(pb => 
            !AreRootTransformParentsEqual(pb.rootTransform, firstRootTransform));

        if (!hasDifferentRootParent || !excludeDifferentRootParent)
        {
            group.groupDescription = GeneratePhysBoneGroupDescription(group);
            PhysBoneGroups.Add(group);
        }
    }

    private bool AreRootTransformParentsEqual(Transform root1, Transform root2)
    {
        if (root1 == null && root2 == null)
            return true;
        if (root1 == null || root2 == null)
            return false;
            
        if (root1.parent == null && root2.parent == null)
            return true;
        if (root1.parent == null || root2.parent == null)
            return false;
            
        return root1.parent == root2.parent;
    }

    private bool ArePhysBonesEqual(VRCPhysBone pb1, VRCPhysBone pb2)
    {
        using (var so1 = new SerializedObject(pb1))
        using (var so2 = new SerializedObject(pb2))
        {
            so1.Update();
            so2.Update();
            return ComparePhysBoneProperties(so1, so2);
        }
    }

    private bool ComparePhysBoneProperties(SerializedObject so1, SerializedObject so2)
    {
        var prop1 = so1.GetIterator();
        var prop2 = so2.GetIterator();

        prop1.Next(true);
        prop2.Next(true);

        while (prop1.Next(false) && prop2.Next(false))
        {
            if (ShouldSkipPhysBoneProperty(prop1.propertyPath))
                continue;

            if (IsPhysBoneColliderProperty(prop1.propertyPath))
            {
                if (!excludeDifferentColliders)
                    continue;
                if (!SerializedProperty.DataEquals(prop1, prop2))
                    return false;
                continue;
            }

            if (!SerializedProperty.DataEquals(prop1, prop2))
                return false;
        }
        
        return true;
    }

    private bool ShouldSkipPhysBoneProperty(string propertyPath)
    {
        return propertyPath.StartsWith("m_") ||
            propertyPath == "rootTransform" ||
            propertyPath == "_rootTransform" ||
            propertyPath == "m_Script" ||
            propertyPath == "m_GameObject";
    }

    private bool IsPhysBoneColliderProperty(string propertyPath)
    {
        return propertyPath == "colliders" || 
            propertyPath == "_colliders";
    }

    private bool CanMergePhysBones(VRCPhysBone pb1, VRCPhysBone pb2)
    {
        using (var so1 = new SerializedObject(pb1))
        using (var so2 = new SerializedObject(pb2))
        {
            so1.Update();
            so2.Update();

            var iterator1 = so1.GetIterator();
            var iterator2 = so2.GetIterator();

            bool enterChildren = true;
            while (iterator1.Next(enterChildren) && iterator2.Next(enterChildren))
            {
                if (iterator1.propertyPath == "m_Script" || 
                    iterator1.propertyPath.StartsWith("m_") ||
                    iterator1.propertyPath == "m_GameObject" ||
                    iterator1.propertyPath == "rootTransform" ||
                    iterator1.propertyPath == "_rootTransform")
                {
                    continue;
                }

                if (iterator1.propertyPath == "colliders" || iterator1.propertyPath == "_colliders")
                {
                    if (!excludeDifferentColliders)
                    {
                        continue;
                    }
                    else
                    {
                        if (!ArePropertiesEqual(iterator1.Copy(), iterator2.Copy()))
                            return false;
                        continue;
                    }
                }

                switch (iterator1.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if (iterator1.intValue != iterator2.intValue) return false;
                        break;

                    case SerializedPropertyType.Boolean:
                        if (iterator1.boolValue != iterator2.boolValue) return false;
                        break;

                    case SerializedPropertyType.Float:
                        if (!Mathf.Approximately(iterator1.floatValue, iterator2.floatValue)) return false;
                        break;

                    case SerializedPropertyType.String:
                        if (iterator1.stringValue != iterator2.stringValue) return false;
                        break;

                    case SerializedPropertyType.Color:
                        if (iterator1.colorValue != iterator2.colorValue) return false;
                        break;

                    case SerializedPropertyType.ObjectReference:
                        if (iterator1.objectReferenceValue != iterator2.objectReferenceValue) return false;
                        break;

                    case SerializedPropertyType.Enum:
                        if (iterator1.enumValueIndex != iterator2.enumValueIndex) return false;
                        break;

                    case SerializedPropertyType.Vector2:
                        if (iterator1.vector2Value != iterator2.vector2Value) return false;
                        break;

                    case SerializedPropertyType.Vector3:
                        if (iterator1.vector3Value != iterator2.vector3Value) return false;
                        break;

                    case SerializedPropertyType.Vector4:
                        if (iterator1.vector4Value != iterator2.vector4Value) return false;
                        break;

                    case SerializedPropertyType.Rect:
                        if (iterator1.rectValue != iterator2.rectValue) return false;
                        break;

                    case SerializedPropertyType.ArraySize:
                        if (iterator1.intValue != iterator2.intValue) return false;
                        break;

                    case SerializedPropertyType.Character:
                        if (iterator1.intValue != iterator2.intValue) return false;
                        break;

                    case SerializedPropertyType.AnimationCurve:
                        if (!SerializedProperty.DataEquals(iterator1, iterator2)) return false;
                        break;

                    case SerializedPropertyType.Bounds:
                        if (iterator1.boundsValue != iterator2.boundsValue) return false;
                        break;

                    case SerializedPropertyType.Gradient:
                        if (!SerializedProperty.DataEquals(iterator1, iterator2)) return false;
                        break;

                    case SerializedPropertyType.Quaternion:
                        if (iterator1.quaternionValue != iterator2.quaternionValue) return false;
                        break;

                    case SerializedPropertyType.Generic:
                        enterChildren = true;
                        break;

                    default:
                        if (!SerializedProperty.DataEquals(iterator1, iterator2)) return false;
                        enterChildren = false;
                        break;
                }
            }

            return true;
        }
    }

    private bool ArePropertiesEqual(SerializedProperty prop1, SerializedProperty prop2)
    {
        if (prop1.propertyType != prop2.propertyType)
            return false;

        switch (prop1.propertyType)
        {
            case SerializedPropertyType.Integer:
                return prop1.intValue == prop2.intValue;

            case SerializedPropertyType.Boolean:
                return prop1.boolValue == prop2.boolValue;

            case SerializedPropertyType.Float:
                return Mathf.Approximately(prop1.floatValue, prop2.floatValue);

            case SerializedPropertyType.String:
                return prop1.stringValue == prop2.stringValue;

            case SerializedPropertyType.Color:
                return prop1.colorValue == prop2.colorValue;

            case SerializedPropertyType.ObjectReference:
                return prop1.objectReferenceValue == prop2.objectReferenceValue;

            case SerializedPropertyType.Enum:
                return prop1.enumValueIndex == prop2.enumValueIndex;

            case SerializedPropertyType.Vector2:
                return prop1.vector2Value == prop2.vector2Value;

            case SerializedPropertyType.Vector3:
                return prop1.vector3Value == prop2.vector3Value;

            case SerializedPropertyType.Vector4:
                return prop1.vector4Value == prop2.vector4Value;

            case SerializedPropertyType.Rect:
                return prop1.rectValue == prop2.rectValue;

            case SerializedPropertyType.ArraySize:
                return prop1.intValue == prop2.intValue;

            case SerializedPropertyType.Character:
                return prop1.intValue == prop2.intValue;

            case SerializedPropertyType.AnimationCurve:
                return SerializedProperty.DataEquals(prop1, prop2);

            case SerializedPropertyType.Bounds:
                return prop1.boundsValue == prop2.boundsValue;

            case SerializedPropertyType.Gradient:
                return SerializedProperty.DataEquals(prop1, prop2);

            case SerializedPropertyType.Quaternion:
                return prop1.quaternionValue == prop2.quaternionValue;

            case SerializedPropertyType.Generic:
                return CompareGenericProperties(prop1, prop2);

            default:
                return SerializedProperty.DataEquals(prop1, prop2);
        }
    }

    private bool CompareGenericProperties(SerializedProperty prop1, SerializedProperty prop2)
    {
        var p1End = prop1.GetEndProperty();
        var p2End = prop2.GetEndProperty();
        
        while (prop1.Next(true) && prop2.Next(true) && !SerializedProperty.EqualContents(prop1, p1End))
        {
            if (!ArePropertiesEqual(prop1.Copy(), prop2.Copy()))
                return false;
        }
        
        return true;
    }

    private bool ArePhysBoneCollidersEqual(List<VRCPhysBoneColliderBase> colliders1, List<VRCPhysBoneColliderBase> colliders2)
    {
        int count1 = GetValidPhysBoneColliderCount(colliders1);
        int count2 = GetValidPhysBoneColliderCount(colliders2);
        
        if (count1 != count2) return false;
        if (count1 == 0 && count2 == 0) return true;

        var validColliders1 = colliders1?.Where(c => c != null).ToList() ?? new List<VRCPhysBoneColliderBase>();
        var validColliders2 = colliders2?.Where(c => c != null).ToList() ?? new List<VRCPhysBoneColliderBase>();

        if (validColliders1.Count != validColliders2.Count) return false;

        for (int i = 0; i < validColliders1.Count; i++)
        {
            if (validColliders1[i] != validColliders2[i]) return false;
        }

        return true;
    }

    private void SelectAllGroups(bool select)
    {
        foreach (var group in PhysBoneGroups)
        {
            group.isSelected = select;
        }
    }

    private void SetupMergePhysBone()
    {
    if (targetObject == null)
    {
        Debug.LogError("Target object is not set!");
        return;
    }

    GameObject mergeRoot = CreatePhysBoneMergeTarget();

    int processedGroups = 0;
    foreach (var group in PhysBoneGroups.Where(g => g.isSelected && g.PhysBones.Count >= 2))
    {
        processedGroups++;
        SetupPhysBoneMergeGroup(mergeRoot, group, processedGroups);
    }

    if (processedGroups == 0)
    {
        Debug.LogWarning("マージ可能なPhysBoneグループが選択されていません");
        Undo.DestroyObjectImmediate(mergeRoot);
        return;
    }

            Selection.activeGameObject = mergeRoot;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(mergeRoot);
            EditorUtility.SetDirty(mergeRoot);
            AssetDatabase.SaveAssets();
    }

    private GameObject CreatePhysBoneMergeTarget()
    {
        string baseName = "MergePhysBone_Groups";
        string newName = AAOMergeHelper.GetUniqueName(targetObject.transform, baseName);
        GameObject mergeRoot = new GameObject(newName);
        
        mergeRoot.transform.SetParent(targetObject.transform, false);
        mergeRoot.transform.localPosition = Vector3.zero;
        mergeRoot.transform.localRotation = Quaternion.identity;
        mergeRoot.transform.localScale = Vector3.one;
        
        Undo.RegisterCreatedObjectUndo(mergeRoot, "Create PhysBone Merge Groups");
        return mergeRoot;
    }

    private void SetupPhysBoneMergeGroup(GameObject root, PhysBoneGroup group, int index)
    {
    if (root == null || group == null || group.PhysBones.Count < 2) return;

    var firstPb = group.PhysBones[0];
    var commonParent = firstPb.transform.parent;
    bool allUnderSameParent = group.PhysBones.All(pb => pb.transform.parent == commonParent);
    
    string groupName;
    if (allUnderSameParent)
    {
        groupName = $"Under_{commonParent.name}";
    }
    else
    {
        groupName = $"Similar_{firstPb.name}";
    }

    GameObject groupObject = new GameObject(groupName);
    Undo.RegisterCreatedObjectUndo(groupObject, "Create PhysBone Merge Group");
    
    groupObject.transform.SetParent(root.transform, false);

    var mergeComponent = Undo.AddComponent(groupObject, mergePhysBoneType);
    if (mergeComponent == null)
    {
        Debug.LogError("Failed to add MergePhysBone component");
        return;
    }

    SetupPhysBoneMergeSettings(mergeComponent, group);
    }

    private void SetupPhysBoneMergeSettings(Object mergeComponent, PhysBoneGroup group)
    {
    var serializedComponent = new SerializedObject(mergeComponent);

    var mainSetProperty = serializedComponent.FindProperty("componentsSet").FindPropertyRelative("mainSet");
    if (mainSetProperty != null)
    {
        mainSetProperty.ClearArray();
        for (int i = 0; i < group.PhysBones.Count; i++)
        {
            mainSetProperty.arraySize++;
            var element = mainSetProperty.GetArrayElementAtIndex(i);
            if (element != null)
            {
                element.objectReferenceValue = group.PhysBones[i];
            }
        }
    }
    else
    {
        Debug.LogWarning("Could not find 'mainSet' property in componentsSet");
    }

    serializedComponent.ApplyModifiedProperties();
    EditorUtility.SetDirty(mergeComponent);
    }

    private int GetValidPhysBoneColliderCount(List<VRCPhysBoneColliderBase> colliders)
    {
        if (colliders == null) return 0;
        return colliders.Count(c => c != null);
    }

    private string GeneratePhysBoneGroupDescription(PhysBoneGroup group)
    {
        if (group.PhysBones.Count == 0) return "空のグループ";

        var firstPb = group.PhysBones[0];
        var commonParent = firstPb.transform.parent;
        var firstRootTransform = firstPb.rootTransform;
        var firstColliders = firstPb.colliders;

        bool hasDifferentRootParent = group.PhysBones.Any(pb => !AreRootTransformParentsEqual(pb.rootTransform, firstRootTransform));
        bool hasDifferentColliders = group.PhysBones.Any(pb => !ArePhysBoneCollidersEqual(pb.colliders, firstColliders));

        bool allUnderSameParent = group.PhysBones.All(pb => pb.transform.parent == commonParent);
        string baseDesc;

        if (allUnderSameParent)
        {
            baseDesc = $"'{commonParent.name}' の配下";
        }
        else
        {
            baseDesc = $"'{firstPb.name}' と類似（新しい親オブジェクトが必要）";
        }

        if (hasDifferentRootParent)
        {
            baseDesc += " (異なるRoot Transform親階層)";
        }
        if (hasDifferentColliders)
        {
            baseDesc += " (異なるコライダー)";
        }

        return baseDesc;
    }
}
