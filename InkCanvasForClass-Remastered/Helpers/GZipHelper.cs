using System.IO;
using System.IO.Compression;

namespace InkCanvasForClass_Remastered.Helpers
{
    public static class GZipHelper
    {
        public static void CompressFileAndDelete(string path)
        {
            using FileStream originalFileStream = File.Open(path, FileMode.Open);
            using FileStream compressedFileStream = File.Create(path + ".gz");
            using GZipStream compressor = new(compressedFileStream, CompressionMode.Compress);
            originalFileStream.CopyTo(compressor);
            compressor.Close();
            originalFileStream.Close();
            File.Delete(path);
        }
    }
}