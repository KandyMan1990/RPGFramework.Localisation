using System;
using RPGFramework.Localisation.Editor.LocalisationBinGenerator;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    [CustomEditor(typeof(LocalisationMaster))]
    internal class LocalisationMasterEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            root.Q<PropertyField>($"PropertyField:{nameof(LocalisationMaster.SheetAssets)}")?.SetEnabled(false);

            PropertyField      sheetId   = root.Q<PropertyField>($"PropertyField:{nameof(LocalisationMaster.SheetId)}");
            PropertyField      webAppUrl = root.Q<PropertyField>($"PropertyField:{nameof(LocalisationMaster.WebAppUrl)}");
            SerializedProperty access    = serializedObject.FindProperty(nameof(LocalisationMaster.Access));
            Button             copy      = new Button(OnCopyScriptButtonCB)
                                           {
                                               text    = "Copy web app script",
                                               tooltip = "Copies the script to deploy as a web app from the spreadsheet's Extensions > Apps Script. The package's README has the steps"
                                           };

            webAppUrl?.parent.Insert(webAppUrl.parent.IndexOf(webAppUrl) + 1, copy);

            ShowAccessFields(access, sheetId, webAppUrl, copy);
            root.TrackPropertyValue(access, changed => ShowAccessFields(changed, sheetId, webAppUrl, copy));

            root.Add(new VisualElement
                     {
                         style =
                         {
                             height = 8
                         }
                     });

            VisualElement buttons = new VisualElement
                                    {
                                        style =
                                        {
                                            flexDirection = FlexDirection.Row
                                        }
                                    };

            buttons.Add(new Button(OnPullButtonCB)
                        {
                            text = "Pull spreadsheet",
                            style =
                            {
                                flexGrow = 1
                            }
                        });
            buttons.Add(new Button(OnGenerateButtonCB)
                        {
                            text = "Generate files",
                            style =
                            {
                                flexGrow = 1
                            }
                        });

            root.Add(buttons);

            return root;
        }

        private async void OnPullButtonCB()
        {
            try
            {
                LocalisationMaster asset = (LocalisationMaster)target;

                if (asset.Access == SpreadsheetAccess.WebApp && string.IsNullOrWhiteSpace(asset.WebAppUrl))
                {
                    EditorUtility.DisplayDialog("Missing Web App URL", $"Set {nameof(LocalisationMaster.WebAppUrl)} on {asset.name} to the URL of the web app deployed from the spreadsheet's script", "OK");
                    return;
                }

                if (asset.Access == SpreadsheetAccess.SharedLink && string.IsNullOrEmpty(asset.SheetId))
                {
                    EditorUtility.DisplayDialog("Missing Sheet Id", $"Set {nameof(LocalisationMaster.SheetId)} on {asset.name} to the spreadsheet's id, or a link to it", "OK");
                    return;
                }

                await LocalisationWriter.PullAsync(asset);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ShowAccessFields(SerializedProperty access, VisualElement sheetId, VisualElement webAppUrl, VisualElement copy)
        {
            bool         isWebApp  = access.intValue == (int)SpreadsheetAccess.WebApp;
            DisplayStyle linkShown = isWebApp ? DisplayStyle.None : DisplayStyle.Flex;
            DisplayStyle appShown  = isWebApp ? DisplayStyle.Flex : DisplayStyle.None;

            if (sheetId != null)
            {
                sheetId.style.display = linkShown;
            }

            if (webAppUrl != null)
            {
                webAppUrl.style.display = appShown;
            }

            copy.style.display = appShown;
        }

        private static void OnCopyScriptButtonCB()
        {
            EditorGUIUtility.systemCopyBuffer = GoogleSheetDataProvider.WEB_APP_SCRIPT;

            Debug.Log("Copied the web app script. Paste it into the spreadsheet's Extensions > Apps Script, then deploy it as a web app");
        }

        private void OnGenerateButtonCB()
        {
            try
            {
                LocalisationWriter.Generate((LocalisationMaster)target);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}