using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.Editor.LocalisationBinGenerator
{
    internal static class GoogleSheetDataProvider
    {
        private const int    REQUEST_TIMEOUT_SECONDS = 60;
        private const string WEB_APP_HOST            = "https://script.google.com/";

        /// <summary>
        /// The script a web app is deployed from, bound to the spreadsheet. It runs as whoever deployed it, so it reads a
        /// spreadsheet shared with nobody, and returns the same .xlsx export a shared link gives, as base64 text.
        /// </summary>
        internal const string WEB_APP_SCRIPT = @"// Deployed as a web app, this lets RPG Framework Localisation pull this spreadsheet without sharing it.
// It runs as you and returns this spreadsheet alone, as an .xlsx file. Anyone holding its URL can read the spreadsheet.
function doGet() {
  var url = 'https://docs.google.com/spreadsheets/d/' + SpreadsheetApp.getActiveSpreadsheet().getId() + '/export?format=xlsx';
  var xlsx = UrlFetchApp.fetch(url, { headers: { Authorization: 'Bearer ' + ScriptApp.getOAuthToken() } });

  return ContentService.createTextOutput(Utilities.base64Encode(xlsx.getContent()));
}
";

        /// <summary>
        /// The whole spreadsheet, every tab, as one .xlsx file. The export is anonymous, so the spreadsheet must be
        /// shared as "anyone with the link can view".
        /// </summary>
        internal static async Task<byte[]> GetWorkbookAsync(string sheetId)
        {
            string url      = $"https://docs.google.com/spreadsheets/d/{GetSpreadsheetId(sheetId)}/export?format=xlsx";
            byte[] workbook = await RequestAsync(url, nameof(GetWorkbookAsync), "A spreadsheet must be shared as \"anyone with the link can view\" for its export to work");

            // A spreadsheet that is not link-shared answers 200 with a Google sign-in page, where an .xlsx file is a zip.
            if (!IsZip(workbook))
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{nameof(GetWorkbookAsync)} [{url}] did not return an .xlsx file. The spreadsheet is probably not shared as \"anyone with the link can view\"");
            }

            return workbook;
        }

        /// <summary>
        /// The whole spreadsheet as <see cref="GetWorkbookAsync" /> gives it, through a web app deployed from
        /// <see cref="WEB_APP_SCRIPT" />, so the spreadsheet need not be shared.
        /// </summary>
        internal static async Task<byte[]> GetWorkbookFromWebAppAsync(string webAppUrl)
        {
            string url = webAppUrl?.Trim();

            if (string.IsNullOrEmpty(url) || !url.StartsWith(WEB_APP_HOST, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{nameof(GoogleSheetDataProvider)}::{nameof(GetWorkbookFromWebAppAsync)} [{url}] is not a web app's URL. Deploy the spreadsheet's script as a web app and use the URL it gives, which starts {WEB_APP_HOST}macros/");
            }

            byte[] response = await RequestAsync(url, nameof(GetWorkbookFromWebAppAsync), "Check the web app is deployed with access for \"Anyone\"");
            byte[] workbook = null;

            try
            {
                workbook = Convert.FromBase64String(Encoding.UTF8.GetString(response));
            }
            catch (FormatException)
            {
            }

            // A web app not deployed for anyone answers with a Google sign-in page, and a script that fails with an error page.
            if (!IsZip(workbook))
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{nameof(GetWorkbookFromWebAppAsync)} [{url}] did not return an .xlsx file. Check the web app is deployed from the spreadsheet's script, executing as you, with access for \"Anyone\", and that you authorised it");
            }

            return workbook;
        }

        private static async Task<byte[]> RequestAsync(string url, string caller, string advice)
        {
            using UnityWebRequest req = UnityWebRequest.Get(url);

            req.timeout = REQUEST_TIMEOUT_SECONDS;

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"{nameof(GoogleSheetDataProvider)}::{caller} Status [{req.result}] code [{req.responseCode}] error [{req.error}] requesting [{url}]. {advice}");
            }

            byte[] response = req.downloadHandler.data;

            return response;
        }

        private static bool IsZip(byte[] data)
        {
            bool isZip = data != null && data.Length >= 2 && data[0] == 'P' && data[1] == 'K';

            return isZip;
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
