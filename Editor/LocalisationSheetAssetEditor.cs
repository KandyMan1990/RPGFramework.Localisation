using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Localisation.Editor
{
    [CustomEditor(typeof(LocalisationSheetAsset))]
    public class LocalisationSheetAssetEditor : UnityEditor.Editor
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
                                                   LocalisationSheetAsset asset = (LocalisationSheetAsset)target;
                                                   asset.Generate();
                                               })
                                    {
                                            text = "Generate .locbin"
                                    };

            root.Add(generateButton);

            return root;
        }
    }
}