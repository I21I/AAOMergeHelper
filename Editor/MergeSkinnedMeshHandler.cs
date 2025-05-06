/*
AAO Merge Helper - MergeSkinnedMeshHandler
Copyright (c) 2024 二十一世紀症候群
All rights reserved.

MergeSkinnedMesh設定処理
*/


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;

public class MergeSkinnedMeshHandler
{
    private GameObject targetObject;
    private HashSet<GameObject> FXToggledObjects;
    private HashSet<string> MAControlledPaths;
    private HashSet<GameObject> LilycalControlledObjects;
    private Vector2 scrollPosition;
    private System.Type mergeSkinnedMeshType;

    private bool excludeFXToggled = true;
    private bool excludeMAControlled = true;
    private bool excludeLilycal = true;

    private List<MeshGroup> meshGroups = new List<MeshGroup>();
    private Transform commonRootBone;
    private Transform commonProbeAnchor;
    private SkinnedMeshRenderer referenceRenderer;

    public class MeshGroup
    {
        public List<Renderer> Renderers = new List<Renderer>();
        public bool isSelected = true;
        public bool isFoldout = true;
        public string groupDescription;
        public GameObject commonParent;
        public List<bool> rendererSelection;

        public MeshGroup()
        {
            rendererSelection = new List<bool>();
        }
    }

    public MergeSkinnedMeshHandler()
    {
        InitializeAvatarOptimizerTypes();
    }

    private bool InitializeAvatarOptimizerTypes()
    {
        if (mergeSkinnedMeshType != null) return true;

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var avatarOptimizerAssembly = assemblies.FirstOrDefault(a => 
            a.GetName().Name == "com.anatawa12.avatar-optimizer.runtime" ||
            a.GetName().Name == "anatawa.avatar-optimizer.runtime");
        
        if (avatarOptimizerAssembly == null)
        {
            return false;
        }

        mergeSkinnedMeshType = avatarOptimizerAssembly.GetType("Anatawa12.AvatarOptimizer.MergeSkinnedMesh");
        if (mergeSkinnedMeshType == null)
        {
            return false;
        }
        
        return true;
    }

    public void SetTarget(GameObject target)
    {
        targetObject = target;
        meshGroups.Clear();
        InitializeAvatarOptimizerTypes();
    }

    public void OnGUI()
    {
        if (mergeSkinnedMeshType == null && !InitializeAvatarOptimizerTypes())
        {
            return;
        }
        
        DrawExclusionOptions();
        DrawSearchButton();

        if (meshGroups.Count > 0)
        {
            DrawMeshGroupList();
            DrawReferenceSettings();
            DrawMergeButton();
        }
    }

    private void DrawExclusionOptions()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            GUILayout.Label("除外設定", headerStyle);
            EditorGUILayout.Space(2);

            DrawToggleWithTooltip(
                ref excludeFXToggled,
                "FXレイヤーでトグルされるオブジェクトを除外",
                "アニメーションで表示切り替えされるオブジェクトを除外します"
            );

            DrawToggleWithTooltip(
                ref excludeMAControlled,
                "ModularAvatarで制御されるオブジェクトを除外",
                "ModularAvatarのObject ToggleやShape Changerで制御されるオブジェクトを除外します"
            );

            DrawToggleWithTooltip(
                ref excludeLilycal,
                "lilycalInventoryで制御されるオブジェクトを除外",
                "lilycalInventoryのToggle制御で使用されるオブジェクトを除外します"
            );
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
            if (GUILayout.Button("マージ可能なSkinnedMeshを検索"))
            {
                FindMergableSkinnedMeshes();
            }
        }
    }

    private void DrawMeshGroupList()
    {
        if (meshGroups.Count == 0) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("マージ可能なSkinnedMeshグループ", EditorStyles.boldLabel);
            
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

        for (int i = 0; i < meshGroups.Count; i++)
        {
            DrawMeshGroup(meshGroups[i], i);
        }

        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(3);
        EditorGUILayout.EndVertical();
    }

    private void DrawMeshGroup(MeshGroup group, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        
        var hasAnySelected = group.rendererSelection.Any(selected => selected);
        var hasAllSelected = group.rendererSelection.All(selected => selected);
        EditorGUI.showMixedValue = hasAnySelected && !hasAllSelected;
        
        EditorGUI.BeginChangeCheck();
        group.isSelected = EditorGUILayout.Toggle(hasAllSelected, GUILayout.Width(16));
        if (EditorGUI.EndChangeCheck())
        {
            for (int i = 0; i < group.rendererSelection.Count; i++)
            {
                group.rendererSelection[i] = group.isSelected;
            }
        }
        EditorGUI.showMixedValue = false;
        
        group.isFoldout = EditorGUILayout.Foldout(
            group.isFoldout, 
            $"- {group.groupDescription} -", 
            true
        );

        if (GUILayout.Button("グループを表示", GUILayout.Width(100)))
        {
            Selection.objects = group.Renderers.Select(r => r.gameObject).Cast<Object>().ToArray();
            EditorGUIUtility.PingObject(group.Renderers[0].gameObject);
        }
        EditorGUILayout.EndHorizontal();

        if (group.isFoldout)
        {
            for (int i = 0; i < group.Renderers.Count; i++)
            {
                var renderer = group.Renderers[i];
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Space(20);
                EditorGUI.BeginChangeCheck();
                bool newSelected = EditorGUILayout.Toggle(group.rendererSelection[i], GUILayout.Width(16));
                if (EditorGUI.EndChangeCheck())
                {
                    group.rendererSelection[i] = newSelected;
                }

                GUILayout.Space(2);
                EditorGUILayout.ObjectField(renderer, renderer.GetType(), true);

                if (GUILayout.Button("表示", GUILayout.Width(80)))
                {
                    Selection.activeGameObject = renderer.gameObject;
                    EditorGUIUtility.PingObject(renderer.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawReferenceSettings()
    {
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newReference = EditorGUILayout.ObjectField(
                    "参照先",
                    referenceRenderer, 
                    typeof(SkinnedMeshRenderer), 
                    true
                ) as SkinnedMeshRenderer;

                if (check.changed && newReference != referenceRenderer)
                {
                    referenceRenderer = newReference;
                    if (referenceRenderer != null)
                    {
                        commonRootBone = referenceRenderer.rootBone;
                        commonProbeAnchor = referenceRenderer.probeAnchor;
                    }
                }
            }

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Root Bone", commonRootBone, typeof(Transform), true);
                EditorGUILayout.ObjectField("Anchor Override", commonProbeAnchor, typeof(Transform), true);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawMergeButton()
    {
        EditorGUILayout.Space(2);
        if (GUILayout.Button("選択したグループをMergeSkinnedMesh"))
        {
            SetupMergeSkinnedMesh();
        }
    }

    private void FindMergableSkinnedMeshes()
    {
        if (!InitializeAvatarOptimizerTypes())
        {
            return;
        }
        
        meshGroups.Clear();
        
        if (excludeFXToggled)
        {
            CollectFXToggledObjects();
        }
        
        if (excludeMAControlled)
        {
            CollectMAControlledObjects();
        }

        if (excludeLilycal)
        {
            CollectLilycalControlledObjects();
        }

        var validSkinnedMeshes = CollectValidSkinnedMeshes();
        CreateSkinnedMeshGroups(validSkinnedMeshes);

        SetInitialSkinnedMeshReference();
        
        if (meshGroups.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "検索結果", 
                "マージ可能なSkinnedMeshが見つかりませんでした。\n以下をご確認ください：\n\n- 除外設定を確認する\n- 複数のSkinnedMeshが存在するか確認する", 
                "OK");
        }
    }

    private void AnalyzeAnimatorController(UnityEditor.Animations.AnimatorController controller)
    {
        if (controller == null) return;

        foreach (var layer in controller.layers)
        {
            AnalyzeStateMachine(layer.stateMachine);
        }
    }

    private void AnalyzeStateMachine(UnityEditor.Animations.AnimatorStateMachine stateMachine)
    {
        if (stateMachine == null) return;

        foreach (var state in stateMachine.states)
        {
            AnalyzeState(state.state);
            }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            AnalyzeStateMachine(subStateMachine.stateMachine);
        }
    }

    private void AnalyzeState(UnityEditor.Animations.AnimatorState state)
    {
        if (state?.motion == null) return;

        var clip = state.motion as AnimationClip;
        if (clip == null) return;

        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.propertyName == "m_IsActive")
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                
                if (curve.keys.Any(key => Mathf.Approximately(key.value, 0f)))
                {
                    var targetObject = FindObjectFromPath(binding.path);
                    if (targetObject != null)
                    {
                        AddObjectAndChildren(targetObject, CollectionType.FXToggled);
                    }
                }
            }
        }
    }

    private List<Renderer> CollectValidSkinnedMeshes()
    {
        return targetObject.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !ShouldSkipSkinnedMesh(renderer))
            .ToList();
    }

    private void CreateSkinnedMeshGroups(List<Renderer> renderers)
    {
        var groupedRenderers = renderers
            .GroupBy(r => r.transform.parent)
            .Where(g => g.Key != null);

        foreach (var group in groupedRenderers)
        {
            var meshGroup = new MeshGroup
            {
                Renderers = group.ToList(),
                commonParent = group.Key?.gameObject,
                groupDescription = GenerateSkinnedMeshGroupDescription(group.Key)
            };
            meshGroup.rendererSelection = new List<bool>(new bool[meshGroup.Renderers.Count].Select(_ => true));
            meshGroups.Add(meshGroup);
        }
    }

    private void SetInitialSkinnedMeshReference()
    {
        var firstSkinnedMesh = meshGroups
            .SelectMany(g => g.Renderers)
            .OfType<SkinnedMeshRenderer>()
            .FirstOrDefault();

        if (firstSkinnedMesh != null)
        {
            referenceRenderer = firstSkinnedMesh;
            commonRootBone = firstSkinnedMesh.rootBone;
            commonProbeAnchor = firstSkinnedMesh.probeAnchor;
        }
    }

    private void CollectFXToggledObjects()
    {
        FXToggledObjects = new HashSet<GameObject>();

        var avatarDescriptor = targetObject.GetComponent<VRCAvatarDescriptor>();
        if (avatarDescriptor == null) return;

        var fxController = avatarDescriptor.baseAnimationLayers
            .FirstOrDefault(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX)
            .animatorController;

        if (fxController == null) return;

        AnalyzeAnimatorController(fxController as UnityEditor.Animations.AnimatorController);
    }

    private void CollectMAControlledObjects()
    {
        MAControlledPaths = new HashSet<string>();
        var components = targetObject.GetComponentsInChildren<Component>(true);
        
        foreach (var component in components)
        {
            if (component.GetType().FullName == "nadena.dev.modular_avatar.core.ModularAvatarObjectToggle")
            {
                ProcessMAObjectToggle(component);
            }
            else if (component.GetType().FullName == "nadena.dev.modular_avatar.core.ModularAvatarMeshSettings")
            {
                ProcessMAMeshSettings(component);
            }
        }
    }

    private void ProcessMAObjectToggle(Component component)
    {
        var so = new SerializedObject(component);
        var objectsArray = so.FindProperty("m_objects");
        if (objectsArray == null || !objectsArray.isArray) return;

        for (int i = 0; i < objectsArray.arraySize; i++)
        {
            var element = objectsArray.GetArrayElementAtIndex(i);
            var targetObjProp = element.FindPropertyRelative("Object.targetObject");
            if (targetObjProp?.objectReferenceValue is GameObject targetObj)
            {
                AddObjectAndChildren(targetObj, CollectionType.MAControlled);
            }
        }
    }

    private void ProcessMAMeshSettings(Component component)
    {
        var so = new SerializedObject(component);
        var rendererProp = so.FindProperty("renderer");
        if (rendererProp?.objectReferenceValue is Renderer targetRenderer)
        {
            var path = GetGameObjectPath(targetRenderer.transform);
            MAControlledPaths.Add(path);
        }
    }

    private void CollectLilycalControlledObjects()
    {
        LilycalControlledObjects = new HashSet<GameObject>();
        
        var components = targetObject.GetComponentsInChildren<Component>(true);
        
        foreach (var component in components)
        {
            if (component == null) continue;
            
            string typeName = component.GetType().FullName;
            if (!typeName.Contains("lilycal")) continue;
            
            switch (typeName)
            {
                case "jp.lilxyzw.lilycalinventory.runtime.Prop":
                    AddObjectAndChildren(component.gameObject, CollectionType.Lilycal);
                    break;

                case "jp.lilxyzw.lilycalinventory.runtime.ItemToggler":
                    ProcessLilycalItemToggler(component);
                    break;

                case "jp.lilxyzw.lilycalinventory.runtime.AutoDresser":
                    ProcessLilycalAutoDresser(component);
                    break;

                case "jp.lilxyzw.lilycalinventory.runtime.CostumeChanger":
                    ProcessLilycalCostumeChanger(component);
                    break;
            }
        }
    }

    private void ProcessLilycalItemToggler(Component component)
    {
        var so = new SerializedObject(component);
        var parameterProp = so.FindProperty("parameter");
        if (parameterProp != null)
        {
            var objectsProp = parameterProp.FindPropertyRelative("objects");
            ProcessItemsProperty(objectsProp);
        }
    }

    private void ProcessLilycalAutoDresser(Component component)
    {
        var so = new SerializedObject(component);
        var parameterProp = so.FindProperty("parameter");
        if (parameterProp != null)
        {
            var objectsProp = parameterProp.FindPropertyRelative("objects");
            ProcessItemsProperty(objectsProp);
        }
    }

    private void ProcessLilycalCostumeChanger(Component component)
    {
        var so = new SerializedObject(component);
        var costumesProp = so.FindProperty("costumes");
        if (costumesProp != null && costumesProp.isArray)
        {
            for (int i = 0; i < costumesProp.arraySize; i++)
            {
                var costumeElement = costumesProp.GetArrayElementAtIndex(i);
                var parametersProp = costumeElement.FindPropertyRelative("parametersPerMenu");
                var objectsProp = parametersProp?.FindPropertyRelative("objects");
                ProcessItemsProperty(objectsProp);
            }
        }
    }

    private void ProcessItemsProperty(SerializedProperty objectsProp)
    {
        if (objectsProp != null && objectsProp.isArray)
        {
            for (int i = 0; i < objectsProp.arraySize; i++)
            {
                var itemElement = objectsProp.GetArrayElementAtIndex(i);
                var objProp = itemElement.FindPropertyRelative("obj");
                if (objProp?.objectReferenceValue is GameObject targetObj)
                {
                    AddObjectAndChildren(targetObj, CollectionType.Lilycal);
                }
            }
        }
    }

    private void SetupMergeSkinnedMesh()
    {
        InitializeAvatarOptimizerTypes();

        if (targetObject == null || meshGroups.Count == 0 || mergeSkinnedMeshType == null)
        {
            Debug.LogError($"必要なコンポーネントが見つかりません\nTarget: {targetObject != null}\nGroups: {meshGroups.Count}\nType: {mergeSkinnedMeshType != null}");
            return;
        }

        var selectedSkinnedMeshes = meshGroups
            .Where(g => g.isSelected)
            .SelectMany((g, i) => g.Renderers
                .Where((r, index) => g.rendererSelection[index]))
            .ToList();

        if (!selectedSkinnedMeshes.Any())
        {
            Debug.LogError("選択されたSkinnedMeshがありません");
            return;
        }

        Undo.RegisterCompleteObjectUndo(targetObject, "Setup Merge SkinnedMesh");
        var mergeTarget = CreateSkinnedMeshMergeTarget();
        SetupSkinnedMeshMergeSettings(mergeTarget, selectedSkinnedMeshes);
    }

    private GameObject CreateSkinnedMeshMergeTarget()
    {
        string baseName = "MergeSkinnedMesh_Auto";
        string newName = AAOMergeHelper.GetUniqueName(targetObject.transform, baseName);
        GameObject mergeTarget = new GameObject(newName);
        Undo.RegisterCreatedObjectUndo(mergeTarget, "Create Merge SkinnedMesh Target");
        
        mergeTarget.transform.SetParent(targetObject.transform, false);
        mergeTarget.transform.localPosition = Vector3.zero;
        mergeTarget.transform.localRotation = Quaternion.identity;
        mergeTarget.transform.localScale = Vector3.one;

        var skinnedMeshRenderer = mergeTarget.AddComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.rootBone = commonRootBone;
        skinnedMeshRenderer.probeAnchor = commonProbeAnchor;

        return mergeTarget;
    }

    private void SetupSkinnedMeshMergeSettings(GameObject target, List<Renderer> selectedSkinnedMeshes)
    {
        var mergeSkinnedMesh = Undo.AddComponent(target, mergeSkinnedMeshType);
        var serializedObject = new SerializedObject(mergeSkinnedMesh);

        var skinnedMeshes = selectedSkinnedMeshes.OfType<SkinnedMeshRenderer>().ToList();
        var staticMeshes = selectedSkinnedMeshes.OfType<MeshRenderer>().ToList();

        SetupRendererSet(serializedObject, "renderersSet", skinnedMeshes);
        SetupRendererSet(serializedObject, "staticRenderersSet", staticMeshes);

        SetupSkinnedMeshMergeDefaults(serializedObject);

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        Selection.activeGameObject = target;
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(target);
    }

    private void SetupSkinnedMeshMergeDefaults(SerializedObject serializedObject)
    {
        SetProperty(serializedObject, "removeEmptyRendererObject", true);
        SetProperty(serializedObject, "skipEnablementMismatchedRenderers", true);
        SetProperty(serializedObject, "copyEnablementAnimation", false);
    }

    private void SetupRendererSet<T>(SerializedObject serializedObject, string propertyName, List<T> renderers) where T : Renderer
    {
        var renderersSetProperty = serializedObject.FindProperty(propertyName);
        if (renderersSetProperty != null)
        {
            var mainSetProperty = renderersSetProperty.FindPropertyRelative("mainSet");
            if (mainSetProperty != null)
            {
                mainSetProperty.arraySize = renderers.Count;
                for (int i = 0; i < renderers.Count; i++)
                {
                    mainSetProperty.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                }
            }
        }
    }

    private enum CollectionType
    {
        FXToggled,
        Lilycal,
        MAControlled
    }

    private void AddObjectAndChildren(GameObject targetObj, CollectionType collectionType)
    {
        switch (collectionType)
        {
            case CollectionType.FXToggled:
                FXToggledObjects.Add(targetObj);
                foreach (var child in targetObj.GetComponentsInChildren<Transform>(true))
                {
                    FXToggledObjects.Add(child.gameObject);
                }
                break;

            case CollectionType.Lilycal:
                LilycalControlledObjects.Add(targetObj);
                foreach (var child in targetObj.GetComponentsInChildren<Transform>(true))
                {
                    LilycalControlledObjects.Add(child.gameObject);
                }
                break;

            case CollectionType.MAControlled:
                var path = GetGameObjectPath(targetObj.transform);
                MAControlledPaths.Add(path);
                foreach (var child in targetObj.GetComponentsInChildren<Transform>(true))
                {
                    var childPath = GetGameObjectPath(child);
                    MAControlledPaths.Add(childPath);
                }
                break;
        }
    }

    private void SelectAllGroups(bool select)
    {
        foreach (var group in meshGroups)
        {
            group.isSelected = select;
            for (int i = 0; i < group.rendererSelection.Count; i++)
            {
                group.rendererSelection[i] = select;
            }
        }
    }

    private bool ShouldSkipSkinnedMesh(Renderer renderer)
    {
        if (mergeSkinnedMeshType == null) return true;

        var obj = renderer.gameObject;
        var path = GetGameObjectPath(obj.transform);

        if (obj == targetObject || !obj.activeInHierarchy || 
            obj.GetComponent(mergeSkinnedMeshType) != null)
            return true;

        Transform current = obj.transform;
        while (current != null && current != targetObject.transform)
        {
            if (current.gameObject.CompareTag("EditorOnly"))
                return true;
            current = current.parent;
        }

        current = obj.transform;
        while (current != null && current != targetObject.transform)
        {
            if (excludeFXToggled && FXToggledObjects?.Contains(current.gameObject) == true)
                return true;
            current = current.parent;
        }

        current = obj.transform;
        while (current != null && current != targetObject.transform)
        {
            var currentPath = GetGameObjectPath(current);
            if (excludeMAControlled && MAControlledPaths?.Any(controlledPath => 
                ArePathsEqual(currentPath, controlledPath)) == true)
                return true;
            current = current.parent;
        }

        current = obj.transform;
        while (current != null && current != targetObject.transform)
        {
            if (excludeLilycal && LilycalControlledObjects?.Contains(current.gameObject) == true)
                return true;
            current = current.parent;
        }

        return false;
    }

    private string GetGameObjectPath(Transform transform)
    {
        var pathParts = new List<string>();
        var current = transform;
        
        while (current != null && current != targetObject.transform)
        {
            pathParts.Insert(0, current.gameObject.name);
            current = current.parent;
        }
        
        return string.Join("/", pathParts);
    }

    private bool ArePathsEqual(string path1, string path2)
    {
        var path1Parts = path1.Split('/');
        var path2Parts = path2.Split('/');

        var path1Rest = string.Join("/", path1Parts.Skip(1));
        var path2Rest = string.Join("/", path2Parts.Skip(1));

        return path1Rest == path2Rest;
    }

    private GameObject FindObjectFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return targetObject;

        return targetObject.transform.Find(path)?.gameObject;
    }


    private string GenerateSkinnedMeshGroupDescription(Transform parent)
    {
        return $"'{parent?.name ?? "Root"}' の配下";
    }

    private void SetProperty(SerializedObject serializedObject, string propertyName, bool value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }
}
