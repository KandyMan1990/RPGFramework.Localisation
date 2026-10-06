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

        /// <summary>
        /// The whole spreadsheet, every tab, as one .xlsx file. The export is anonymous, so the spreadsheet must be
        /// shared as "anyone with the link can view".
        /// </summary>
        internal static async Task<byte[]> GetWorkbookAsync(string sheetId)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{GetSpreadsheetId(sheetId)}/export?format=xlsx";

            using UnityWebRequest req = UnityWebRequest.Get(url);

            req.timeout = REQUEST_TIMEOUT_SECONDS;

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{nameof(GetWorkbookAsync)} Status [{req.result}] code [{req.responseCode}] error [{req.error}] requesting [{url}]. A spreadsheet must be shared as \"anyone with the link can view\" for its export to work");
            }

            byte[] workbook = req.downloadHandler.data;

            // A spreadsheet that is not link-shared answers 200 with a Google sign-in page, where an .xlsx file is a zip.
            if (workbook == null || workbook.Length < 2 || workbook[0] != 'P' || workbook[1] != 'K')
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{nameof(GetWorkbookAsync)} [{url}] did not return an .xlsx file. The spreadsheet is probably not shared as \"anyone with the link can view\"");
            }

            return workbook;
        }

        /// <summary>The spreadsheet's id, from either the id itself or a link to the spreadsheet.</summary>
        private static string GetSpreadsheetId(string sheetId)
        {
            if (string.IsNullOrEmpty(sheetId))
            {
                throw new ArgumentException($"{nameof(GoogleSheetDataProvider)}::{nameof(GetSpreadsheetId)} Sheet id is null or empty");
            }

            string id = sheetId;

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

            return id;
        }
    }
}