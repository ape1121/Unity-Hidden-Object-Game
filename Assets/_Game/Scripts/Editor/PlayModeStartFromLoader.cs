#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayModeStartFromLoader
{
    private const string LoaderScenePath = "Assets/_Game/Scenes/Loader.unity";
    private const int TargetGameViewWidth = 1284;
    private const int TargetGameViewHeight = 2778;
    private const string TargetGameViewName = "SearchIt 1284x2778";

    static PlayModeStartFromLoader()
    {
        ApplyLoaderStartScene(logResult: false);
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void ApplyLoaderStartScene(bool logResult)
    {
        SceneAsset loaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoaderScenePath);
        if (loaderScene == null)
        {
            UnityEngine.Debug.LogWarning("PlayModeStartFromLoader: Loader scene not found at " + LoaderScenePath);
            return;
        }

        EditorSceneManager.playModeStartScene = loaderScene;
        if (logResult)
        {
            UnityEngine.Debug.Log("PlayModeStartFromLoader: Play Mode start scene set to " + LoaderScenePath);
        }
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
    {
        if (stateChange != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        TrySetGameViewSize(TargetGameViewWidth, TargetGameViewHeight, TargetGameViewName, out _);
    }

    private static bool TrySetGameViewSize(int width, int height, string displayName, out string error)
    {
        error = string.Empty;

        try
        {
            Type gameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            Type scriptableSingletonType = Type.GetType("UnityEditor.ScriptableSingleton`1,UnityEditor");
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            Type gameViewSizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            Type gameViewSizeTypeEnum = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            if (gameViewSizesType == null || scriptableSingletonType == null || gameViewType == null || gameViewSizeType == null || gameViewSizeTypeEnum == null)
            {
                error = "Missing UnityEditor GameView reflection types.";
                return false;
            }

            Type singletonType = scriptableSingletonType.MakeGenericType(gameViewSizesType);
            PropertyInfo singletonInstanceProperty = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            object gameViewSizesInstance = singletonInstanceProperty != null ? singletonInstanceProperty.GetValue(null, null) : null;
            if (gameViewSizesInstance == null)
            {
                error = "Could not access GameViewSizes singleton.";
                return false;
            }

            MethodInfo getGroupMethod = gameViewSizesType.GetMethod("GetGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getGroupMethod == null)
            {
                error = "Could not resolve GameViewSizes.GetGroup.";
                return false;
            }

            GameViewSizeGroupType groupType = ResolveCurrentGroupType(gameViewSizesType, gameViewSizesInstance);
            object group = getGroupMethod.Invoke(gameViewSizesInstance, new object[] { groupType });
            if (group == null)
            {
                error = "Could not resolve active GameView size group.";
                return false;
            }

            Type groupTypeInfo = group.GetType();
            MethodInfo getBuiltinCountMethod = groupTypeInfo.GetMethod("GetBuiltinCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo getCustomCountMethod = groupTypeInfo.GetMethod("GetCustomCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo getGameViewSizeMethod = groupTypeInfo.GetMethod("GetGameViewSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo addCustomSizeMethod = groupTypeInfo.GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (getBuiltinCountMethod == null || getCustomCountMethod == null || getGameViewSizeMethod == null || addCustomSizeMethod == null)
            {
                error = "Could not resolve GameView size group methods.";
                return false;
            }

            int targetIndex = FindGameViewSizeIndex(group, width, height, getBuiltinCountMethod, getCustomCountMethod, getGameViewSizeMethod);
            if (targetIndex < 0)
            {
                object gameViewSize = CreateFixedGameViewSize(gameViewSizeType, gameViewSizeTypeEnum, width, height, displayName, out error);
                if (gameViewSize == null)
                {
                    return false;
                }

                addCustomSizeMethod.Invoke(group, new[] { gameViewSize });
                targetIndex = FindGameViewSizeIndex(group, width, height, getBuiltinCountMethod, getCustomCountMethod, getGameViewSizeMethod);
            }

            if (targetIndex < 0)
            {
                error = "Could not find or create target GameView size.";
                return false;
            }

            EditorWindow gameViewWindow = EditorWindow.GetWindow(gameViewType);
            if (gameViewWindow == null)
            {
                error = "Could not open Game view window.";
                return false;
            }

            MethodInfo sizeSelectionMethod = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sizeSelectionMethod != null)
            {
                ParameterInfo[] parameters = sizeSelectionMethod.GetParameters();
                if (parameters.Length == 1)
                {
                    sizeSelectionMethod.Invoke(gameViewWindow, new object[] { targetIndex });
                }
                else if (parameters.Length == 2)
                {
                    sizeSelectionMethod.Invoke(gameViewWindow, new object[] { targetIndex, null });
                }
                else
                {
                    error = "Unsupported SizeSelectionCallback signature.";
                    return false;
                }
            }
            else
            {
                PropertyInfo selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (selectedSizeIndexProperty == null)
                {
                    error = "Could not set GameView selected size.";
                    return false;
                }

                selectedSizeIndexProperty.SetValue(gameViewWindow, targetIndex, null);
            }

            gameViewWindow.Repaint();
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static GameViewSizeGroupType ResolveCurrentGroupType(Type gameViewSizesType, object gameViewSizesInstance)
    {
        MethodInfo getCurrentGroupTypeMethod = gameViewSizesType.GetMethod(
            "GetCurrentGroupType",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        if (getCurrentGroupTypeMethod != null)
        {
            object result = getCurrentGroupTypeMethod.IsStatic
                ? getCurrentGroupTypeMethod.Invoke(null, null)
                : getCurrentGroupTypeMethod.Invoke(gameViewSizesInstance, null);

            if (result is GameViewSizeGroupType resolvedGroupType)
            {
                return resolvedGroupType;
            }
        }

        return GameViewSizeGroupType.Standalone;
    }

    private static int FindGameViewSizeIndex(
        object group,
        int width,
        int height,
        MethodInfo getBuiltinCountMethod,
        MethodInfo getCustomCountMethod,
        MethodInfo getGameViewSizeMethod)
    {
        int builtinCount = Convert.ToInt32(getBuiltinCountMethod.Invoke(group, null));
        int customCount = Convert.ToInt32(getCustomCountMethod.Invoke(group, null));
        int totalCount = builtinCount + customCount;

        for (int index = 0; index < totalCount; index++)
        {
            object gameViewSize = getGameViewSizeMethod.Invoke(group, new object[] { index });
            if (gameViewSize == null)
            {
                continue;
            }

            if (TryReadGameViewSizeDimensions(gameViewSize, out int currentWidth, out int currentHeight) &&
                currentWidth == width &&
                currentHeight == height)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TryReadGameViewSizeDimensions(object gameViewSize, out int width, out int height)
    {
        Type gameViewSizeType = gameViewSize.GetType();
        bool hasWidth = TryReadIntMember(gameViewSizeType, gameViewSize, "width", out width);
        bool hasHeight = TryReadIntMember(gameViewSizeType, gameViewSize, "height", out height);

        return hasWidth && hasHeight;
    }

    private static bool TryReadIntMember(Type targetType, object targetInstance, string memberName, out int value)
    {
        value = 0;

        PropertyInfo property = targetType.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null)
        {
            value = Convert.ToInt32(property.GetValue(targetInstance, null));
            return true;
        }

        FieldInfo field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            value = Convert.ToInt32(field.GetValue(targetInstance));
            return true;
        }

        return false;
    }

    private static object CreateFixedGameViewSize(
        Type gameViewSizeType,
        Type gameViewSizeTypeEnum,
        int width,
        int height,
        string displayName,
        out string error)
    {
        error = string.Empty;

        object fixedResolutionEnum = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");

        ConstructorInfo constructor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
        if (constructor != null)
        {
            return constructor.Invoke(new object[] { fixedResolutionEnum, width, height, displayName });
        }

        ConstructorInfo intConstructor = gameViewSizeType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(string) });
        if (intConstructor != null)
        {
            int fixedResolutionValue = Convert.ToInt32(fixedResolutionEnum);
            return intConstructor.Invoke(new object[] { fixedResolutionValue, width, height, displayName });
        }

        error = "Could not resolve GameViewSize constructor.";
        return null;
    }
}
#endif
