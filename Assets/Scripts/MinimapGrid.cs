using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class MinimapGrid : MonoBehaviour
{

    //grid specifics
    [SerializeField]
    private int rows;
    [SerializeField]
    private int cols;
    [SerializeField]
    private Vector2 gridSize;
    [SerializeField]
    private Vector2 gridOffset;

    //about cells
    [SerializeField]
    private Sprite cellSprite;
    private Vector2 cellSize;
    private Vector2 cellScale;
    
    void Start()
    {
        rows = MapManager.MAP_HEIGHT;
        cols = MapManager.MAP_WIDTH;
        gridSize = new Vector2(MapManager.MAP_WIDTH, MapManager.MAP_HEIGHT);
        cellSprite = (Sprite)Resources.Load("BlueSp", typeof(Sprite));


        

        InitCells(); //Initialize all cells
    }

    void InitCells()
    {
        GameObject cellObject = new GameObject();

        //creates an empty object and adds a sprite renderer component -> set the sprite to cellSprite
        cellObject.AddComponent<SpriteRenderer>().sprite = cellSprite;

        //catch the size of the sprite
        cellSize = cellSprite.bounds.size;

        //get the new cell size -> adjust the size of the cells to fit the size of the grid
        Vector2 newCellSize = new Vector2(gridSize.x / (float)cols, gridSize.y / (float)rows);

        //Get the scales so you can scale the cells and change their size to fit the grid
        cellScale.x = newCellSize.x / cellSize.x;
        cellScale.y = newCellSize.y / cellSize.y;

        cellSize = newCellSize; //the size will be replaced by the new computed size, we just used cellSize for computing the scale

        cellObject.transform.localScale = new Vector2(cellScale.x, cellScale.y);

        //fix the cells to the grid by getting the half of the grid and cells add and minus experiment
        gridOffset.x = -(gridSize.x / 2) + cellSize.x / 2;
        gridOffset.y = -(gridSize.y / 2) + cellSize.y / 2;

        //fill the grid with cells by using Instantiate
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                //add the cell size so that no two cells will have the same x and y position
                Vector2 pos = new Vector2(col * cellSize.x + gridOffset.x + transform.position.x, row * cellSize.y + gridOffset.y + transform.position.y);

                //instantiate the game object, at position pos, with rotation set to identity
                GameObject cO = Instantiate(cellObject, pos, Quaternion.identity) as GameObject;

                //set the parent of the cell to GRID so you can move the cells together with the grid;
                cO.transform.parent = transform;
            }
        }

        //destroy the object used to instantiate the cells
        Destroy(cellObject);
    }

    //so you can see the width and height of the grid on editor
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, gridSize);
    }


    /*
    public Tile highlightTile;
    public Tilemap highlightMap;

    private Vector3Int previous;

    // do late so that the player has a chance to move in update if necessary
    private void LateUpdate()
    {
        // get current grid location
        Vector3Int currentCell = highlightMap.WorldToCell(transform.position);
        // add one in a direction (you'll have to change this to match your directional control)
        currentCell.x += 1;

        // if the position has changed
        if (currentCell != previous)
        {
            // set the new tile
            highlightMap.SetTile(currentCell, highlightTile);

            // erase previous
            highlightMap.SetTile(previous, null);

            // save the new position for next frame
            previous = currentCell;
        }
    }
    */
}