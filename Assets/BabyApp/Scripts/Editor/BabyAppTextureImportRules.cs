using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 這個專案是 3D 範本起家的，圖片匯入後預設不是 Sprite，直接拉進 UI 會用不了。
    /// 這支會在匯入時自動把 Art 資料夾裡的圖轉成 Sprite 設定，
    /// 所以 AI 生成的圖只要丟進資料夾就能用，不用手動改 Inspector。
    /// </summary>
    public class BabyAppTextureImportRules : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(BabyAppPaths.Root + "/Art")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            // 介面小圖示（箭頭之類的）壓縮後邊緣會糊掉，而且本來就只有幾 KB，不值得壓；
            // 條紋要能平鋪，所以介面圖統一用 Repeat
            bool isUiIcon = assetPath.StartsWith(BabyAppPaths.UiArt);
            importer.wrapMode = isUiIcon ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            // 大圓角底圖要九宮格切，四邊各留圓角半徑，拉成任何大小角都不變形
            if (assetPath.EndsWith("/roundrect.png"))
            {
                float r = BabyAppUiSprites.RoundRectRadius;
                importer.spriteBorder = new Vector4(r, r, r, r);
            }
            importer.maxTextureSize = isUiIcon ? 256 : 1024;
            importer.textureCompression = isUiIcon
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(BabyAppPaths.Root + "/Audio")) return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;

            // 卡片語音都是一兩秒的短音，整個解壓在記憶體裡播最快，按下去沒有延遲
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
        }
    }
}
