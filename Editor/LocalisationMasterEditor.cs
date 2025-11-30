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
                                                   LocalisationMaster asset = (LocalisationMaster)target;

                                                   foreach (LocalisationSheetAsset sheetAsset in asset.SheetAssets)
                                                   {
                                                       sheetAsset.Generate();
                                                   }
                                               })
                                    {
                                            text = "Generate .locbin files"
                                    };

            root.Add(generateButton);

            return root;
        }
    }
}