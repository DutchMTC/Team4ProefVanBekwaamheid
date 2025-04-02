using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TileSettings : MonoBehaviour
{
    public enum OccupantType
    {
        None,
        Player,
        Enemy,
        Obstacle
    }
    
    // Settings
    public OccupantType occupantType;
    public int column;
    public int row;
    internal UnityEvent OccupationChangedEvent;
    
    public void Initzialize(OccupantType occupantType, int column, int row)
    {
        this.column = column;
        this.row = row;
        this.occupantType = occupantType;
    }

    void Start()
    {
        OccupationChangedEvent = new UnityEvent();
        OccupationChangedEvent.AddListener(OnOccupationChange);
    }

    // Will eventually be used
    public void OnOccupationChange()
    {
        switch (occupantType)
        {
            case OccupantType.None:
                break;
            case OccupantType.Player:
                break;
            case OccupantType.Enemy:
                break;
            case OccupantType.Obstacle:
                break;
        }
    }
}