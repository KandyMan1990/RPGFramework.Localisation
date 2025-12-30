using System;
using RPGFramework.Localisation.Editor.LocalisationBinWriter;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    [CustomEditor(typeof(LocalisationMaster))]
    public class LocalisationMasterEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            root.Add(new VisualElement
                     {
                             style =
                             {
                                     height = 8
                             }
                     });

            Button generateButton = new Button(async () =>
                                               {
                                                   try
                                                   {
                                                       LocalisationMaster asset = (LocalisationMaster)target;

                                                       foreach (LocalisationSheetAsset sheetAsset in asset.SheetAssets)
                                                       {
                                                           if (string.IsNullOrEmpty(sheetAsset.SheetName))
                                                           {
                                                               EditorUtility.DisplayDialog("Missing Sheet Name", $"Set SheetName on {sheetAsset.name} (for folder naming)", "OK");
                                                               return;
                                                           }

                                                           if (string.IsNullOrEmpty(sheetAsset.Gid))
                                                           {
                                                               EditorUtility.DisplayDialog("Missing Gid", $"Set Gid on {sheetAsset.name}", "OK");
                                                               return;
                                                           }
                                                       }

                                                       await LocalisationWriter.WriteAsync(asset);
                                                   }
                                                   catch (Exception e)
                                                   {
                                                       Debug.LogException(e);
                                                   }
                                                   finally
                                                   {
                                                       EditorUtility.ClearProgressBar();
                                                   }
                                               })
                                    {
                                            text = "Generate .locbin and manifest files"
                                    };

            root.Add(generateButton);

            return root;
        }
    }
}