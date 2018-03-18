using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarNode {


    public bool Walkable;
    public Vector3 WorldPosition;
    public int GCost;
    public int HCost;
    public int GridX;
    public int GridY;
    public AStarNode Parent;

    public AStarNode ( bool walkable, Vector3 worldPos, int gridX, int gridY)
    {
        GridX = gridX;
        GridY = gridY;
        Walkable = walkable;
        WorldPosition = worldPos;
    }

    public int FCost {  get { return GCost + HCost; } }

}
