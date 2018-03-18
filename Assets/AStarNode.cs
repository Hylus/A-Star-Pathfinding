using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarNode : IHeapItem<AStarNode> {


    public bool Walkable;
    public Vector3 WorldPosition;
    public int GCost;
    public int HCost;
    public int GridX;
    public int GridY;
    public AStarNode Parent;
    int heapIndex;

    public AStarNode ( bool walkable, Vector3 worldPos, int gridX, int gridY)
    {
        GridX = gridX;
        GridY = gridY;
        Walkable = walkable;
        WorldPosition = worldPos;
    }

    public int FCost {  get { return GCost + HCost; } }

    public int HeapIndex
    {
        get { return heapIndex; }
        set { heapIndex = value; }
    }

    public int CompareTo(AStarNode nodeToCompare)
    {
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if(compare==0)
        {
            compare = HCost.CompareTo(nodeToCompare.HCost);
        }
        return -compare;
    }
}
