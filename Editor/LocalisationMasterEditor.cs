using RPGFramework.Localisation.Editor.LocalisationBinWriter;
using UnityEditor;
using UnityEditor.UIElements;
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

            Button generateButton = new Button(() =>
                                               {
                                                   LocalisationMaster     asset                 = (LocalisationMaster)target;
                                                   ILocalisationBinWriter localisationBinWriter = LocalisationBinWriterProvider.GetLocalisationBinWriter((byte)asset.Version);

                                                   foreach (LocalisationSheetAsset sheetAsset in asset.SheetAssets)
                                                   {
                                                       if (string.IsNullOrEmpty(sheetAsset.SheetName))
                                                       {
                                                           EditorUtility.DisplayDialog("Missing Sheet Name", $"Set SheetName on {sheetAsset.name} (for folder naming)", "OK");
                                                       }
                                                       else if (string.IsNullOrEmpty(sheetAsset.Gid))
                                                       {
                                                           EditorUtility.DisplayDialog("Missing Gid", $"Set Gid on {sheetAsset.name}", "OK");
                                                       }
                                                       else
                                                       {
                                                           localisationBinWriter.GenerateLocalisationBin(asset, sheetAsset);
                                                       }
                                                   }

                                                   localisationBinWriter.GenerateLocalisationManifest(asset);
                                               })
                                    {
                                            text = "Generate .locbin and manifest files"
                                    };

            root.Add(generateButton);

            return root;
        }
    }
}