using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Game.Core
{
    internal static class AssetLoader
    {
        public static Image LoadImage(string folder, string fileName)
        {
            string imagePath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                folder,
                fileName);

            if (File.Exists(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"图片已加载: {imagePath}");
                return Image.FromFile(imagePath);
            }

            System.Diagnostics.Debug.WriteLine($"图片未找到: {imagePath}");
            return null;
        }
    }
}
