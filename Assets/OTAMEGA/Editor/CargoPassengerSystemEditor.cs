using UnityEngine;
using UnityEditor; // エディタ拡張用

// CargoPassengerSystem コンポーネントのインスペクターをカスタムする
[CustomEditor(typeof(CargoPassengerSystem))]
public class CargoPassengerSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // もともとのインスペクター表示（変数の設定など）を描画
        DrawDefaultInspector();

        // 対象のスクリプトを取得
        CargoPassengerSystem script = (CargoPassengerSystem)target;

        GUILayout.Space(10); // 余白
        GUILayout.Label("テスト用操作", EditorStyles.boldLabel);

        // 横並びレイアウト開始
        GUILayout.BeginHorizontal();

        // ＋ボタン
        if (GUILayout.Button("＋ 乗客を追加 (Add)", GUILayout.Height(30)))
        {
            script.TryAddPassenger();
        }

        // －ボタン
        if (GUILayout.Button("－ 乗客を削除 (Remove)", GUILayout.Height(30)))
        {
            script.RemovePassenger();
        }

        // 横並びレイアウト終了
        GUILayout.EndHorizontal();

        // 現在人数の表示
        GUILayout.Space(5);
        EditorGUILayout.HelpBox($"現在の乗客数: {script.CurrentCount} / {script.maxPassengers}", MessageType.Info);
    }
}