using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Unit : MonoBehaviour {

	
    public int CurrentX { get; set; }
    public int CurrentY { get; set; }

    public string Name;
    public string Type;
    public int Player;
    public int Movement;
    public int BaseAttack;
    public int BaseDefense;
    public int BaseRange;
    public int BaseVision;
    public int HitPoints;
    public int ActionsRemaining;

    public GameObject GameUnit;

    
    public List<Casilla> ReachableSquares;
    public List<Casilla> CasillasInspeccionadas;
    public int[,] RemainingMove = new int[MapManager.MAP_WIDTH, MapManager.MAP_HEIGHT];

    public void SetPosition(int x, int y)
    {
        CurrentX = x;
        CurrentY = y;
    }
    


    

    






}
