using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.Editor.LocalisationBinWriter
{
    internal static class GoogleSheetDataProvider
    {
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

            await req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"{nameof(GoogleSheetDataProvider)}::{nameof(FetchCsvSync)} Fetch error: {req.error}");
                return null;
            }

            return req.downloadHandler.text;
        }
    }
}