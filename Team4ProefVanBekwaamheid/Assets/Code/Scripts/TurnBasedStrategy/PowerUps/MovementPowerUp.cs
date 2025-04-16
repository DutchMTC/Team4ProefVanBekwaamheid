using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementPowerUp : MonoBehaviour
{
    [SerializeField] private int _range; // The range of the power-up
    private TileSelection _tileSelection; // Reference to the TileSelection script

    void Start()
    {
        _tileSelection = FindObjectOfType<TileSelection>(); // Find the TileSelection script in the scene
    }

    void Update()
    {
        // Check if the player has selected this power-up
        if (Input.GetKeyDown(KeyCode.M)) // Replace with your input method
        {
            MovementPowerUpSelected();
        }
    }

    // When the player selects this power-up, it will check tiles within the range
    private void MovementPowerUpSelected()
    {
        // Choose a tile within the range of the power-up
        _tileSelection.FindTilesInRange(_range);

    }
}
