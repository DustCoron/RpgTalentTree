using UnityEngine;
using UnityEditor;
using RpgTalentTree.Core.Dungeon;

namespace RpgTalentTree.Editor
{
    /// <summary>
    /// Custom editor for ProBuilderDungeonGenerator with handy buttons
    /// </summary>
    [CustomEditor(typeof(ProBuilderDungeonGenerator))]
    public class ProBuilderDungeonGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProBuilderDungeonGenerator generator = (ProBuilderDungeonGenerator)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            // Generate button
            if (GUILayout.Button("Generate Dungeon", GUILayout.Height(30)))
            {
                generator.GenerateDungeon();
            }

            // Clear button
            if (GUILayout.Button("Clear Dungeon", GUILayout.Height(30)))
            {
                generator.ClearDungeon();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Click 'Generate Dungeon' to create a new procedural dungeon.\n" +
                "Adjust the settings above to customize the generation.",
                MessageType.Info);
        }
    }
}
