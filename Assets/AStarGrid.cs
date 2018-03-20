using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarGrid : MonoBehaviour {

    public TerrainType[] walkableRegions;
    public bool displayGridGizmos;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public int obstacleProximityPenalty = 10;

    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    LayerMask walkableMask;
    AStarNode[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMax = int.MinValue;
    int penaltyMin = int.MaxValue;

    private void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt( gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add( (int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }

        CreateGrid();

    }

    void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize*2+1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltysHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltysVerticalPass = new int[gridSizeX, gridSizeY];

        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = -kernelExtents; x < kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltysHorizontalPass[0, y] += grid[sampleX, y].PenaltyValue;
            }

            for (int x = 1; x < gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);
                penaltysHorizontalPass[x, y] = penaltysHorizontalPass[x - 1, y] - grid[removeIndex, y].PenaltyValue + grid[addIndex, y].PenaltyValue;
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = -kernelExtents; y < kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltysVerticalPass[x, 0] += penaltysHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltysVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].PenaltyValue = blurredPenalty;


            for (int y = 1; y < gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);
                penaltysVerticalPass[x, y] = penaltysVerticalPass[x, y-1] - penaltysHorizontalPass[x, removeIndex] + penaltysHorizontalPass[x, addIndex];

                blurredPenalty = Mathf.RoundToInt((float)penaltysVerticalPass[x, y] / (kernelSize * kernelSize));
                grid[x, y].PenaltyValue = blurredPenalty;

                if(blurredPenalty > penaltyMax)
                {
                    penaltyMax = blurredPenalty;
                }
                if(blurredPenalty < penaltyMin)
                {
                    penaltyMin = blurredPenalty;
                }

            }
        }

    }

    public int MaxSize
    {
        get { return gridSizeX * gridSizeY; }        
     }

    public List<AStarNode> GetNeighbours( AStarNode node)
    {
        List<AStarNode> neighbours = new List<AStarNode>();

        for (int x = -1; x <= 1 ; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.GridX + x;
                int checkY = node.GridY + y;

                if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    void CreateGrid()
    {
        grid = new AStarNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.x/2;

        for (int x = 0; x < gridSizeX ; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));

                int movementPenalty = 0;
                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100, walkableMask))
                {
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }

                if(!walkable)
                {
                    movementPenalty += obstacleProximityPenalty;
                }

                grid[x, y] = new AStarNode(walkable, worldPoint, x, y, movementPenalty);
            }
        }

        BlurPenaltyMap(3);

    }

    public AStarNode NodeFromWorldPoint(Vector3 position)
    {
       float percentX = (position.x + gridWorldSize.x / 2) / gridWorldSize.x;
       float percentY = (position.z + gridWorldSize.y / 2) / gridWorldSize.y;

       // float percentX = position.x / gridWorldSize.x + 0.5f;
        //float percentY = position.z / gridWorldSize.y + 0.5f;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt ((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null && displayGridGizmos)
        {
            foreach (var node in grid)
            {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, node.PenaltyValue));

                Gizmos.color = (node.Walkable) ? Gizmos.color : Color.red;
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * (nodeDiameter));
            }
        }

    }
};


[System.Serializable]
public class TerrainType
{
    public LayerMask terrainMask;
    public int terrainPenalty;

};

