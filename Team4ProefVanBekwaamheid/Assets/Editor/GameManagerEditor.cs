using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private GameState _selectedState;
    private bool _initialized = false; // Flag to ensure initialization happens once

    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector();

        GameManager gameManager = (GameManager)target;

        // Initialize _selectedState with the current game state the first time the inspector is drawn
        if (!_initialized && Application.isPlaying)
        {
            _selectedState = gameManager.State;
            _initialized = true;
        }
        // Reset initialization flag if not playing
        else if (!Application.isPlaying)
        {
             _initialized = false;
        }


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug State Changer", EditorStyles.boldLabel);

        // Dropdown to select the desired game state
        _selectedState = (GameState)EditorGUILayout.EnumPopup("Target State", _selectedState);

        // Button to update the game state
        if (GUILayout.Button("Update Game State"))
        {
            if (Application.isPlaying)
            {
                Undo.RecordObject(gameManager, "Change Game State"); // Record state for Undo
                gameManager.UpdateGameState(_selectedState);
                EditorUtility.SetDirty(gameManager); // Mark the object as dirty to ensure changes are registered
                Debug.Log($"Game state updated to: {_selectedState} via Inspector.");
            }
            else
            {
                Debug.LogWarning("Cannot update game state while not in Play Mode.");
            }
        }
    }
}