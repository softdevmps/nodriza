using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Backend.Utils
{
    public static class MenuViewGenerator
    {
        public static bool TryEnsureParentFolder(string parentTitle)
        {
            if (!IsEnabled())
                return false;

            var viewsRoot = FindViewsRoot();
            if (viewsRoot == null)
                return false;

            var parentFolderName = ToSafeName(parentTitle);
            var folderPath = Path.Combine(viewsRoot, parentFolderName);
            Directory.CreateDirectory(folderPath);

            return true;
        }

        public static bool TryCreateChildView(string parentTitle, string childTitle)
        {
            if (!IsEnabled())
                return false;

            var viewsRoot = FindViewsRoot();
            if (viewsRoot == null)
                return false;

            var parentFolderName = ToSafeName(parentTitle);
            var childFileName = $"{ToSafeName(childTitle)}.vue";
            var folderPath = Path.Combine(viewsRoot, parentFolderName);
            var filePath = Path.Combine(folderPath, childFileName);

            Directory.CreateDirectory(folderPath);

            if (File.Exists(filePath))
                return false;

            var content = BuildVueTemplate(childTitle);
            File.WriteAllText(filePath, content, new UTF8Encoding(false));

            return true;
        }

        public static string? NormalizeRoute(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return null;

            var cleaned = ruta.Trim();

            if (!cleaned.StartsWith("/"))
                cleaned = "/" + cleaned;

            if (cleaned.Length > 1)
                cleaned = cleaned.TrimEnd('/');

            return cleaned;
        }

        public static string ToSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "vista";

            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            var separatorPending = false;

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(ch))
                {
                    if (separatorPending && sb.Length > 0)
                        sb.Append('-');

                    sb.Append(char.ToLowerInvariant(ch));
                    separatorPending = false;
                }
                else
                {
                    separatorPending = true;
                }
            }

            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "vista" : result;
        }

        public static string ToSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Vista";

            var normalized = value.Normalize(NormalizationForm.FormD);
            var words = new List<string>();
            var current = new StringBuilder();

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(ch))
                {
                    current.Append(ch);
                }
                else if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
                words.Add(current.ToString());

            if (words.Count == 0)
                return "Vista";

            var result = new StringBuilder();
            foreach (var word in words)
            {
                var lower = word.ToLowerInvariant();
                result.Append(char.ToUpperInvariant(lower[0]));
                if (lower.Length > 1)
                    result.Append(lower[1..]);
            }

            return result.ToString();
        }

        private static string BuildVueTemplate(string title)
        {
            var safeTitle = WebUtility.HtmlEncode(title);

            return $@"<template>
  <v-container fluid>
    <!-- Vista auto-generada -->
    <h1>{safeTitle}</h1>
  </v-container>
</template>
";
        }

        private static string? FindViewsRoot()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "frontend", "src", "views");
                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            return null;
        }

        private static bool IsEnabled()
        {
            var flag = Environment.GetEnvironmentVariable("MENU_VIEW_GENERATOR");
            if (!string.IsNullOrWhiteSpace(flag))
                return flag.Equals("1", StringComparison.OrdinalIgnoreCase)
                    || flag.Equals("true", StringComparison.OrdinalIgnoreCase);

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }
}
