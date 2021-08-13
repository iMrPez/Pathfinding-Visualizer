using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    // Path Colors
    public Color openNodeColor;
    public Color closedNodeColor;
    public Color pathColor;
    
    // Sets
    public List<Node> openSet = new List<Node>();
    private HashSet<Node> _closedSet = new HashSet<Node>();
    
    // Final path
    public List<Node> path = new List<Node>();

    // List of nodes 
    private HashSet<Node> _coloredNodes = new HashSet<Node>();

    public TextMeshProUGUI pauseText;

    private bool _stopPathfinding;
    private bool _pausePathfinding;

    
    /// <summary>
    /// Find path to end node using the A* algorithm
    /// </summary>
    public IEnumerator FindPathWithAStar(Node[,] nodes, Node startNode, Node endNode)
    {
        // Clear board of previously colored nodes
        ClearColoredNodes();
        
        // Clear sets
        openSet.Clear();
        _closedSet.Clear();
        path.Clear();
        
        // Add start node to open set
        openSet.Add(startNode);
        
        // Loop through open set until no nodes remain
        while (openSet.Count > 0)
        {
            // Set current node to first node in open set
            Node currentNode = openSet[0];
            
            // Find lowest FCost node and set it to current node
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || 
                    openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            
            // Remove current node from openSet to closedSet of nodes
            openSet.Remove(currentNode);
            _closedSet.Add(currentNode);

            // Check if the end node has been found then retrace the path
            if (currentNode == endNode)
            {
                FinishPath(startNode, endNode);
                yield break;
            }
            
            // Find and add neighbors to openSet
            foreach (var neighbor in GetNeighbors(currentNode, nodes))
            {
                // Check if the neighbor is walkable and has not already been visited
                if (neighbor.isBlocked || _closedSet.Contains(neighbor)) continue;
                
                
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, endNode);
                    neighbor.parentNode = currentNode;
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            DrawNodes(startNode, endNode);
            
            // Wait while paused
            while (_pausePathfinding) yield return null;
            
            
            if (_stopPathfinding)
            {
                _stopPathfinding = false;
                yield break;
            }
            
            yield return new WaitForSeconds(Grid.Instance.pathSpeed);
        }

        Grid.Instance.actionText.text = "No path found";
        yield return null;
    }

    /// <summary>
    /// Find path to end node using the Greedy Best-First algorithm
    /// </summary>
    public IEnumerator FindPathWithGreedyBestFirst(Node[,] nodes, Node startNode, Node endNode)
    {
        // Clear board of previously colored nodes
        ClearColoredNodes();
        
        // Clear sets
        openSet.Clear();
        _closedSet.Clear();
        path.Clear();

        // Add start node to open set
        openSet.Add(startNode);

        // Loop through open set until no nodes remain
        while (openSet.Count > 0)
        {
            // Set current node to first node in open set
            Node currentNode = openSet[0];
            
            // Find lowest FCost node and set it to current node
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost)
                {
                    currentNode = openSet[i];
                }
            }

            // Remove current node from openSet to closedSet of nodes
            openSet.Remove(currentNode);
            _closedSet.Add(currentNode);

            // Check if the end node has been found then retrace the path
            if (currentNode == endNode)
            {
                FinishPath(startNode, endNode);
                yield break;
            }

            // Find and add neighbors to openSet
            foreach (var neighbor in GetNeighbors(currentNode, nodes))
            {
                // Check if the neighbor is walkable and has not already been visited
                if (neighbor.isBlocked || _closedSet.Contains(neighbor)) continue;
                
                
                if (!openSet.Contains(neighbor))
                {
                    neighbor.hCost = GetDistance(neighbor, endNode);
                    neighbor.parentNode = currentNode;
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            DrawNodes(startNode, endNode);
            // Wait while paused

            while (_pausePathfinding) yield return null;
            
            if (_stopPathfinding)
            {
                _stopPathfinding = false;
                yield break;
            }
            
            yield return new WaitForSeconds(Grid.Instance.pathSpeed); 
        }
        Grid.Instance.actionText.text = "No Path found";
    }

    /// <summary>
    /// Finish Path
    /// </summary>
    private void FinishPath(Node startNode, Node endNode)
    {
        RetracePath(startNode, endNode);
        DrawNodes(startNode, endNode);
        Grid.Instance.state = State.FinishedPath;
        Grid.Instance.actionText.text = "Finished Path";
        StopAllCoroutines();
    }


    /// <summary>
    /// Retrace the path from end node to start node
    /// </summary>
    public void RetracePath(Node startNode, Node endNode)
    {
        List<Node> newPath = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            newPath.Add(currentNode);
            currentNode = currentNode.parentNode;
        }

        newPath.Reverse();
        path = newPath;
    }
    

    /// <summary>
    /// Get the distance between two nodes
    /// </summary>
    public int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridPos.x - nodeB.gridPos.x);
        int dstY = Mathf.Abs(nodeA.gridPos.y - nodeB.gridPos.y);

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY);
        }

        return 14 * dstX + 10 * (dstY - dstX);
    }
    
    
    /// <summary>
    /// Gets the neighbors of a node
    /// </summary>
    public IEnumerable<Node> GetNeighbors(Node node, Node[,] nodes)
    {
        for (int x = node.gridPos.x - 1; x <= node.gridPos.x + 1; x++)
        for (int y = node.gridPos.y - 1; y <= node.gridPos.y + 1; y++)
        {
            if (x == node.gridPos.x && y == node.gridPos.y) continue;
            
            if (IsInRange(x, y, nodes)) yield return nodes[x, y];
        }
    }

    
    /// <summary>
    /// Check if position is within node grid
    /// </summary>
    public bool IsInRange(int x, int y, Node[,] nodes)
    {
        var size = new Vector2Int(nodes.GetUpperBound(0), nodes.GetUpperBound(1));
        return ((x <= size.x && x >= 0) && (y <= size.y && y >= 0));
    }

    
    /// <summary>
    /// Clear all colored nodes
    /// </summary>
    public void ClearColoredNodes()
    {
        if (_coloredNodes is null || _coloredNodes.Count == 0) return;
        
        foreach (var coloredNode in new HashSet<Node>(_coloredNodes))
        {
            coloredNode.image.color = Color.white;
            _coloredNodes.Remove(coloredNode);
        }
    }

    
    /// <summary>
    /// Draw set nodes
    /// </summary>
    public void DrawNodes(Node startNode, Node endNode)
    {
        foreach (var openNode in openSet)
        {
            if (startNode != openNode && endNode != openNode)
            {
                openNode.image.color = openNodeColor;
                _coloredNodes.Add(openNode);
            }
        }
        
        foreach (var closedNode in _closedSet)
        {
            if (startNode != closedNode && endNode != closedNode)
            {
                closedNode.image.color = closedNodeColor;
                _coloredNodes.Add(closedNode);
            }
        }
        
        foreach (var pathNode in path)
        {
            if (startNode != pathNode && endNode != pathNode)
            {
                pathNode.image.color = pathColor;
                _coloredNodes.Add(pathNode);
            }
        }
    }

    
    /// <summary>
    /// Pause and resume pathfinder
    /// </summary>
    public void TogglePathfinder()
    {
        _pausePathfinding = !_pausePathfinding;
        pauseText.text = _pausePathfinding ? "Resume" : "Stop";

    }
    
    /// <summary>
    /// Stop pathfinder
    /// </summary>
    public void StopPathfinder()
    {
        _stopPathfinding = true;
        _pausePathfinding = false;
    }
}
