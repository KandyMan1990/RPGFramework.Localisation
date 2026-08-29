using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal static class GoogleSheetDataProvider
    {
        private const int REQUEST_TIMEOUT_SECONDS = 30;

        internal static async Task<string> GetCsv(LocalisationMaster master, LocalisationSheetAsset sheetAsset)
        {
            string csvUrl = BuildCsvUrl(master.SheetId, sheetAsset.Gid);
            if (string.IsNullOrEmpty(csvUrl))
            {
                throw new Exception($"{nameof(GoogleSheetDataProvider)}::{nameof(GetCsv)} Could not build CSV URL");
            }

            string csv = await FetchCsvSync(csvUrl);
            if (string.IsNullOrEmpty(csv))
            {
                throw new Exception($"{nameof(GoogleSheetDataProvider)}::{nameof(GetCsv)} Failed to fetch CSV");
            }

            return csv;
        }

        private static string BuildCsvUrl(string masterSheetId, string gid)
        {
            string id = masterSheetId;
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException($"{nameof(GoogleSheetDataProvider)}::{nameof(BuildCsvUrl)} Sheet id is null or empty");
            }

            if (string.IsNullOrEmpty(gid))
            {
                throw new ArgumentException($"{nameof(GoogleSheetDataProvider)}::{nameof(BuildCsvUrl)} Gid is null or empty");
            }

            if (id.Contains("docs.google.com"))
            {
                const string token = "/d/";
                int          idx   = id.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    idx += token.Length;
                    string rest = id[idx..];
                    int    end  = rest.IndexOf('/');
                    id = end >= 0 ? rest[..end] : rest;
                }
            }

            string csvUrl = $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}";

            return csvUrl;
        }

        private static async Task<string> FetchCsvSync(string url)
        {
            using UnityWebRequest req = UnityWebRequest.Get(url);

            req.timeout = REQUEST_TIMEOUT_SECONDS;

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{nameof(FetchCsvSync)} Status [{req.result}] code [{req.responseCode}] error [{req.error}] requesting [{url}]. A sheet must be shared as \"anyone with the link can view\" for CSV export to work");
            }

            string text = req.downloadHandler.text;

            // A sheet that is not link-shared answers 200 with a Google sign-in page rather than CSV, which
            // otherwise fails much later as a confusing "language code is not valid" parse error.
            if (text != null && text.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{nameof(FetchCsvSync)} [{url}] returned HTML, not CSV. The sheet is probably not shared as \"anyone with the link can view\"");
            }

            return text;
        }
    }
}