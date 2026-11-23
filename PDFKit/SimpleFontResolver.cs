using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFKit
{

    /// <summary>
    /// Official PdfSharp (6.x, cross-platform build) does NOT automatically use
    /// system/GDI fonts on any OS, including Windows - it needs an explicit
    /// IFontResolver that hands back raw font file bytes. Without registering
    /// one, "Arial" etc. will throw at render time.
    ///
    /// Point FontsFolder at a directory containing actual .ttf/.ttc files
    /// (e.g. copy them out of C:\Windows\Fonts on a Windows box, or ship
    /// open-license equivalents like Liberation Sans / Carlito alongside
    /// the app) and register this once at startup:
    ///
    ///     GlobalFontSettings.FontResolver = new SimpleFontResolver("/path/to/fonts");
    ///
    /// Do this ONCE, before the first PdfBuilder is created (e.g. in Program.cs).
    /// </summary>
    public sealed class SimpleFontResolver : IFontResolver
    {
        private readonly string _fontsFolder;

        // Maps the logical font family name used in code (e.g. "Arial") to the
        // actual font file names on disk. Adjust to match whatever files you ship.
        private readonly Dictionary<string, string> _regularFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Arial"] = "arial.ttf",
        };

        private readonly Dictionary<string, string> _boldFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Arial"] = "arialbd.ttf",
        };

        public SimpleFontResolver(string fontsFolder)
        {
            _fontsFolder = fontsFolder;
        }

        public string? DefaultFontName => "Arial";

        public byte[] GetFont(string faceName)
        {
            var path = Path.Combine(_fontsFolder, faceName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Font file '{faceName}' not found in '{_fontsFolder}'. " +
                    "Copy the required .ttf files into this folder.", path);
            }
            return File.ReadAllBytes(path);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var files = isBold ? _boldFiles : _regularFiles;

            if (!files.TryGetValue(familyName, out var fileName))
            {
                // Fall back to the default family's regular/bold file if an
                // unmapped family name is requested.
                files.TryGetValue(DefaultFontName!, out fileName);
            }

            return new FontResolverInfo(fileName ?? _regularFiles[DefaultFontName!]);
        }
    }
}

