using UnityEngine;
using UnityEngine.UI;

public class Node : MonoBehaviour
{
    // UI Image
    public Image image;
    
    // Image Rect
    public RectTransform rectTransform;
    
    // Grid Position
    public Vector2Int gridPos;
    
    // Is the node traversable
    public bool isBlocked;
    
    // Distance to get from start node to this node
    public int gCost;
    
    // Distance from node to end node
    public int hCost;
    
    // Combined cost of gCost and hCost
    public int FCost => gCost + hCost;
    
    public Node parentNode;

    /// <summary>
    /// Triggered when node is clicked on board
    /// </summary>
    public void OnNodeClicked()
    {
        Grid.Instance.OnNodeClicked(this);
    }
}