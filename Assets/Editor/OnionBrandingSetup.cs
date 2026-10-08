using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Confere uma vez por instalação as marcas usadas pela abertura e pelo executável.
/// A configuração principal fica em ProjectSettings; este script corrige referências
/// caso o Unity reimporte as imagens ao abrir o projeto em outro computador.
/// </summary>
internal static class OnionBrandingSetup
{
    private const string BrandLogoPath = "Assets/Logos/Logo da Marca.png";
    private const string BrandTypePath = "Assets/Logos/Tipografia Marca.png";
    private const string GameIconPath = "Assets/Logos/Logo do Jogo.png";

    [InitializeOnLoadMethod]
    private static void ScheduleInitialCheck()
    {
        EditorApplication.delayCall += CheckOnce;
    }

    private static void CheckOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CheckOnce;
            return;
        }

        string key = "Onion.Branding.Configured." + Application.dataPath;
        if (EditorPrefs.GetBool(key, false)) return;

        if (!HasExpectedSplash() || !HasExpectedIcon())
        {
            ApplyBranding();
        }

        if (HasExpectedSplash() && HasExpectedIcon())
        {
            EditorPrefs.SetBool(key, true);
        }
    }

    [MenuItem("Onion/Aplicar logos de abertura e ícone do EXE")]
    private static void ApplyFromMenu()
    {
        ApplyBranding();
        if (HasExpectedSplash() && HasExpectedIcon())
        {
            EditorPrefs.SetBool("Onion.Branding.Configured." + Application.dataPath, true);
        }
    }

    private static void ApplyBranding()
    {
        Sprite brand = LoadSingleSprite(BrandLogoPath);
        Sprite typography = LoadSingleSprite(BrandTypePath);
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(GameIconPath);
        if (icon == null)
        {
            AssetDatabase.ImportAsset(GameIconPath, ImportAssetOptions.ForceUpdate);
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(GameIconPath);
        }
        if (brand == null || typography == null || icon == null)
        {
            Debug.LogError("Onion: não foi possível importar uma das três imagens em Assets/Logos.");
            return;
        }

        // A marca aparece antes da tipografia. O logo obrigatório da Unity permanece abaixo.
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
        PlayerSettings.SplashScreen.logos = new[]
        {
            PlayerSettings.SplashScreenLogo.Create(2f, brand),
            PlayerSettings.SplashScreenLogo.Create(2f, typography)
        };

        // O Unity informa quantos tamanhos de ícone a plataforma Standalone espera.
        int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
        if (sizes == null || sizes.Length == 0)
        {
            Debug.LogWarning("Onion: suporte de build Standalone indisponível; ícone não aplicado.");
            return;
        }

        var icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = icon;
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
        AssetDatabase.SaveAssets();
    }

    private static Sprite LoadSingleSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }

        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.maxTextureSize < 4096)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool HasExpectedSplash()
    {
        var logos = PlayerSettings.SplashScreen.logos;
        return PlayerSettings.SplashScreen.show &&
               PlayerSettings.SplashScreen.showUnityLogo &&
               logos != null && logos.Length == 2 &&
               AssetDatabase.GetAssetPath(logos[0].logo) == BrandLogoPath &&
               AssetDatabase.GetAssetPath(logos[1].logo) == BrandTypePath;
    }

    private static bool HasExpectedIcon()
    {
        Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Application);
        if (icons == null || icons.Length == 0) return false;
        foreach (Texture2D icon in icons)
        {
            if (icon == null || AssetDatabase.GetAssetPath(icon) != GameIconPath) return false;
        }
        return true;
    }
}
