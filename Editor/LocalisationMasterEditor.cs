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

                if (string.IsNullOrEmpty(asset.SheetId))
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