using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Grid : MonoBehaviour
{
    // Singleton instance
    public static Grid Instance;

    // Node size
    public Vector2Int size;
    
    // Spacing between nodes
    public int spacing;

    public RectTransform gridDisplayPanel;
    public GameObject nodePrefab;

    // Color of wall
    public Color wallColor;
    
    // Text that shows current action
    public TextMeshProUGUI actionText;
    
    // Current state
    public State state;
    
    public PathFinder pathFinder;

    // Current pathing speed
    public float pathSpeed = 0.1f;

    // Set of walls on board
    private HashSet<Node> _walls = new HashSet<Node>();

    // currently selected algorithm
    private int _algorithmId = 0;
    
    // Node Grid
    private Node[,] _nodes;
    
    private Node _startNode;
    private Node _endNode;
    
    private void Awake()
    {
        // Set Singleton
        Instance = this;
    }

    private void Start()
    {
        // Generate Grid   
        StartCoroutine(GenerateGridNodes());
    }

    
    /// <summary>
    /// Generate grid and initialize nodes
    /// </summary>
    IEnumerator GenerateGridNodes()
    {
        var rect = gridDisplayPanel.rect;
        
        // Get amount of nodes that can fit in panel
        var sizeX = (int) rect.width / (size.x + spacing) - 1;
        var sizeY = (int) rect.height / (size.y + spacing);

        // Get total size of grid
        var gridWidth = sizeX * (size.x + spacing);
        var gridHeight = sizeY * (size.y + spacing);

        // Get Amount to move in order to center grid in panel
        var centerWidth = (rect.width - gridWidth) / 2; 
        var centerHeight = (rect.height - gridHeight) / 2; 
        
        // Init 2d node array
        _nodes = new Node[sizeX, sizeY];

        // Create Nodes
        for (int x = 0; x < sizeX; x++)
        for (int y = 0; y < sizeY; y++)
        {
            // Create Node from prefab
            var node = Instantiate(nodePrefab, gridDisplayPanel).GetComponent<Node>();

            // Set Node position
            node.rectTransform.anchoredPosition = new Vector3(
                x * (size.x + spacing), 
                -y * (size.y + spacing), 
                -10) + new Vector3(centerWidth, -centerHeight);

            node.gridPos = new Vector2Int(x, y);
            
            _nodes[x, y] = node;
        }
        
        yield return null;
    }

    /// <summary>
    /// Node clicked, setting node to a color depending on the current state
    /// </summary>
    public void OnNodeClicked(Node node)
    {
        switch (state)
        {
            case State.PlacingStart:
                if (_startNode != null) _startNode.image.color = Color.white;
                _startNode = node;
                node.image.color = Color.green;
                break;
            case State.PlacingEnd:
                if (_endNode != null) _endNode.image.color = Color.white;
                _endNode = node;
                node.image.color = Color.red;
                break;
            case State.PlacingWalls:
                if (node.isBlocked)
                {
                    node.image.color = Color.white;
                    node.isBlocked = false;
                    _walls.Remove(node);
                    break;
                }

                _walls.Add(node);
                node.image.color = wallColor;
                node.isBlocked = true;
                break;
        }
    }
    
    
    /// <summary>
    /// Place start button clicked
    /// </summary>
    public void OnPlacingStartClicked()
    {
        if (state == State.GeneratingPath) return;
        
        state = State.PlacingStart;
        actionText.text = "Placing Start";
        pathFinder.ClearColoredNodes();
    }
    
    /// <summary>
    /// Place end button clicked
    /// </summary>
    public void OnPlacingEndClicked()
    {
        if (state == State.GeneratingPath) return;

        state = State.PlacingEnd;
        actionText.text = "Placing End";
        pathFinder.ClearColoredNodes();
    }
    
    
    // Place wall button clicked
    public void OnPlacingWallClicked()
    {
        if (state == State.GeneratingPath) return;

        state = State.PlacingWalls;
        actionText.text = "Placing Walls";
        pathFinder.ClearColoredNodes();
    }

    
    /// <summary>
    /// Clears all created walls
    /// </summary>
    public void ClearWalls()
    {
        if (state == State.GeneratingPath) return;

        foreach (var wall in new HashSet<Node>(_walls))
        {
            wall.image.color = Color.white;
            _walls.Remove(wall);
        }
    }
    
    
    /// <summary>
    /// Generate Path button clicked
    /// </summary>
    public void OnGeneratePathClicked()
    {
        Debug.Log("Generate Clicked");
        // Check if there is already a path being generated
        if (state == State.GeneratingPath) return;

        // Check if a start and end node is set
        if (_startNode == null && _endNode == null)
        {
            Debug.LogError($"Start Node or End Node is not set!");
            return;
        }
        
        actionText.text = "Generating Path";
        state = State.GeneratingPath;

        Debug.Log($"algorithm id:{_algorithmId}");
        pathFinder.StopAllCoroutines();
        ResetNodeCosts();
        // Start pathfinding algorithm depending on currently selected algorithm
        switch (_algorithmId)
        {
            case 0:
                StartCoroutine(pathFinder.FindPathWithAStar(_nodes, _startNode, _endNode));
                break;
            case 1:
                StartCoroutine(pathFinder.FindPathWithGreedyBestFirst(_nodes, _startNode, _endNode));
                break;
        }
    }

    
    /// <summary>
    /// Set speed when speed dropdown value is changed
    /// </summary>
    /// <param name="speed"></param>
    public void OnSpeedChanged(int speed)
    {
        switch (speed)
        {
            case 0: // Fast
                pathSpeed = 0.1f;
                break;
            case 1: // Average
                pathSpeed = 0.25f;
                break;
            case 2: // Fast
                pathSpeed = 0.55f;
                break;
        }
    }

    /// <summary>
    /// Reset Costs of nodes
    /// </summary>
    public void ResetNodeCosts()
    {
        foreach (var node in _nodes)
        {
            node.gCost = 0;
            node.hCost = 0;
        }
    }

    
    /// <summary>
    /// Set algorithm when algorithm dropdown value is changed
    /// </summary>
    /// <param name="id"></param>
    public void OnAlgorithmChanged(int id)
    {
        _algorithmId = id;
    }
}