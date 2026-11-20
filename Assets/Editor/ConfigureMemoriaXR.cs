using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEditor.XR.OpenXR.Features;

public static class ConfigureMemoriaXR
{
    [MenuItem("Memoria Viva/Configurar OpenXR")]
    public static void Configure()
    {
        const string assetPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
        if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
        if (!AssetDatabase.IsValidFolder("Assets/XR/Settings")) AssetDatabase.CreateFolder("Assets/XR", "Settings");

        var perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(assetPath);
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, assetPath);
        }
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        EnableTarget(perTarget, BuildTargetGroup.Standalone);
        EnableTarget(perTarget, BuildTargetGroup.Android);

        // El tipo de ajustes de OpenXR es interno; su API pública crea el asset.
        Type packageType = Type.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings, Unity.XR.OpenXR.Editor");
        packageType?.GetMethod("GetOrCreateInstance", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        EnableProfiles(BuildTargetGroup.Standalone);
        EnableProfiles(BuildTargetGroup.Android);
        EditorUtility.SetDirty(perTarget);
        AssetDatabase.SaveAssets();
        Debug.Log("MEMORIA VIVA: OpenXR configurado para Windows PC VR y Android. La compilación de Android requiere el módulo Android Build Support.");
    }

    private static void EnableTarget(XRGeneralSettingsPerBuildTarget store, BuildTargetGroup target)
    {
        if (!store.HasSettingsForBuildTarget(target)) store.CreateDefaultSettingsForBuildTarget(target);
        if (!store.HasManagerSettingsForBuildTarget(target)) store.CreateDefaultManagerSettingsForBuildTarget(target);
        XRManagerSettings manager = store.ManagerSettingsForBuildTarget(target);
        manager.automaticLoading = true;
        manager.automaticRunning = true;
        if (!XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", target))
            Debug.LogWarning("No se pudo asignar OpenXR a " + target + ". Comprueba el soporte de compilación instalado.");
        EditorUtility.SetDirty(manager);
    }

    private static void EnableProfiles(BuildTargetGroup target)
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
        if (settings == null)
        {
            Debug.LogWarning("Los perfiles OpenXR de " + target + " estarán disponibles al instalar su módulo de compilación.");
            return;
        }
        var touch = settings.GetFeature<OculusTouchControllerProfile>();
        var vive = settings.GetFeature<HTCViveControllerProfile>();
        var index = settings.GetFeature<ValveIndexControllerProfile>();
        if (touch != null) touch.enabled = true;
        if (vive != null) vive.enabled = true;
        if (index != null) index.enabled = true;
        EditorUtility.SetDirty(settings);
    }
}
