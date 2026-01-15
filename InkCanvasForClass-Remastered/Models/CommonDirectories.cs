using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkCanvasForClass_Remastered.Models
{
    public static class CommonDirectories
    {
        public static string AppRootFolderPath { get; } = "./";
        public static string AppLogFolderPath { get; } = Path.Combine(AppRootFolderPath, "Logs");
        public static string AppSavesRootFolderPath { get; internal set; } = string.Empty;
        public static string AutoSaveAnnotationStrokesFolderPath => Path.Combine(AppSavesRootFolderPath, "Auto Saved - Annotation Strokes");
        public static string AutoSaveWhiteboardStrokesFolderPath => Path.Combine(AppSavesRootFolderPath, "Auto Saved - BlackBoard Strokes");
        public static string AutoSavePresentationStrokesFolderPath => Path.Combine(AppSavesRootFolderPath, "Auto Saved - Presentations");
        public static string AutoSaveScreenshotsFolderPath => Path.Combine(AppSavesRootFolderPath, "Auto Saved - Screenshots");
        public static string UserSaveAnnotationStrokesFolderPath => Path.Combine(AppSavesRootFolderPath, "User Saved - Annotation Strokes");
        public static string UserSaveWhiteboardStrokesFolderPath => Path.Combine(AppSavesRootFolderPath, "User Saved - BlackBoard Strokes");

    }
}
