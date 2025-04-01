using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSettings : MonoBehaviour
{
    // Settings
    public bool occupied;

    public enum OccupantType
    {
        None,
        Player,
        Enemy,
        Obstacle
    }

    public OccupantType occupantType;

    public int column;
    public int row;
    
    public void Initzialize(OccupantType occupantType, int column, int row)
    {
        this.column = column;
        this.row = row;
        this.occupantType = occupantType;
    }

    void Update()
    {
        switch (occupantType)
        {
            case OccupantType.None:
                occupied = false;
                break;
            case OccupantType.Player:
                occupied = true;
                break;
            case OccupantType.Enemy:
                occupied = true;
                break;
            case OccupantType.Obstacle:
                occupied = true;
                break;
        }
    }
}