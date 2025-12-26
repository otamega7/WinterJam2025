using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NPCGenerator))]
public class NPCGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 元のInspector表示（変数設定など）を描画
        DrawDefaultInspector();

        // スペースを空ける
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // 生成ボタンを作成
        NPCGenerator generator = (NPCGenerator)target;
        if (GUILayout.Button("Generate NPC", GUILayout.Height(40)))
        {
            generator.GenerateNPC();
        }
    }
}