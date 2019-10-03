﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public int borderSize;

    public GameObject tilePrefab;
    public GameObject[] gamePiecePrefabs;

    public float swapTime = 0.5f;

    private Tile[,] m_allTiles;
    private GamePiece[,] m_allGamePieces;

    private Tile m_clickedTile;
    private Tile m_targetTile;

    private bool m_playerInputEnabled = true;

    private void Start()
    {
        m_allTiles = new Tile[width, height];
        m_allGamePieces = new GamePiece[width, height];

        SetupTiles();
        SetupCamera();
        FillBoard();        
    }

    private void SetupTiles()
    {
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0), Quaternion.identity) as GameObject;
                tile.name = "Tile (" + i + "," + j + ")";
                tile.transform.SetParent(transform);

                m_allTiles[i, j] = tile.GetComponent<Tile>();                
                m_allTiles[i, j].Init(i, j, this);
            }
        }
    }

    private void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((float)(width-1) / 2f, (float)(height-1) / 2f, -10f);

        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float verticalSize = (float)height / 2f + (float)borderSize;
        float horizontalSize = ((float)width / 2f + (float)borderSize) / aspectRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    private GameObject GetRandomGamePiece()
    {
        int randomIndex = Random.Range(0, gamePiecePrefabs.Length);

        if(gamePiecePrefabs[randomIndex] == null)
        {
            Debug.LogWarning("BOARD: " + randomIndex + " does not contain a valid GamePiece prefab!");
        }

        return gamePiecePrefabs[randomIndex];
    }

    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        if(gamePiece == null)
        {
            Debug.LogWarning("BOARD: " + "Invalid GamePiece !");
            return;
        }

        gamePiece.transform.position = new Vector3(x, y, 0);
        gamePiece.transform.rotation = Quaternion.identity;

        if(IsWithinBounds(x, y))
        {
            m_allGamePieces[x, y] = gamePiece;
        }
        
        gamePiece.SetCoord(x, y);
    }

    private bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    private GamePiece FillRandomAt(int x, int y)
    {
        GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity) as GameObject;

        if (randomPiece != null)
        {
            randomPiece.GetComponent<GamePiece>().Init(this);
            PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), x, y);
            randomPiece.transform.SetParent(transform);

            return randomPiece.GetComponent<GamePiece>();
        }

        return null;
    }

    private void FillBoard()
    {
        int maxIterations = 100;
        int iterations = 0;

        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                GamePiece piece = FillRandomAt(i, j);
                iterations = 0;

                while(HasMatchOnFill(i, j))
                {
                    ClearPieceAt(i, j);
                    piece = FillRandomAt(i, j);
                    iterations += 1;

                    if (iterations >= maxIterations)
                    {                        
                        Debug.Log("break ================");
                        break;
                    }                        
                }
            }
        }
    }

    private bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);
        List<GamePiece> downwardMatches = FindMatches(x, y, new Vector2(0, -1), minLength);

        if (leftMatches == null)
            leftMatches = new List<GamePiece>();

        if (downwardMatches == null)
            downwardMatches = new List<GamePiece>();

        return leftMatches.Count > 0 || downwardMatches.Count > 0;
    }

    public void ClickTile(Tile tile)
    {
        if(m_clickedTile == null)
        {
            m_clickedTile = tile;
        }
    }

    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(tile, m_clickedTile))
        {
            m_targetTile = tile;
        }
    }

    public void ReleaseTile()
    {
        if(m_clickedTile != null && m_targetTile != null)
        {
            SwitchTiles(m_clickedTile, m_targetTile);
        }

        m_clickedTile = null;
        m_targetTile = null;
    }

    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    private IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        if(m_playerInputEnabled)
        {
            GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
            GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

            if (targetPiece != null && clickedPiece != null)
            {
                clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);

                yield return new WaitForSeconds(swapTime);

                List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
                List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

                if (targetPieceMatches.Count == 0 && clickedPieceMatches.Count == 0)
                {
                    clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                    targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                }
                else
                {
                    yield return new WaitForSeconds(swapTime);

                    ClearAndRefillBoard(clickedPieceMatches.Union(targetPieceMatches).ToList());
                }
            }
        }           
    }

    private bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
            return true;

        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex)
            return true;

        return false;
    }

    private List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = null;

        if(IsWithinBounds(startX, startY))
            startPiece = m_allGamePieces[startX, startY];

        if (startPiece != null)
            matches.Add(startPiece);
        else
            return null;

        int nextX;
        int nextY;
        int maxValue = width > height ? width : height;

        for(int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if(!IsWithinBounds(nextX, nextY))
                break;

            GamePiece nextPiece = m_allGamePieces[nextX, nextY];

            if (nextPiece == null)
                break;
            else
            {
                if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece))
                    matches.Add(nextPiece);
                else
                    break;
            }            
        }

        if (matches.Count >= minLength)
            return matches;

        return null;
    }

    private List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        if (upwardMatches == null)
            upwardMatches = new List<GamePiece>();

        if (downwardMatches == null)
            downwardMatches = new List<GamePiece>();

        var combineMatches = upwardMatches.Union(downwardMatches).ToList();

        return combineMatches.Count >= minLength ? combineMatches : null;
    }

    private List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightMatches == null)
            rightMatches = new List<GamePiece>();

        if (leftMatches == null)
            leftMatches = new List<GamePiece>();

        var combineMatches = rightMatches.Union(leftMatches).ToList();

        return combineMatches.Count >= minLength ? combineMatches : null;
    }

    private List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GamePiece> horizMaches = FindHorizontalMatches(x, y, minLength);
        List<GamePiece> vertMaches = FindVerticalMatches(x, y, minLength);

        if (horizMaches == null)
            horizMaches = new List<GamePiece>();

        if (vertMaches == null)
            vertMaches = new List<GamePiece>();

        var combinedMatches = horizMaches.Union(vertMaches).ToList();
        return combinedMatches;
    }

    private List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();

        foreach (GamePiece piece in gamePieces)
            matches = matches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLength)).ToList();

        return matches;
    }

    private void HighlightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
    }

    private void HighlightTileOn(int x, int y, Color col)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = col;
    }

    private void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);

        var combinedMatches = FindMatchesAt(x, y);

        if (combinedMatches.Count > 0)
        {
            foreach (GamePiece piece in combinedMatches)
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
        }
    }

    private void HighlightMatches()
    {
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }

    private void HighlightPieces(List<GamePiece> gamePieces)
    {
        foreach(GamePiece piece in gamePieces)
        {
            if (piece != null)
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
        }
    }

    private void ClearPieceAt(int x, int y)
    {
        GamePiece pieceToClear = m_allGamePieces[x, y];

        if(pieceToClear != null)
        {
            m_allGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);
        }

        HighlightTileOff(x, y);
    }

    private void ClearBoard()
    {
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                ClearPieceAt(i, j);
            }
        }
    }

    private void ClearPieceAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if(piece != null)
                ClearPieceAt(piece.xIndex, piece.yIndex);
        }
            
    }

    private List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();

        for(int i = 0; i < height - 1; i++)
        {
            if(m_allGamePieces[column, i] == null)
            {
                for(int j = i + 1; j < height; j++)
                {
                    if (m_allGamePieces[column, j] != null)
                    {
                        m_allGamePieces[column, j].Move(column, i, collapseTime * (j-i));
                        m_allGamePieces[column, i] = m_allGamePieces[column, j];
                        m_allGamePieces[column, i].SetCoord(column, i);

                        if(!movingPieces.Contains(m_allGamePieces[column, i]))
                        {
                            movingPieces.Add(m_allGamePieces[column, i]);
                        }

                        m_allGamePieces[column, j] = null;
                        break;
                    }
                }
            }
        }

        return movingPieces;
    }

    private List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<int> columnsToCollapse = GetColumns(gamePieces);

        foreach(int column in columnsToCollapse)
        {
            movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
        }

        return movingPieces;
    }

    private List<int> GetColumns(List<GamePiece> gamePieces)
    {
        List<int> columns = new List<int>();

        foreach(GamePiece piece in gamePieces)
        {
            if(!columns.Contains(piece.xIndex))
            {
                columns.Add(piece.xIndex);
            }
        }

        return columns;
    }

    private void ClearAndRefillBoard(List<GamePiece> gamePieces)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
    }

    private IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
    {
        m_playerInputEnabled = false;

        yield return StartCoroutine(ClearAndCollapseRoutine(gamePieces));
        yield return null;

        m_playerInputEnabled = true;
    }

    private IEnumerator ClearAndCollapseRoutine(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<GamePiece> matches = new List<GamePiece>();
        HighlightPieces(gamePieces);

        yield return new WaitForSeconds(0.5f);

        bool isFinished = false;

        while(!isFinished)
        {
            ClearPieceAt(gamePieces);
            yield return new WaitForSeconds(0.25f);

            movingPieces = CollapseColumn(gamePieces);

            while(!IsCollapsed(movingPieces))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            matches = FindMatchesAt(movingPieces);

            if(matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
            {
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }                
        } 
         
        yield return null;
    }

    private bool IsCollapsed(List<GamePiece> gamePieces)
    {
        foreach(GamePiece piece in gamePieces)
        {
            if(piece != null)
            {
                if (piece.transform.position.y - (float)piece.yIndex > 0.001f)
                    return false;
            }
        }

        return true;
    }
}