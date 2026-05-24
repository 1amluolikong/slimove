using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace Game.Core
{
    /// <summary>
    /// 资源管理器 - 负责加载和缓存游戏资源（图像、字体等）
    /// </summary>
    internal class ResourceManager
    {
        private Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();
        private string _resourcePath;

        public ResourceManager(string resourcePath = "Assets")
        {
            _resourcePath = resourcePath;
            
            // 如果路径不存在，创建它
            if (!Directory.Exists(_resourcePath))
            {
                Directory.CreateDirectory(_resourcePath);
            }
        }

        /// <summary>
        /// 加载图像资源
        /// </summary>
        public Image LoadImage(string imagePath)
        {
            // 检查缓存
            if (_imageCache.ContainsKey(imagePath))
            {
                return _imageCache[imagePath];
            }

            try
            {
                string fullPath = Path.Combine(_resourcePath, imagePath);
                
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"图像文件未找到: {fullPath}");
                }

                Image image = Image.FromFile(fullPath);
                _imageCache[imagePath] = image;
                return image;
            }
            catch (Exception ex)
            {
                throw new Exception($"加载图像失败 {imagePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载字体资源
        /// </summary>
        public Font LoadFont(string fontName, float fontSize)
        {
            string key = $"{fontName}_{fontSize}";

            if (_fontCache.ContainsKey(key))
            {
                return _fontCache[key];
            }

            try
            {
                Font font = new Font(fontName, fontSize);
                _fontCache[key] = font;
                return font;
            }
            catch (Exception ex)
            {
                throw new Exception($"加载字体失败 {fontName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取缓存的图像
        /// </summary>
        public Image GetImage(string imagePath)
        {
            if (_imageCache.ContainsKey(imagePath))
            {
                return _imageCache[imagePath];
            }
            return null;
        }

        /// <summary>
        /// 获取缓存的字体
        /// </summary>
        public Font GetFont(string fontName, float fontSize)
        {
            string key = $"{fontName}_{fontSize}";
            if (_fontCache.ContainsKey(key))
            {
                return _fontCache[key];
            }
            return null;
        }

        /// <summary>
        /// 卸载指定的图像资源
        /// </summary>
        public void UnloadImage(string imagePath)
        {
            if (_imageCache.ContainsKey(imagePath))
            {
                _imageCache[imagePath]?.Dispose();
                _imageCache.Remove(imagePath);
            }
        }

        /// <summary>
        /// 卸载所有资源
        /// </summary>
        public void UnloadAll()
        {
            foreach (var image in _imageCache.Values)
            {
                image?.Dispose();
            }
            _imageCache.Clear();

            foreach (var font in _fontCache.Values)
            {
                font?.Dispose();
            }
            _fontCache.Clear();
        }

        /// <summary>
        /// 销毁资源管理器
        /// </summary>
        public void Dispose()
        {
            UnloadAll();
        }
    }
}

