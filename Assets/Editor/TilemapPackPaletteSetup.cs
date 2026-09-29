#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Imports the included tile pack, preserves the user's existing ground tiles,
/// and creates category palettes that can be used directly in the Tile Palette window.
/// </summary>
public static class TilemapPackPaletteSetup
{
    private const string ArtRoot = "Assets/Sprites/Tilemaps/Art";
    private const string TiledTilesetRoot = "Assets/Sprites/Tilemaps/Tiled/Tilesets";
    private const string OutputRoot = "Assets/Art/Tilemaps/PackUnity";
    private const string GameScenePath = "Assets/Scenes/Jogo.unity";
    private const string GroundAtlasPath = "Assets/Sprites/Tilemaps/Art/Ground Tileset/Tileset_Ground.png";
    private const int PixelsPerUnit = 16;
    private const float PaletteAnimationFrameRate = 12f;

    private sealed class AnimationFrameDefinition
    {
        public int TileId;
        public int DurationMilliseconds;
    }

    private sealed class AnimationDefinition
    {
        public int TileId;
        public readonly List<AnimationFrameDefinition> Frames = new List<AnimationFrameDefinition>();
    }

    private sealed class GridSheetDefinition
    {
        public string AssetPath;
        public string BaseName;
        public int CellWidth;
        public int CellHeight;
        public int Columns;
        public int TileCount;
        public int Spacing;
        public int Margin;
        public string PaletteKey;
        public string PaletteTitle;
        public string TileFolder;
        public bool SkipTilePalette;
        public bool SkipSpriteSlicing;
        public bool IsManualDoorSheet;
        public readonly List<AnimationDefinition> Animations = new List<AnimationDefinition>();
        public readonly HashSet<int> VisibleTileIds = new HashSet<int>();
    }

    private sealed class PaletteEntry
    {
        public TileBase Tile;
        public Sprite Sprite;
        public int SourceIndex;
    }

    private static readonly string[] PaletteFolderKeys =
    {
        "01_Terreno", "02_Estradas", "03_Agua_e_Areia", "04_Encostas_Simples",
        "05_Construcoes", "06_Vegetacao", "07_Rochas", "08_Decoracao",
        "09_Sombras", "10_Animacoes", "11_Quadros_de_Porta",
        "12_Atlas_Construcoes", "13_Atlas_Decoracao", "14_Atlas_Rochas", "15_Atlas_Vegetacao"
    };

    [MenuItem("Prototipo/Preparar paletas do pacote de tiles")]
    public static void PreparePalettes()
    {
        try
        {
            EnsureOutputFolders();

            List<GridSheetDefinition> sheets = ReadGridSheetsFromTiled();
            AddDoorFrameSheets(sheets);
            ConfigureAllPngImporters(sheets);
            SliceGridSheets(sheets);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            MoveExistingGroundAssetsIntoPackFolder();

            int tileAssetFilesCreated = 0;
            int palettesCreatedOrUpdated = 0;
            var paletteEntries = new Dictionary<string, List<PaletteEntry>>();

            foreach (GridSheetDefinition sheet in sheets)
            {
                if (sheet.SkipTilePalette || string.IsNullOrEmpty(sheet.PaletteKey))
                    continue;

                List<PaletteEntry> entries = CreateGridTileAssets(sheet, ref tileAssetFilesCreated);
                if (entries.Count == 0)
                    continue;

                if (!paletteEntries.ContainsKey(sheet.PaletteKey))
                    paletteEntries.Add(sheet.PaletteKey, entries);
                else
                    paletteEntries[sheet.PaletteKey].AddRange(entries);
            }

            AddIndividualObjectPalettes(paletteEntries, ref tileAssetFilesCreated);
            AddAnimatedTiles(sheets, paletteEntries, ref tileAssetFilesCreated);

            foreach (KeyValuePair<string, List<PaletteEntry>> pair in paletteEntries)
            {
                if (pair.Value.Count == 0)
                    continue;

                string title = GetPaletteTitle(pair.Key);
                CreateOrUpdatePalette(pair.Key, title, pair.Value);
                palettesCreatedOrUpdated++;
            }

            bool sceneLayersReady = EnsureMapPaintLayers();
            WritePaletteGuide(sheets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string sceneMessage = sceneLayersReady
                ? "Também deixei camadas separadas de Tilemap na cena Jogo."
                : "Abra a cena Jogo e execute o comando novamente para criar as camadas de pintura.";
            string message = "Paletas preparadas: " + palettesCreatedOrUpdated +
                " categorias. Novos arquivos Tile: " + tileAssetFilesCreated +
                ".\n\n" + sceneMessage +
            "\n\nO atlas de encosta detalhada fica no Project como imagem de referência: são 4.096 células com regras AutoMap do Tiled, então ele não é fatiado nem vira uma Palette comum.";

            Debug.Log("[Tilemap Pack] " + message.Replace("\n", " "));
            EditorUtility.DisplayDialog("Paletas do pacote prontas", message, "Fechar");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "Falha ao preparar as paletas",
                "O Unity encontrou um problema durante a preparação. O erro completo está no Console. Nenhum tilemap pintado foi apagado.\n\n" + exception.Message,
                "Fechar");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static List<GridSheetDefinition> ReadGridSheetsFromTiled()
    {
        var result = new List<GridSheetDefinition>();
        string tiledAbsolute = AssetPathToAbsolute(TiledTilesetRoot);
        if (!Directory.Exists(tiledAbsolute))
            throw new DirectoryNotFoundException("Pasta de Tilesets Tiled não encontrada: " + TiledTilesetRoot);

        foreach (string tsxFile in Directory.GetFiles(tiledAbsolute, "*.tsx", SearchOption.AllDirectories))
        {
            XDocument document = XDocument.Load(tsxFile);
            XElement root = document.Root;
            XElement image = root == null ? null : root.Element("image");
            if (root == null || image == null)
                continue;

            string source = (string)image.Attribute("source");
            if (string.IsNullOrWhiteSpace(source))
                continue;

            string sourceAbsolute = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tsxFile), source));
            string sourceAssetPath = AbsolutePathToAssetPath(sourceAbsolute);
            if (string.IsNullOrEmpty(sourceAssetPath) || !File.Exists(sourceAbsolute))
                continue;

            int columns = ReadIntAttribute(root, "columns", 0);
            int tileCount = ReadIntAttribute(root, "tilecount", 0);
            int tileWidth = ReadIntAttribute(root, "tilewidth", 0);
            int tileHeight = ReadIntAttribute(root, "tileheight", 0);
            if (columns <= 0 || tileCount <= 0 || tileWidth <= 0 || tileHeight <= 0)
                continue;

            var sheet = new GridSheetDefinition
            {
                AssetPath = sourceAssetPath,
                BaseName = Path.GetFileNameWithoutExtension(sourceAssetPath),
                CellWidth = tileWidth,
                CellHeight = tileHeight,
                Columns = columns,
                TileCount = tileCount,
                Spacing = ReadIntAttribute(root, "spacing", 0),
                Margin = ReadIntAttribute(root, "margin", 0)
            };

            AssignPalette(sheet);
            ReadAnimationDefinitions(root, sheet);

            GridSheetDefinition duplicate = result.FirstOrDefault(item => item.AssetPath == sourceAssetPath);
            if (duplicate == null)
            {
                result.Add(sheet);
            }
            else
            {
                if (duplicate.CellWidth != sheet.CellWidth || duplicate.CellHeight != sheet.CellHeight ||
                    duplicate.Columns != sheet.Columns || duplicate.TileCount != sheet.TileCount)
                {
                    Debug.LogWarning("[Tilemap Pack] O pacote usa mais de uma configuração para " + sourceAssetPath + ". Mantive a primeira definição.");
                }
                duplicate.Animations.AddRange(sheet.Animations);
                if (string.IsNullOrEmpty(duplicate.PaletteKey) && !string.IsNullOrEmpty(sheet.PaletteKey))
                {
                    duplicate.PaletteKey = sheet.PaletteKey;
                    duplicate.PaletteTitle = sheet.PaletteTitle;
                    duplicate.TileFolder = sheet.TileFolder;
                }
            }
        }

        return result;
    }

    private static void AssignPalette(GridSheetDefinition sheet)
    {
        string name = sheet.BaseName;
        switch (name)
        {
            case "Tileset_Ground": SetPalette(sheet, "01_Terreno", "01 - Terreno", "01_Terreno"); return;
            case "Tileset_Road": SetPalette(sheet, "02_Estradas", "02 - Estradas", "02_Estradas"); return;
            case "Tileset_Water": SetPalette(sheet, "03_Agua_e_Areia", "03 - Água e areia", "03_Agua_e_Areia"); return;
            case "Tileset_RockSlope_Simple": SetPalette(sheet, "04_Encostas_Simples", "04 - Encostas simples", "04_Encostas_Simples"); return;
            case "Tileset_Shadow": SetPalette(sheet, "09_Sombras", "09 - Sombras", "09_Sombras"); return;
            case "Atlas_Buildings": SetPalette(sheet, "12_Atlas_Construcoes", "Avançada - partes do atlas de construções", "12_Atlas_Construcoes"); return;
            case "Atlas_Props": SetPalette(sheet, "13_Atlas_Decoracao", "Avançada - partes do atlas de decoração", "13_Atlas_Decoracao"); return;
            case "Atlas_Rocks": SetPalette(sheet, "14_Atlas_Rochas", "Avançada - partes do atlas de rochas", "14_Atlas_Rochas"); return;
            case "Atlas_Trees_Bushes": SetPalette(sheet, "15_Atlas_Vegetacao", "Avançada - partes do atlas de vegetação", "15_Atlas_Vegetacao"); return;
            case "Tileset_RockSlope":
                sheet.SkipTilePalette = true;
                sheet.SkipSpriteSlicing = true;
                return;
        }

        if (name.StartsWith("Animation_", StringComparison.Ordinal) ||
            name.StartsWith("Flowers_", StringComparison.Ordinal))
        {
            SetPalette(sheet, "10_Animacoes", "10 - Animações", "10_Animacoes");
        }
    }

    private static void SetPalette(GridSheetDefinition sheet, string key, string title, string tileFolder)
    {
        sheet.PaletteKey = key;
        sheet.PaletteTitle = title;
        sheet.TileFolder = tileFolder;
    }

    private static void ReadAnimationDefinitions(XElement root, GridSheetDefinition sheet)
    {
        foreach (XElement tileElement in root.Elements("tile"))
        {
            XElement animationElement = tileElement.Element("animation");
            if (animationElement == null)
                continue;

            var animation = new AnimationDefinition { TileId = ReadIntAttribute(tileElement, "id", -1) };
            foreach (XElement frameElement in animationElement.Elements("frame"))
            {
                animation.Frames.Add(new AnimationFrameDefinition
                {
                    TileId = ReadIntAttribute(frameElement, "tileid", -1),
                    DurationMilliseconds = ReadIntAttribute(frameElement, "duration", 100)
                });
            }

            if (animation.TileId >= 0 && animation.Frames.Count > 1)
                sheet.Animations.Add(animation);
        }
    }

    private static void AddDoorFrameSheets(List<GridSheetDefinition> sheets)
    {
        AddManualSheet(sheets, "Assets/Sprites/Tilemaps/Art/Buildings/Animations/Door_Normal_Wood.png", 16, 26, 4);
        AddManualSheet(sheets, "Assets/Sprites/Tilemaps/Art/Buildings/Animations/Door_Small_Wood.png", 16, 20, 4);
    }

    private static void AddManualSheet(List<GridSheetDefinition> sheets, string path, int cellWidth, int cellHeight, int columns)
    {
        if (!File.Exists(AssetPathToAbsolute(path)))
            return;

        if (sheets.Any(sheet => sheet.AssetPath == path))
            return;

        sheets.Add(new GridSheetDefinition
        {
            AssetPath = path,
            BaseName = Path.GetFileNameWithoutExtension(path),
            CellWidth = cellWidth,
            CellHeight = cellHeight,
            Columns = columns,
            TileCount = columns,
            PaletteKey = "11_Quadros_de_Porta",
            PaletteTitle = "11 - Portas (quadros individuais)",
            TileFolder = "11_Quadros_de_Porta",
            IsManualDoorSheet = true
        });
    }

    private static void ConfigureAllPngImporters(List<GridSheetDefinition> sheets)
    {
        var sheetsByPath = sheets.ToDictionary(sheet => sheet.AssetPath, sheet => sheet);
        string artAbsolute = AssetPathToAbsolute(ArtRoot);
        string[] pngFiles = Directory.GetFiles(artAbsolute, "*.png", SearchOption.AllDirectories);
        int completed = 0;

        foreach (string pngFile in pngFiles)
        {
            string assetPath = AbsolutePathToAssetPath(pngFile);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            GridSheetDefinition sheet;
            bool multiple = sheetsByPath.TryGetValue(assetPath, out sheet) && !sheet.SkipSpriteSlicing;
            bool centeredPivot = assetPath.Contains("/Shadows/");
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = false;
            changed |= SetIfDifferent(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            changed |= SetIfDifferent(importer.spriteImportMode != (multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single), () => importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single);
            changed |= SetIfDifferent(Mathf.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 0.001f, () => importer.spritePixelsPerUnit = PixelsPerUnit);
            changed |= SetIfDifferent(importer.filterMode != FilterMode.Point, () => importer.filterMode = FilterMode.Point);
            changed |= SetIfDifferent(importer.textureCompression != TextureImporterCompression.Uncompressed, () => importer.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetIfDifferent(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            changed |= SetIfDifferent(importer.wrapMode != TextureWrapMode.Clamp, () => importer.wrapMode = TextureWrapMode.Clamp);
            changed |= SetIfDifferent(!importer.alphaIsTransparency, () => importer.alphaIsTransparency = true);

            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            bool textureSettingsChanged = false;
            if (textureSettings.spriteMeshType != SpriteMeshType.FullRect)
            {
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                textureSettingsChanged = true;
            }

            if (!multiple)
            {
                Vector2 desiredPivot = centeredPivot ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
                bool pivotSettingsDiffer = textureSettings.spriteAlignment != (int)SpriteAlignment.Custom ||
                    (textureSettings.spritePivot - desiredPivot).sqrMagnitude > 0.000001f;
                if (pivotSettingsDiffer)
                {
                    textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                    textureSettings.spritePivot = desiredPivot;
                    textureSettingsChanged = true;
                }
            }

            if (textureSettingsChanged)
            {
                importer.SetTextureSettings(textureSettings);
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();

            completed++;
            if (completed % 15 == 0 || completed == pngFiles.Length)
                EditorUtility.DisplayProgressBar("Preparando importação", "Configurando imagens do pacote", (float)completed / Math.Max(1, pngFiles.Length));
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static bool SetIfDifferent(bool shouldChange, Action apply)
    {
        if (!shouldChange)
            return false;
        apply();
        return true;
    }

    private static void SliceGridSheets(List<GridSheetDefinition> sheets)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();

        for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
        {
            GridSheetDefinition sheet = sheets[sheetIndex];
            if (sheet.SkipSpriteSlicing)
                continue;
            string absolutePath = AssetPathToAbsolute(sheet.AssetPath);
            if (!File.Exists(absolutePath))
                continue;

            EditorUtility.DisplayProgressBar("Preparando sprites", "Fatiando " + sheet.BaseName, (float)sheetIndex / Math.Max(1, sheets.Count));
            int sourceHeight;
            HashSet<int> visible = FindVisibleCells(absolutePath, sheet, out sourceHeight);
            sheet.VisibleTileIds.Clear();
            sheet.VisibleTileIds.UnionWith(visible);

            TextureImporter importer = AssetImporter.GetAtPath(sheet.AssetPath) as TextureImporter;
            if (importer == null)
                continue;

            var dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
                throw new InvalidOperationException("Não foi possível acessar o Sprite Editor Data Provider de " + sheet.AssetPath);

            dataProvider.InitSpriteEditorDataProvider();
            SpriteRect[] oldRects = dataProvider.GetSpriteRects() ?? Array.Empty<SpriteRect>();
            var oldRectsByPosition = oldRects.ToDictionary(rect => RectKey(rect.rect), rect => rect);
            var rects = new SpriteRect[sheet.TileCount];

            for (int index = 0; index < sheet.TileCount; index++)
            {
                int column = index % sheet.Columns;
                int row = index / sheet.Columns;
                float x = sheet.Margin + column * (sheet.CellWidth + sheet.Spacing);
                float y = sourceHeight - sheet.Margin - sheet.CellHeight - row * (sheet.CellHeight + sheet.Spacing);
                bool bottomPivot = sheet.IsManualDoorSheet;
                var rect = new Rect(x, y, sheet.CellWidth, sheet.CellHeight);
                var spriteRect = new SpriteRect
                {
                    name = sheet.BaseName + "_" + index,
                    rect = rect,
                    alignment = bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center,
                    pivot = bottomPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };

                SpriteRect oldRect;
                spriteRect.spriteID = oldRectsByPosition.TryGetValue(RectKey(rect), out oldRect)
                    ? oldRect.spriteID
                    : GUID.Generate();
                rects[index] = spriteRect;
            }

            dataProvider.SetSpriteRects(rects);
            dataProvider.Apply();
            AssetDatabase.ImportAsset(sheet.AssetPath, ImportAssetOptions.ForceUpdate);
        }
    }

    private static HashSet<int> FindVisibleCells(string absolutePath, GridSheetDefinition sheet, out int sourceHeight)
    {
        byte[] bytes = File.ReadAllBytes(absolutePath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            if (!ImageConversion.LoadImage(texture, bytes, false))
                throw new InvalidDataException("Não foi possível ler o PNG " + absolutePath);

            int width = texture.width;
            sourceHeight = texture.height;
            Color32[] pixels = texture.GetPixels32();
            var visible = new HashSet<int>();

            for (int index = 0; index < sheet.TileCount; index++)
            {
                int column = index % sheet.Columns;
                int row = index / sheet.Columns;
                int x0 = sheet.Margin + column * (sheet.CellWidth + sheet.Spacing);
                int y0 = sourceHeight - sheet.Margin - sheet.CellHeight - row * (sheet.CellHeight + sheet.Spacing);
                int xEnd = Math.Min(width, x0 + sheet.CellWidth);
                int yEnd = Math.Min(sourceHeight, y0 + sheet.CellHeight);
                bool hasPixel = false;

                for (int y = Math.Max(0, y0); y < yEnd && !hasPixel; y++)
                {
                    int rowOffset = y * width;
                    for (int x = Math.Max(0, x0); x < xEnd; x++)
                    {
                        if (pixels[rowOffset + x].a == 0)
                            continue;
                        hasPixel = true;
                        break;
                    }
                }

                if (hasPixel)
                    visible.Add(index);
            }

            return visible;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static string RectKey(Rect rect)
    {
        return Math.Round(rect.x) + ":" + Math.Round(rect.y) + ":" + Math.Round(rect.width) + ":" + Math.Round(rect.height);
    }

    private static void MoveExistingGroundAssetsIntoPackFolder()
    {
        string legacyPaletteFolder = "Assets/Art/Placeholders/Tilemaps/Palettes";
        string newTileFolder = OutputRoot + "/Tiles/01_Terreno";
        string newPalettePath = OutputRoot + "/Palettes/01_Terreno.prefab";

        if (AssetDatabase.IsValidFolder(legacyPaletteFolder))
        {
            foreach (string assetPath in AssetDatabase.FindAssets("t:Tile", new[] { legacyPaletteFolder })
                         .Select(AssetDatabase.GUIDToAssetPath))
            {
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tile == null || tile.sprite == null || AssetDatabase.GetAssetPath(tile.sprite) != GroundAtlasPath)
                    continue;

                string destination = newTileFolder + "/" + Path.GetFileName(assetPath);
                if (assetPath == destination || AssetDatabase.LoadMainAssetAtPath(destination) != null)
                    continue;

                string error = AssetDatabase.MoveAsset(assetPath, destination);
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning("[Tilemap Pack] Não movi " + assetPath + ": " + error);
            }

            string legacyPalettePath = legacyPaletteFolder + "/Paleta_Cidade.prefab";
            if (AssetDatabase.LoadMainAssetAtPath(legacyPalettePath) != null && AssetDatabase.LoadMainAssetAtPath(newPalettePath) == null)
            {
                string error = AssetDatabase.MoveAsset(legacyPalettePath, newPalettePath);
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning("[Tilemap Pack] Mantive a Palette antiga no local original: " + error);
            }
        }

        AssetDatabase.Refresh();
    }

    private static List<PaletteEntry> CreateGridTileAssets(GridSheetDefinition sheet, ref int count)
    {
        string tileFolder = OutputRoot + "/Tiles/" + sheet.TileFolder;
        EnsureAssetFolder(tileFolder);
        Sprite[] sprites = LoadSprites(sheet.AssetPath);
        var spritesByName = sprites.ToDictionary(sprite => sprite.name, sprite => sprite, StringComparer.Ordinal);
        var entries = new List<PaletteEntry>();

        foreach (int index in sheet.VisibleTileIds.OrderBy(value => value))
        {
            string spriteName = sheet.BaseName + "_" + index;
            Sprite sprite;
            if (!spritesByName.TryGetValue(spriteName, out sprite))
            {
                Debug.LogWarning("[Tilemap Pack] Sprite fatiado não encontrado: " + spriteName + " em " + sheet.AssetPath);
                continue;
            }

            Tile tile = CreateOrUpdateTile(tileFolder, sprite, ref count);
            entries.Add(new PaletteEntry { Tile = tile, Sprite = sprite, SourceIndex = index });
        }

        return entries;
    }

    private static void AddIndividualObjectPalettes(Dictionary<string, List<PaletteEntry>> palettes, ref int count)
    {
        AddIndividualObjects("Buildings", "05_Construcoes", palettes, ref count);
        AddIndividualObjects("Trees and Bushes", "06_Vegetacao", palettes, ref count);
        AddIndividualObjects("Rocks", "07_Rochas", palettes, ref count);
        AddIndividualObjects("Props", "08_Decoracao", palettes, ref count);
        AddIndividualObjects("Shadows", "09_Sombras", palettes, ref count);
    }

    private static void AddIndividualObjects(string artFolderName, string key,
        Dictionary<string, List<PaletteEntry>> palettes, ref int count)
    {
        string folderAssetPath = ArtRoot + "/" + artFolderName;
        string folderAbsolute = AssetPathToAbsolute(folderAssetPath);
        if (!Directory.Exists(folderAbsolute))
            return;

        var entries = new List<PaletteEntry>();
        foreach (string fullPath in Directory.GetFiles(folderAbsolute, "*.png", SearchOption.TopDirectoryOnly))
        {
            string assetPath = AbsolutePathToAssetPath(fullPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                sprite = LoadSprites(assetPath).FirstOrDefault();
            }

            if (sprite == null)
                continue;

            Tile tile = CreateOrUpdateTile(OutputRoot + "/Tiles/" + key, sprite, ref count);
            entries.Add(new PaletteEntry { Tile = tile, Sprite = sprite, SourceIndex = entries.Count });
        }

        if (key == "09_Sombras" && !palettes.ContainsKey(key))
            palettes.Add(key, entries);
        else if (entries.Count > 0)
        {
            if (!palettes.ContainsKey(key))
                palettes.Add(key, entries);
            else
                palettes[key].AddRange(entries);
        }
    }

    private static void AddAnimatedTiles(List<GridSheetDefinition> sheets,
        Dictionary<string, List<PaletteEntry>> palettes, ref int count)
    {
        string tileFolder = OutputRoot + "/Tiles/10_Animacoes";
        EnsureAssetFolder(tileFolder);
        var entries = new List<PaletteEntry>();

        foreach (GridSheetDefinition sheet in sheets)
        {
            Sprite[] sprites = LoadSprites(sheet.AssetPath);
            var spritesByName = sprites.ToDictionary(sprite => sprite.name, sprite => sprite, StringComparer.Ordinal);

            foreach (AnimationDefinition animation in sheet.Animations)
            {
                var frames = new List<Sprite>();
                var durations = new List<int>();
                bool complete = true;

                foreach (AnimationFrameDefinition frame in animation.Frames)
                {
                    string spriteName = sheet.BaseName + "_" + frame.TileId;
                    Sprite sprite;
                    if (!spritesByName.TryGetValue(spriteName, out sprite))
                    {
                        complete = false;
                        break;
                    }

                    frames.Add(sprite);
                    durations.Add(Math.Max(1, frame.DurationMilliseconds));
                }

                if (!complete || frames.Count < 2)
                    continue;

                string fileName = SafeFileName(sheet.BaseName + "_Animacao_" + animation.TileId) + ".asset";
                string tilePath = tileFolder + "/" + fileName;
                AnimatedTile tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<AnimatedTile>();
                    tile.name = Path.GetFileNameWithoutExtension(fileName);
                    AssetDatabase.CreateAsset(tile, tilePath);
                    count++;
                }

                float averageSeconds = durations.Average(value => (float)value) / 1000f;
                float speedMultiplier = (1f / averageSeconds) / PaletteAnimationFrameRate;
                tile.m_AnimatedSprites = frames.ToArray();
                tile.m_MinSpeed = speedMultiplier;
                tile.m_MaxSpeed = speedMultiplier;
                tile.m_TileColliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                entries.Add(new PaletteEntry { Tile = tile, Sprite = frames[0], SourceIndex = entries.Count });
            }
        }

        if (entries.Count > 0)
            palettes["10_Animacoes"] = entries;
    }

    private static Tile CreateOrUpdateTile(string folder, Sprite sprite, ref int count)
    {
        EnsureAssetFolder(folder);
        string path = folder + "/" + SafeFileName(sprite.name) + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(tile, path);
            count++;
        }

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void CreateOrUpdatePalette(string key, string title, List<PaletteEntry> entries)
    {
        string paletteFolder = OutputRoot + "/Palettes";
        string palettePath = paletteFolder + "/" + key + ".prefab";
        EnsureAssetFolder(paletteFolder);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) == null)
        {
            GameObject created = GridPaletteUtility.CreateNewPalette(
                paletteFolder,
                key,
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic,
                Vector3.one,
                GridLayout.CellSwizzle.XYZ);

            if (created == null)
                throw new InvalidOperationException("O Unity não criou a Tile Palette " + key);
        }

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(palettePath);
        try
        {
            prefabContents.name = title;
            Grid grid = prefabContents.GetComponent<Grid>();
            if (grid != null)
                grid.cellSize = Vector3.one;

            Tilemap tilemap = prefabContents.GetComponentInChildren<Tilemap>(true);
            if (tilemap == null)
                throw new InvalidOperationException("A Palette " + key + " não contém Tilemap.");

            tilemap.ClearAllTiles();
            tilemap.animationFrameRate = PaletteAnimationFrameRate;

            if (IsGridPalette(key))
            {
                int columns = GetPaletteColumns(key);
                foreach (PaletteEntry entry in entries.OrderBy(item => item.SourceIndex))
                {
                    int x = entry.SourceIndex % Math.Max(1, columns);
                    int y = entry.SourceIndex / Math.Max(1, columns);
                    tilemap.SetTile(new Vector3Int(x, -y, 0), entry.Tile);
                }
            }
            else
            {
                PlaceObjectEntries(tilemap, entries);
            }

            tilemap.RefreshAllTiles();
            PrefabUtility.SaveAsPrefabAsset(prefabContents, palettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static bool IsGridPalette(string key)
    {
        return key == "01_Terreno" || key == "02_Estradas" || key == "03_Agua_e_Areia" ||
               key == "04_Encostas_Simples" || key == "12_Atlas_Construcoes" ||
               key == "13_Atlas_Decoracao" || key == "14_Atlas_Rochas" ||
               key == "15_Atlas_Vegetacao";
    }

    private static int GetPaletteColumns(string key)
    {
        switch (key)
        {
            case "01_Terreno": return 12;
            case "02_Estradas": return 6;
            case "03_Agua_e_Areia": return 24;
            case "04_Encostas_Simples": return 6;
            case "12_Atlas_Construcoes": return 14;
            case "13_Atlas_Decoracao": return 18;
            case "14_Atlas_Rochas": return 11;
            case "15_Atlas_Vegetacao": return 24;
            default: return 10;
        }
    }

    private static void PlaceObjectEntries(Tilemap tilemap, List<PaletteEntry> entries)
    {
        const int maxWidth = 12;
        int x = 0;
        int y = 0;
        int rowHeight = 1;

        foreach (PaletteEntry entry in entries)
        {
            float width = entry.Sprite != null ? entry.Sprite.rect.width / PixelsPerUnit : 1f;
            float height = entry.Sprite != null ? entry.Sprite.rect.height / PixelsPerUnit : 1f;
            int cellsWide = Math.Max(1, Mathf.CeilToInt(width));
            int cellsHigh = Math.Max(1, Mathf.CeilToInt(height));

            if (x > 0 && x + cellsWide > maxWidth)
            {
                x = 0;
                y += rowHeight + 1;
                rowHeight = 1;
            }

            tilemap.SetTile(new Vector3Int(x, -y, 0), entry.Tile);
            x += cellsWide + 1;
            rowHeight = Math.Max(rowHeight, cellsHigh);
        }
    }

    private static bool EnsureMapPaintLayers()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(GameScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[Tilemap Pack] Abra a cena Jogo para que o comando crie as camadas de pintura.");
            return false;
        }

        GameObject gridObject = FindInScene(scene, "Grid | Cidade Inicial");
        if (gridObject == null || gridObject.GetComponent<Grid>() == null)
        {
            Debug.LogWarning("[Tilemap Pack] Não encontrei Grid | Cidade Inicial na cena Jogo. As palettes foram criadas, mas as camadas de pintura não.");
            return false;
        }

        var layerDefinitions = new[]
        {
            new { Name = "Tilemap_Estradas", Order = 1, Individual = false },
            new { Name = "Tilemap_Agua", Order = 1, Individual = false },
            new { Name = "Tilemap_Encostas", Order = 2, Individual = false },
            new { Name = "Tilemap_Sombras", Order = 4, Individual = false },
            new { Name = "Tilemap_Objetos - atras do jogador", Order = 8, Individual = true },
            new { Name = "Tilemap_Animacoes", Order = 8, Individual = true },
            new { Name = "Tilemap_Objetos - a frente do jogador", Order = 15, Individual = true }
        };

        bool changed = false;
        foreach (var definition in layerDefinitions)
        {
            Transform existing = gridObject.transform.Find(definition.Name);
            GameObject layerObject;
            if (existing == null)
            {
                layerObject = new GameObject(definition.Name, typeof(Tilemap), typeof(TilemapRenderer));
                Undo.RegisterCreatedObjectUndo(layerObject, "Criar camada " + definition.Name);
                layerObject.transform.SetParent(gridObject.transform, false);
                changed = true;
            }
            else
            {
                layerObject = existing.gameObject;
                if (layerObject.GetComponent<Tilemap>() == null)
                {
                    layerObject.AddComponent<Tilemap>();
                    changed = true;
                }
                if (layerObject.GetComponent<TilemapRenderer>() == null)
                {
                    layerObject.AddComponent<TilemapRenderer>();
                    changed = true;
                }
            }

            Tilemap tilemap = layerObject.GetComponent<Tilemap>();
            TilemapRenderer renderer = layerObject.GetComponent<TilemapRenderer>();
            Vector3 desiredAnchor = definition.Individual ? new Vector3(0.5f, 0f, 0f) : new Vector3(0.5f, 0.5f, 0f);
            if (tilemap.tileAnchor != desiredAnchor)
            {
                tilemap.tileAnchor = desiredAnchor;
                changed = true;
            }
            if (renderer.sortingOrder != definition.Order)
            {
                renderer.sortingOrder = definition.Order;
                changed = true;
            }
            TilemapRenderer.Mode desiredMode = definition.Individual ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;
            if (renderer.mode != desiredMode)
            {
                renderer.mode = desiredMode;
                changed = true;
            }
            if (renderer.sortingLayerName != "Default")
            {
                renderer.sortingLayerName = "Default";
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return true;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static void EnsureOutputFolders()
    {
        EnsureAssetFolder(OutputRoot);
        EnsureAssetFolder(OutputRoot + "/Tiles");
        EnsureAssetFolder(OutputRoot + "/Palettes");
        foreach (string key in PaletteFolderKeys)
            EnsureAssetFolder(OutputRoot + "/Tiles/" + key);
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string name = Path.GetFileName(assetFolder);
        if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
            throw new DirectoryNotFoundException("A pasta pai do asset não existe: " + assetFolder);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static Sprite[] LoadSprites(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
    }

    private static int ReadIntAttribute(XElement element, string name, int fallback)
    {
        XAttribute attribute = element == null ? null : element.Attribute(name);
        int result;
        return attribute != null && int.TryParse(attribute.Value, out result) ? result : fallback;
    }

    private static string GetPaletteTitle(string key)
    {
        switch (key)
        {
            case "01_Terreno": return "01 - Terreno";
            case "02_Estradas": return "02 - Estradas";
            case "03_Agua_e_Areia": return "03 - Água e areia";
            case "04_Encostas_Simples": return "04 - Encostas simples";
            case "05_Construcoes": return "05 - Construções";
            case "06_Vegetacao": return "06 - Árvores e arbustos";
            case "07_Rochas": return "07 - Rochas";
            case "08_Decoracao": return "08 - Decoração";
            case "09_Sombras": return "09 - Sombras";
            case "10_Animacoes": return "10 - Animações";
            case "11_Quadros_de_Porta": return "11 - Portas (quadros individuais)";
            case "12_Atlas_Construcoes": return "Avançada - partes do atlas de construções";
            case "13_Atlas_Decoracao": return "Avançada - partes do atlas de decoração";
            case "14_Atlas_Rochas": return "Avançada - partes do atlas de rochas";
            case "15_Atlas_Vegetacao": return "Avançada - partes do atlas de vegetação";
            default: return key;
        }
    }

    private static string SafeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name.Replace('/', '_').Replace('\\', '_');
    }

    private static string AssetPathToAbsolute(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string AbsolutePathToAssetPath(string absolutePath)
    {
        string assetRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string full = Path.GetFullPath(absolutePath);
        if (!full.StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        return "Assets" + full.Substring(assetRoot.Length).Replace('\\', '/');
    }

    private static void WritePaletteGuide(List<GridSheetDefinition> sheets)
    {
        string path = AssetPathToAbsolute(OutputRoot + "/GUIA_DO_PACOTE.md");
        var text = new StringBuilder();
        text.AppendLine("# Tile palettes do pacote");
        text.AppendLine();
        text.AppendLine("Os PNGs originais continuam em `Assets/Sprites/Tilemaps/Art`. Este comando cria Tiles e Palettes em `Assets/Art/Tilemaps/PackUnity` sem pintar o mapa.");
        text.AppendLine();
        text.AppendLine("## Palettes de uso comum");
        text.AppendLine();
        text.AppendLine("- `01_Terreno`: atlas de grama e chão.");
        text.AppendLine("- `02_Estradas`: peças de estrada.");
        text.AppendLine("- `03_Agua_e_Areia`: água, margens e areia.");
        text.AppendLine("- `04_Encostas_Simples`: peças simples de encosta.");
        text.AppendLine("- `05_Construcoes`: casas, poço e portão.");
        text.AppendLine("- `06_Vegetacao`: árvores e arbustos completos.");
        text.AppendLine("- `07_Rochas`: rochas completas.");
        text.AppendLine("- `08_Decoracao`: objetos e props completos.");
        text.AppendLine("- `09_Sombras`: sombras individuais e peças do atlas de sombra.");
        text.AppendLine("- `10_Animacoes`: flores e fogo montados a partir das animações do TSX.");
        text.AppendLine("- `11_Quadros_de_Porta`: quadros individuais dos sprites de porta; a interação de abrir/fechar ainda precisa de Animator/roteiro.");
        text.AppendLine();
        text.AppendLine("## Palettes avançadas");
        text.AppendLine();
        text.AppendLine("As palettes `12` a `15` contêm as peças 16×16 dos atlas originais de construções, decoração, rochas e vegetação. Elas são úteis para montar detalhes manualmente; para casas, árvores e props inteiros, prefira as palettes de uso comum.");
        text.AppendLine();
        text.AppendLine("O atlas `Tileset_RockSlope.png` permanece como uma única imagem de referência no Project: ele tem 4.096 células e depende das regras AutoMap do Tiled, então não foi fatiado nem convertido em Palette. A versão `Tileset_RockSlope_Simple` está pronta para pintar.");
        text.AppendLine();
        text.AppendLine("## Como pintar");
        text.AppendLine();
        text.AppendLine("1. Abra `Window > 2D > Tile Palette`.");
        text.AppendLine("2. Escolha uma Palette na lista e escolha como alvo a camada Tilemap correspondente na cena.");
        text.AppendLine("3. Para chão, use `Chao_Base`; estradas, água, encostas, sombras, objetos atrás do jogador, animações e objetos à frente têm camadas próprias na Grid da cidade inicial.");
        text.AppendLine("4. Use o pincel para pintar; o conta-gotas seleciona uma peça que já esteja na cena.");
        text.AppendLine();
        text.AppendLine("Os Tiles são visuais e não recebem colliders neste preparo. As colisões de árvores, casas, muros, portas e o sistema de telhado retrátil serão uma etapa separada. Para árvores e construções, use a camada atrás ou à frente do jogador conforme o trecho que deve ficar por cima.");
        text.AppendLine();
        text.AppendLine("O script leu os arquivos `.tsx` para respeitar dimensões, células e quadros de animação do pacote. Nenhum `.tmx` de exemplo é aplicado à cena.");

        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(OutputRoot + "/GUIA_DO_PACOTE.md", ImportAssetOptions.ForceUpdate);
    }
}
#endif
