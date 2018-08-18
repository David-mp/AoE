//#define print

using UnityEngine;
using System.Collections.Generic;



/* SET UP:
 * -Open the Script folder
 * -Drag the "FollowUnit" into the "Main Camera" Object as a new Component
 * -Run
 * -Wheel-clic to jump into the first available unit
 * 
 * POSSIBLE PROBLEMS:
 * When left-clicking a unit , it doesn't respond:
 *      -It could be an enemy unit, try another one
 *      -The "MapPlane" layer is missing: To add it, go to "layers" at the top-right corner -> Edit layers -> Write at the 9 position "MapPlane", save and rerun
 * 
 * 
 * GAME INFO
 * There are different terrains represented by a color with their own upsides/downsides:
 * -Grey: Trail - 1 Movement (--Hard to spot)
 * -Yellow: Meadow - 2 Movement (++Attack bonus, -Hard to spot)
 * -Orange: Hill - 3 Movement (+Range bonus, ++Defense bonus, +Hard to spot)
 * -Green: Forest - 3 Movement (-Range bonus, - Attack bonus, ++Defense bonus, ++Hard to spot)
 * -Brown: Mountain - 4 Movement (+Range bonus, +Attack bonus, ++Defense bonus, --Hard to spot)
 * -Blue: River - Unreachable
 * 
 * The battle system is simple. To attack, the enemy unit has to be in attack range and in vision range of yours. If the enemy survives and is in attack and vision range of your unit, it will retaliate.
 * The day and night cycle will cause each player to have a limited turn time. In addition, during the night, the units will have reduced vision and movement values, which will facilitate ambushes during this period.
 * 
 * Unit types available:
 * -Siege: They have pretty high fighting stats, but can only do 1 action per turn. This means, they can only move or attack,
 * so positioning is very important due to their low movement.
 *         
 * 
 * Controls:
 * Left clicl: Select unit / Move unit
 * Right-clic: End turn
 * Wheel-clic: Jump into the next available unit
 * 
 * */

public struct Casilla
    {
        public int x;
        public int y;
        public string Type;
        public int RangeAdaptor;
        public float AttackAdaptor;
        public float DefenseAdaptor;
        public int MovementCost;
        public int VisionCost;
        public GameObject Quad;
    }

public class MapManager : MonoBehaviour
{

    private const float TILE_SIZE = 1.0f;
    private const float TILE_OFFSET = 0.5f;

    //Map size, it can be adjusted to the player's preferences
    public const int MAP_WIDTH = 10;
    public const int MAP_HEIGHT = 10;

    public int selectionX = -1;
    public int selectionY = -1;

    //Scenery variables
    public static int cubeHeight = 4;
    public static int lateralEdge = 3;
    public static int turnCubeHeight = 6;


    //Terrain variable, it has all the terrain info, and it doesn't change over the time
    public static Casilla[,] Casillas;
    //Unit variable, it collects where all the units are at any moment
    public Unit[,] Units;

    //List of units that a player owns. Used to refresh actions when passing turns and to swap between units within a player turn.
    public List<Unit> UnitsP1;
    public List<Unit> UnitsP2;

    //List of squares that have been scouted in the game. The player will recognize the type of terrain on those, but will not know if there is an enemy unit unless there is a unit in vision range
    public List<Casilla> VisitedSquaresP1;
    public List<Casilla> VisitedSquaresP2;


    public Unit SelectedUnit;

    //1 = blue, 2 = red...
    public int PlayerTurn = 1;

    public bool isDay = true;
    public static bool changeTurn = false;


    //UnitPrefabs contains every unitPrefab needed. It has to be initializated manually with Unity UI.
    //ActiveUnits contains the GameObject of every unit, and has being created by code.
    private List<GameObject> UnitPrefabs;
    public List<GameObject> ActiveUnits;

    private void Start()
    {
        Casillas = new Casilla[MAP_WIDTH, MAP_HEIGHT];

        CreateCollisionPlane();
        SetUpPrefabs();

        //Create every Quad and place them correctly
        for (int i = 0; i < MAP_WIDTH; i++)
        {
            for (int j = 0; j < MAP_HEIGHT; j++)
            {
                    Casillas[i, j].Quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Casillas[i, j].Quad.transform.Rotate(90, 0, 0);
                    Casillas[i, j].Quad.transform.position = new Vector3(i + TILE_OFFSET, 0, j + TILE_OFFSET);
                    Casillas[i, j].Quad.transform.parent = GameObject.Find("Map").transform;
            }
        }

        SetCasillasProperties();

        //Unit setup
        SpawnAllUnits();

        //Scenery setup
        CreateScenery();
        CreateSun();

        //Vision setup
        PaintBlack();
        PaintOwnUnits();
        PaintFog();
        PaintVision();
    }

    //Create a Plane for Collisions and implementing fog of war
    private void CreateCollisionPlane()
    {
        GameObject Plane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        Plane.transform.SetParent(GameObject.Find("Map").transform);

        Plane.transform.position = new Vector3(MAP_WIDTH / 2, -0.05f, MAP_HEIGHT / 2);

        float scalex = (float)MAP_WIDTH / 10;
        float scaley = (float)MAP_HEIGHT / 10;
        Plane.transform.localScale = new Vector3(scalex, 0, scaley);

        Destroy(Plane.GetComponent<MeshCollider>());
        Plane.AddComponent<BoxCollider>();
        //Sets the layer number 9, "MapPlane" for the Plane
        Plane.layer = 9;
    }

    private void CreateSun()
    {

        GameObject Sunprefab = (GameObject)Resources.Load("SunPrefab", typeof(GameObject));
        GameObject Sun = Instantiate(Sunprefab, GetTileCenter(0, 0), Quaternion.identity) as GameObject;
        Sunprefab.name = "Sun";

        Sun.AddComponent<SunLights>();


    }

    // Update is called once per frame
    private void Update()
    {
        CheckChangeTurn();
        UpdateSelection();
        DrawMap();
        

        //Below are shown all user interactions

        //Select a Unit and Move/Battle
        if (Input.GetMouseButtonDown(0))
        {
            if(selectionX >= 0 && selectionY >= 0)
            {
                if(SelectedUnit == null)
                {
                    SelectUnit(selectionX, selectionY);
                }
                else
                {   //Unit already selected - Move/Attack order

                    //If there is no unit on the terrain, it is a move command
                    if (Units[selectionX, selectionY] == null)
                    {
                        MoveUnit(selectionX, selectionY);
                    } else
                    { //Clicked on a enemy unit
                        if((Units[selectionX, selectionY].Player != PlayerTurn))
                        {
                            //If the unit is visible, the attacker intended to attack that unit. If that is possible, attack it
                            if(IsVisible(Units[selectionX, selectionY]))
                            {
                                if (PossibleMove(SelectedUnit, selectionX, selectionY, 2))
                                {
                                    Battle(SelectedUnit, Units[selectionX, selectionY]);
                                } 
                                
                                
                            } else
                            //The enemy unit was unspotted for the attacker, so he just wanted to move in that direction
                            {
                                if(PossibleMove(SelectedUnit, selectionX, selectionY, 1))
                                {
                                    Debug.Log("UNIDAD ESCONDIDA PILLIN, TE VAS A METER EN UN LIO POR GLITCHER");
                                    Casilla destiny = SelectedUnit.Path[Casillas[selectionX, selectionY]];
                                    MoveUnit(destiny.x, destiny.y);
                                }
                                
                            }
                        }
                    }   
                }
            }
        }
        
        //End turn
        if (Input.GetMouseButtonDown(1))
        {
            changeTurn = true;
            
        }

        //Jump into the next unit
        if (Input.GetMouseButtonDown(2))
        {
            CameraOnNextUnit(PlayerTurn);
        }
    }

    //Active a SelectedUnit to interact with
    private void SelectUnit(int x, int y)
    {
        //Clicked on terrain - canceled
        if (Units[x, y] == null)
            return;

        //Clicked on a not own unit - canceled
        if (Units[x, y].Player != PlayerTurn)
            return;

        //Clicked on an own unit - success
        SelectedUnit = Units[x, y];
        

    }

    //Moves the selected unit to the position (x,y)
    private void MoveUnit(int x, int y)
    {
        if (PossibleMove(SelectedUnit, x, y, 1))
        {
            //Move the unit and its transform
            Units[SelectedUnit.CurrentX, SelectedUnit.CurrentY] = null;
            SelectedUnit.transform.position = GetTileCenter(x, y);
            Units[x, y] = SelectedUnit;
            Units[x, y].CurrentX = x;
            Units[x, y].CurrentY = y;
            Units[x, y].ActionsRemaining--;

            //Remove the old Square pool and calculate it again at the new position
            GetReachableSquares(Units[x, y], 1);
            GetReachableSquares(Units[x, y], 2);

            //Update the unit vision
            PaintFog();
            UnpaintEnemies();
            PaintVision();
        }
        SelectedUnit = null;

    }

    //Battle between two units. First, the attacker attacks, and then, if the defender is able to retaliate, their remaining units will retaliate
    private void Battle(Unit attacker, Unit defender)
    {
        //First, we calculate the unit's stats for the incoming battle (Terrain modificator, type bonusses, unit buffs...) 
        //This way, these bonusses will expire and the unit will no longer maintain them
        //Attacker stats
        int AttackStatA = (int)(attacker.BaseAttack * (1 + Casillas[attacker.CurrentX,attacker.CurrentY].AttackAdaptor));
        int DefenseStatA = (int)(attacker.BaseDefense * (1 + Casillas[attacker.CurrentX, attacker.CurrentY].DefenseAdaptor)); 
        
        //Defender stats
        int AttackStatD = (int)(defender.BaseAttack * (1 + Casillas[defender.CurrentX, attacker.CurrentY].AttackAdaptor));
        int DefenseStatD = (int)(defender.BaseDefense * (1 + Casillas[defender.CurrentX, attacker.CurrentY].DefenseAdaptor));

        //Now, the attacker will attack the oponent
        Debug.Log("Ataque:" + AttackStatA + "   --Defensa:" + DefenseStatD + "   --HpAt:" + attacker.HitPoints + "  --HpD:" + defender.HitPoints);
        defender.HitPoints = Fight(AttackStatA, DefenseStatD, attacker.HitPoints, defender.HitPoints);

        Debug.Log("BATTLE RESULT:  " + "--HpAt:" + attacker.HitPoints + "  --HpD:" + defender.HitPoints);


        //If the oponent survives and is in attack range, it will retaliate. Otherwise, it is killed
        if (defender.HitPoints <= 0)
        {
            KillUnit(defender);
        } else
        {
            if(PossibleMove(defender, attacker.CurrentX, attacker.CurrentY, 2))
            {
                Debug.Log("The defender retaliates!! ");
                Debug.Log("Ataque:" + AttackStatD + "   --Defensa:" + DefenseStatA + "   --HpAt:" + defender.HitPoints + "  --HpD:" + attacker.HitPoints);
                attacker.HitPoints = Fight(AttackStatD, DefenseStatA, defender.HitPoints, attacker.HitPoints);
                Debug.Log("BATTLE RESULT:  " +  "--HpAt:" + attacker.HitPoints + "  --HpD:" + defender.HitPoints);

                if (attacker.HitPoints <= 0)
                {
                    KillUnit(attacker);
                }
            }
        }

        SelectedUnit = null;
        attacker.ActionsRemaining = 0;
    }

    //Fight rule: Every hitpoint of the attacker unit will cause 1hitpoint damage to the defender if the attacker doubles the defender's defense
    private int Fight(int attack, int defense, int hpA, int hpD)
    {
        
        float DefensorLosses = (float)hpA  * attack / (2 * (float)defense);
        Debug.Log("Losses" + DefensorLosses);
        hpD = hpD - (int)DefensorLosses;

        return hpD;
    }

    //Draw all the cursor interactions
    private void DrawMap()
    {
        Vector3 widthLine = Vector3.right * MAP_WIDTH;
        Vector3 heightLine = Vector3.forward * MAP_HEIGHT;


        //Draw Map
        for (int i = 0; i <= MAP_WIDTH; i++)
        {
            Vector3 start = Vector3.forward * i;
            Debug.DrawLine(start, start + widthLine);

            for (int j = 0; j <= MAP_HEIGHT; j++)
            {
                start = Vector3.right * j;
                Debug.DrawLine(start, start + heightLine);
            }
        }
        //Draw Selection

        if (selectionX >= 0 && selectionY >= 0)
        {
            Debug.DrawLine(
                Vector3.forward * selectionY + Vector3.right * selectionX,
                Vector3.forward * (selectionY + 1) + Vector3.right * (selectionX + 1));

            Debug.DrawLine(
                Vector3.forward * (selectionY + 1) + Vector3.right * selectionX,
                Vector3.forward * selectionY + Vector3.right * (selectionX + 1));

            Debug.DrawLine(
                Vector3.forward * (selectionY + 0.5f) + Vector3.right * selectionX,
                Vector3.forward * (selectionY + 0.5f) + Vector3.right * (selectionX + 1));

            Debug.DrawLine(
                Vector3.forward * selectionY  + Vector3.right * (selectionX + 0.5f),
                Vector3.forward * (selectionY + 1) + Vector3.right * (selectionX + 0.5f));
        }

        //Draw PossibleMoves
        if (SelectedUnit != null)
        {
            PaintPossibleMoves(SelectedUnit);
        }

        
            

    }

    //Once the Squares are created, it fills all their fields
    private void SetCasillasProperties()
    {
        Material newMat;
        float a;

        //Asignar uno de los terrenos a cada casilla, y sus propiedades
        for (int i = 0; i < MAP_WIDTH; i++)
        {
            for (int j = 0; j < MAP_HEIGHT; j++)
            {
                a = Random.Range(0.0f, 5.3f);
                if (0 <= a && a < 1)
                {
                    newMat = Resources.Load("Forest", typeof(Material)) as Material;
                    Casillas[i, j].Quad.GetComponent<Renderer>().material = newMat;
                    Casillas[i, j].Type = "Forest";
                    Casillas[i, j].x = i;
                    Casillas[i, j].y = j;
                    Casillas[i, j].RangeAdaptor = -1;
                    Casillas[i, j].AttackAdaptor = -0.1f;
                    Casillas[i, j].DefenseAdaptor = 0.2f;
                    Casillas[i, j].MovementCost = 3;
                    Casillas[i, j].VisionCost = 5;

                }
                if (1 <= a && a < 2)
                {
                    newMat = Resources.Load("Hill", typeof(Material)) as Material;
                    Casillas[i, j].Quad.GetComponent<Renderer>().material = newMat;
                    Casillas[i, j].Type = "Hill";
                    Casillas[i, j].x = i;
                    Casillas[i, j].y = j;
                    Casillas[i, j].RangeAdaptor = 1;
                    Casillas[i, j].AttackAdaptor = 0.0f;
                    Casillas[i, j].DefenseAdaptor = 0.2f;
                    Casillas[i, j].MovementCost = 3;
                    Casillas[i, j].VisionCost = 4;
                }
                if (2 <= a && a < 3.1f)
                {
                    newMat = Resources.Load("Meadow", typeof(Material)) as Material;
                    Casillas[i, j].Quad.GetComponent<Renderer>().material = newMat;
                    Casillas[i, j].Type = "Meadow";
                    Casillas[i, j].x = i;
                    Casillas[i, j].y = j;
                    Casillas[i, j].RangeAdaptor = 0;
                    Casillas[i, j].AttackAdaptor = 0.2f;
                    Casillas[i, j].DefenseAdaptor = 0.0f;
                    Casillas[i, j].MovementCost = 2;
                    Casillas[i, j].VisionCost = 3;
                }
                if (3.1f <= a && a < 3.9f)
                {
                    newMat = Resources.Load("Mountain", typeof(Material)) as Material;
                    Casillas[i, j].Quad.GetComponent<Renderer>().material = newMat;
                    Casillas[i, j].Type = "Mountain";
                    Casillas[i, j].x = i;
                    Casillas[i, j].y = j;
                    Casillas[i, j].RangeAdaptor = 1;
                    Casillas[i, j].AttackAdaptor = 0.1f;
                    Casillas[i, j].DefenseAdaptor = 0.2f;
                    Casillas[i, j].MovementCost = 4;
                    Casillas[i, j].VisionCost = 2;
                }
                if (3.9f <= a && a < 5)
                {
                    newMat = Resources.Load("Trail", typeof(Material)) as Material;
                    Casillas[i, j].Quad.GetComponent<Renderer>().material = newMat;
                    Casillas[i, j].Type = "Trail";
                    Casillas[i, j].x = i;
                    Casillas[i, j].y = j;
                    Casillas[i, j].RangeAdaptor = 0;
                    Casillas[i, j].AttackAdaptor = 0.0f;
                    Casillas[i, j].DefenseAdaptor = 0.0f;
                    Casillas[i, j].MovementCost = 1;
                    Casillas[i, j].VisionCost = 2;
                }
                if (5 <= a && a <= 5.3f)
                {
                    newMat = Resources.Load("River", typeof(Material)) as Material;
                    Casillas[i, j].Quad.GetComponent<Renderer>().material = newMat;
                    Casillas[i, j].Type = "River";
                    Casillas[i, j].x = i;
                    Casillas[i, j].y = j;
                    Casillas[i, j].RangeAdaptor = 0;
                    Casillas[i, j].AttackAdaptor = 0.0f;
                    Casillas[i, j].DefenseAdaptor = 0.0f;
                    Casillas[i, j].MovementCost = 50;
                    Casillas[i, j].VisionCost = 3;
                }
            }

        }
    }

    //Helper that initializes the unit prefabs 
    private void SetUpPrefabs()
    {
        UnitPrefabs = new List<GameObject>();

        GameObject unitprefab = (GameObject)Resources.Load("siegeBallista", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeCatapult", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeRam", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeTrebuchet", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeBallista (red)", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeCatapult (red)", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeRam (red)", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);

        unitprefab = (GameObject)Resources.Load("siegeTrebuchet (red)", typeof(GameObject));
        UnitPrefabs.Add(unitprefab);
    }

    //Selects which units are going to be in the game aswell as their positions
    private void SpawnAllUnits()
    {
        ActiveUnits = new List<GameObject>();
        Units = new Unit[MAP_WIDTH, MAP_HEIGHT];
        UnitsP1 = new List<Unit>();
        UnitsP2 = new List<Unit>();
        VisitedSquaresP1 = new List<Casilla>();
        VisitedSquaresP2 = new List<Casilla>();


        //Spawn the blue team
        SpawnUnits(0, 3, 0);
        SpawnUnits(1, 3, 3);
        SpawnUnits(2, 5, 3);
        SpawnUnits(3, 7, 0);



        //Spawn the red team
        SpawnUnits(4, 3, 9);
        SpawnUnits(5, 3, 6);
        SpawnUnits(6, 5, 6);
        SpawnUnits(7, 7, 9);

    }

    //Create the GameObject
    private void SpawnUnits(int index, int x, int y)
    {
        GameObject go = Instantiate(UnitPrefabs[index], GetTileCenter(x,y), Quaternion.identity) as GameObject;
        go.transform.SetParent(transform);
        go.AddComponent<Unit>();
        Units[x, y] = go.GetComponent<Unit>();
        Units[x, y].SetPosition(x, y);
        Units[x, y].GameUnit = go;
        SetUnitProperties(index, x, y);
        ActiveUnits.Add(go);
    }

    //Initialize the properties of a unit with the index given
    private void SetUnitProperties(int index, int x, int y)
    {
        switch (index)
        {
            case 0:
                FillUnitProperties(Units[x, y], "SiegeBallista", "Siege", 1, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP1.Add(Units[x, y]);
                break;
            case 1:
                FillUnitProperties(Units[x, y], "SiegeCatapult", "Siege", 1, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP1.Add(Units[x, y]);
                break;
            case 2:
                FillUnitProperties(Units[x, y], "SiegeRam", "Siege", 1, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP1.Add(Units[x, y]);
                break;
            case 3:
                FillUnitProperties(Units[x, y], "SiegeBallista", "Siege", 1, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP1.Add(Units[x, y]);
                break;
            case 4:
                FillUnitProperties(Units[x, y], "SiegeBallista", "Siege", 2, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP2.Add(Units[x, y]);
                break;
            case 5:
                FillUnitProperties(Units[x, y], "SiegeCatapult", "Siege", 2, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP2.Add(Units[x, y]);
                break;
            case 6:
                FillUnitProperties(Units[x, y], "SiegeRam", "Siege", 2, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP2.Add(Units[x, y]);
                break;
            case 7:
                FillUnitProperties(Units[x, y], "SiegeTrebuchet", "Siege", 2, new int[2] { 7, 3 }, 300, 200, 3, new int[2] { 8, 4 }, 1);

                GetReachableSquares(Units[x, y], 1);
                GetReachableSquares(Units[x, y], 2);
                UnitsP2.Add(Units[x, y]);
                break;

            default:
                Debug.Log("Wrong index of UnitPrefabs");
                break;
        }
    }

    //Helper of SetUnitProperties
    private void FillUnitProperties(Unit unit, string name, string type, int player, int[] movement, int baseAttack, int baseDefense, int baseRange, int[] BaseVision, int actions)
    {
        unit.Name = name;
        unit.Type = type;
        unit.Player = player;
        unit.Movement = movement;
        unit.BaseAttack = baseAttack;
        unit.BaseDefense = baseDefense;
        unit.BaseRange = baseRange;
        unit.BaseVision = BaseVision;
        unit.ActionsRemaining = actions;

        unit.HitPoints = 100;
        unit.ReachableSquares = new List<Casilla>();
        unit.CasillasInspeccionadas = new List<Casilla>();
        unit.VisionSquares = new List<Casilla>();
        unit.Path = new Dictionary<Casilla, Casilla>();
    }

    //Remove a unit from the game
    private void KillUnit(Unit unit)
    {
        if (ActiveUnits.Contains(unit.GameUnit))
        {
            //Remove from ActiveUnits on the board

            ActiveUnits.Remove(unit.GameUnit);
            Debug.Log("Se ha eliminado la unidad, unidades activas:" + ActiveUnits.Count);

            //Remove from the array
            Units[unit.CurrentX, unit.CurrentY] = null;

            //Remove from the available units list of the player
            if (unit.Player == 1)
            {
                UnitsP1.Remove(unit);
            }
            if (unit.Player == 2)
            {
                UnitsP2.Remove(unit);
            }

            //Remove from the board
            Destroy(unit.GameUnit);

            //Check if it was the last player's unit and end the game
            if (UnitsP1.Count == 0)
            {
                Debug.Log("PLAYER 2 WINS");
                //EndGame(P2);
            }
            if (UnitsP2.Count == 0)
            {
                Debug.Log("PLAYER 1 WINS");
                //EndGame(P1);
            }
        }


    }

    private void UpdateTeamProperties()
    {
        switch (PlayerTurn)
        {
            case 1:
                foreach (Unit u in UnitsP1)
                {
                    GetReachableSquares(u, 1);
                    GetReachableSquares(u, 2);
                }
                    break;
            case 2:
                foreach (Unit u in UnitsP2)
                {
                    GetReachableSquares(u, 1);
                    GetReachableSquares(u, 2);
                }
                break;

            default:
                
                break;
        }
        
    }

    //Algorithm that fills the ReachableSquares List of a unit. It can be called for getting the squares in vision range or in movement range
    //Mode: 1-Movement mode; 2-Vision mode
    private void GetReachableSquares(Unit unit, int mode)
    {
        //A copy of ReachableSquares is necesary because you cannot iterate a collection that is changing inside the foreach loop
        List<Casilla> ReachableCopy = new List<Casilla>();

        if (mode == 1)
        {
            unit.ReachableSquares.RemoveRange(0, unit.ReachableSquares.Count);
            unit.CasillasInspeccionadas.RemoveRange(0, unit.CasillasInspeccionadas.Count);
            unit.Path.Clear();
            if (isDay)
            {
                unit.RemainingMove[unit.CurrentX, unit.CurrentY] = unit.Movement[0];
            }else
            {
                unit.RemainingMove[unit.CurrentX, unit.CurrentY] = unit.Movement[1];
            }
               
        }
        if(mode == 2)
        {
            unit.VisionSquares.RemoveRange(0, unit.VisionSquares.Count);
            if (isDay)
            {
                unit.RemainingMove[unit.CurrentX, unit.CurrentY] = unit.BaseVision[0];
            }
            else
            {
                unit.RemainingMove[unit.CurrentX, unit.CurrentY] = unit.BaseVision[1];
            }
        }

        unit.ReachableSquares.Add(Casillas[unit.CurrentX, unit.CurrentY]);


        while (unit.ReachableSquares.Count > 0)
        {
            //Before every iteration, the copy is getting removed and then, copied from the original
            ReachableCopy.RemoveRange(0, ReachableCopy.Count);
            ReachableCopy = unit.ReachableSquares.GetRange(0, unit.ReachableSquares.Count);

#if print
            Debug.Log("Reachable: " + unit.ReachableSquares.Count + "  --- Inspeccionadas: " + unit.CasillasInspeccionadas.Count);
#endif
            foreach (Casilla c in ReachableCopy)
            {
                //Inside the loop, we only modify the original List
                if(mode == 1)
                {
                    GetAdjacent(unit, c.x, c.y, unit.RemainingMove[c.x, c.y]);
                }
                if(mode == 2)
                {
                    GetAdjacentVision(unit, c.x, c.y, unit.RemainingMove[c.x, c.y]);
                }
            }
        }
    }

    //Helper of GetReachableSquares. Inspect the 4 Adjacent squares to the x,y square
    private void GetAdjacentVision(Unit unit, int x, int y, int vision)
    {

        //First, we check if the adjacent square is outside of the map. 
        //Then, if we haven't already evaluated that square and if the unit can see the terrain with its remaining vision points
        //x,y+1 
        if (y != (MAP_HEIGHT - 1))
        {
            if (!unit.VisionSquares.Contains(Casillas[x, y + 1]) && Casillas[x, y + 1].VisionCost <= vision)
            {
#if print
                Debug.Log("Coste de casilla x,y+1: [" + x + "," + (y + 1) + "] - " + casillas[x, y + 1].MovementCost);
#endif

                unit.ReachableSquares.Add(Casillas[x, y + 1]);
                unit.RemainingMove[x, y + 1] = vision - Casillas[x, y + 1].VisionCost;
            }
        }

        //x,y-1
        if (y != 0)
        {

            if (!unit.VisionSquares.Contains(Casillas[x, y - 1]) && Casillas[x, y - 1].VisionCost <= vision)
            {
#if print
                Debug.Log("Coste de casilla x,y-1: [" + x + "," + (y - 1) + "] - " + casillas[x, y - 1].MovementCost);
#endif
                unit.ReachableSquares.Add(Casillas[x, y - 1]);
                unit.RemainingMove[x, y - 1] = vision - Casillas[x, y - 1].VisionCost;
            }
        }


        //x+1,y
        if (x != (MAP_WIDTH - 1))
        {
            if (!unit.VisionSquares.Contains(Casillas[x + 1, y]) && Casillas[x + 1, y].VisionCost <= vision)
            {
#if print
                Debug.Log("Coste de casilla x+1,y: [" + (x + 1) + "," + y + "] - " + casillas[x + 1, y].MovementCost);            
#endif
                unit.ReachableSquares.Add(Casillas[x + 1, y]);
                unit.RemainingMove[x + 1, y] = vision - Casillas[x + 1, y].VisionCost;
            }
        }


        //x-1,y
        if (x != 0)
        {
            if (!unit.VisionSquares.Contains(Casillas[x - 1, y]) && Casillas[x - 1, y].VisionCost <= vision)
            {
#if print
                Debug.Log("Coste de casilla x-1,y: [" + (x - 1) + "," + y + "] - " + casillas[x - 1, y].MovementCost);
#endif
                unit.ReachableSquares.Add(Casillas[x - 1, y]);
                unit.RemainingMove[x - 1, y] = vision - Casillas[x - 1, y].VisionCost;
            }
        }


        unit.VisionSquares.Add(Casillas[x, y]);
        unit.ReachableSquares.Remove(Casillas[x, y]);
        //Add the Square to the visitedSquares List of the player, so it is visible until the end of the game
        if(unit.Player == 1)
        {
            VisitedSquaresP1.Add(Casillas[x, y]);
        }
        if (unit.Player == 2)
        {
            VisitedSquaresP2.Add(Casillas[x, y]);
        }

    }

    //Helper of GetReachableSquares. Inspect the 4 Adjacent squares to the x,y square
    private void GetAdjacent(Unit unit, int x, int y, int movement)
    {

        //First, we check if the adjacent square is outside of the map. 
        //Then, if we haven't already evaluated that square and if the unit can reach the terrain with its remaining movement points
        //x,y+1 
        if (y != (MAP_HEIGHT - 1))
        {
            if (!unit.CasillasInspeccionadas.Contains(Casillas[x, y + 1]) && Casillas[x, y + 1].MovementCost <= movement && !EnemyThere(unit, x, y))
            {
                #if print
                Debug.Log("Coste de casilla x,y+1: [" + x + "," + (y + 1) + "] - " + casillas[x, y + 1].MovementCost);
                #endif
                
                unit.ReachableSquares.Add(Casillas[x, y + 1]);
                unit.RemainingMove[x, y + 1] = movement - Casillas[x, y + 1].MovementCost;

                //Save the Path followed to reach that square
                if (!unit.Path.ContainsKey(Casillas[x, y + 1]))
                {
                    unit.Path.Add(Casillas[x, y + 1], Casillas[x, y]); 
                }
            }
        }

        //x,y-1
        if (y != 0)
        {

            if (!unit.CasillasInspeccionadas.Contains(Casillas[x, y - 1]) && Casillas[x, y - 1].MovementCost <= movement && !EnemyThere(unit, x, y))
            {
                #if print
                Debug.Log("Coste de casilla x,y-1: [" + x + "," + (y - 1) + "] - " + casillas[x, y - 1].MovementCost);
                #endif
                unit.ReachableSquares.Add(Casillas[x, y - 1]);
                unit.RemainingMove[x, y - 1] = movement - Casillas[x, y - 1].MovementCost;

                if (!unit.Path.ContainsKey(Casillas[x, y - 1]))
                {
                    unit.Path.Add(Casillas[x, y - 1], Casillas[x, y]);
                }
            }
        }


        //x+1,y
        if (x != (MAP_WIDTH - 1))
        {
            if (!unit.CasillasInspeccionadas.Contains(Casillas[x + 1, y]) && Casillas[x + 1, y].MovementCost <= movement && !EnemyThere(unit, x, y))
            {
                #if print
                Debug.Log("Coste de casilla x+1,y: [" + (x + 1) + "," + y + "] - " + casillas[x + 1, y].MovementCost);            
                #endif
                unit.ReachableSquares.Add(Casillas[x + 1, y]);
                unit.RemainingMove[x + 1, y] = movement - Casillas[x + 1, y].MovementCost;

                if (!unit.Path.ContainsKey(Casillas[x + 1, y]))
                {
                    unit.Path.Add(Casillas[x + 1, y], Casillas[x, y]);
                }
            }
        }


        //x-1,y
        if (x != 0)
        {
            if (!unit.CasillasInspeccionadas.Contains(Casillas[x - 1, y]) && Casillas[x - 1, y].MovementCost <= movement && !EnemyThere(unit, x, y))
            {
                #if print
                Debug.Log("Coste de casilla x-1,y: [" + (x - 1) + "," + y + "] - " + casillas[x - 1, y].MovementCost);
                #endif
                unit.ReachableSquares.Add(Casillas[x - 1, y]);
                unit.RemainingMove[x - 1, y] = movement - Casillas[x - 1, y].MovementCost;

                if (!unit.Path.ContainsKey(Casillas[x - 1, y]))
                {
                    unit.Path.Add(Casillas[x - 1, y], Casillas[x, y]);
                }
            }
        }


        unit.CasillasInspeccionadas.Add(Casillas[x, y]);
        unit.ReachableSquares.Remove(Casillas[x, y]);
    }

    //Helper that returns if there is an enemy in that location
    private bool EnemyThere(Unit unit, int x, int y)
    {
        if(Units[x,y] == null)
        {
            return false;
        } else
        {
            if(Units[x,y].Player != unit.Player)
            {
                return true; ;
            }
            return false;
        }
    }

    //Helper that Paints the possible moves when selecting a unit
    private void PaintPossibleMoves(Unit unit)
    {
        foreach (Casilla c in unit.CasillasInspeccionadas)
        {

            Debug.DrawLine(
            Vector3.forward * c.y + Vector3.right * c.x,
            Vector3.forward * (c.y + 1) + Vector3.right * (c.x + 1));

            Debug.DrawLine(
                Vector3.forward * (c.y + 1) + Vector3.right * c.x,
                Vector3.forward * c.y + Vector3.right * (c.x + 1));

        }
    }

    //Helper that allows an action or not
    public bool PossibleMove(Unit unit, int x, int y, int mode)
    {
        switch (mode)
        {
            //mode = 1 - The action is a move
            case 1:
                if (unit.CasillasInspeccionadas.Contains(Casillas[x, y]) && unit.ActionsRemaining != 0)
                    return true;
                break;

            //mode = 2 - The action is an attack
            case 2:
                //First we check if the defender is invisible. You cannot attack an invisible unit
                if (!IsVisible(Units[x,y]))
                {
                    
                    SelectedUnit = null;
                    return false;
                }

                //Then, if the unit still has action points
                if (unit.ActionsRemaining == 0)
                {
                    Debug.Log("No actions remaining");
                    SelectedUnit = null;
                    return false;
                }

                //Finally, if the defender is in attack range
                int attackerRange = unit.BaseRange + Casillas[unit.CurrentX, unit.CurrentY].RangeAdaptor;
                int distance = Mathf.Abs(unit.CurrentX - x) + Mathf.Abs(unit.CurrentY - y);
                
                if(distance <= attackerRange)
                {
                    Debug.Log("Enemy in range!!");
                    return true;
                }
                Debug.Log("Enemy NOT in range!!");
                break;

            default:
                Debug.Log("Wrong mode index at PossibleMove:" + mode);
                break;
        }
        SelectedUnit = null;
        return false;
    }

    //Swaps the turn and units reobtain its actions
    private void EndTurn()
    {
        if (PlayerTurn == 1)
        {
            foreach (Unit u in UnitsP1)
            {
                switch (u.Type)
                {
                    case "Siege":
                        u.ActionsRemaining = 1;
                        break;

                    default:
                        u.ActionsRemaining = 2;
                        break;
                }
            }
            PlayerTurn = 2;

            //Swaps the color cubes and texts that show the player turn
            GameObject.Find("TurnCube1").GetComponent<Renderer>().material = Resources.Load("Red", typeof(Material)) as Material;
            GameObject.Find("TurnCube2").GetComponent<Renderer>().material = Resources.Load("Red", typeof(Material)) as Material;
            GameObject.Find("TurnText1").GetComponent<TextMesh>().text = "P2 TURN";
            GameObject.Find("TurnText2").GetComponent<TextMesh>().text = "P2 TURN";

            //Sets the sun position
            SunLights.SetTimeCounter(Mathf.PI/2);

            UpdateTeamProperties();

            //Update Vision
            PaintBlack();
            PaintOwnUnits();
            PaintFog();
            PaintVision();

            SelectedUnit = null;
            return;

        }
        if (PlayerTurn == 2)
        {
            foreach (Unit u in UnitsP2)
            {
                switch (u.Type)
                {
                    case "Siege":
                        u.ActionsRemaining = 1;
                        break;

                    default:
                        u.ActionsRemaining = 2;
                        break;
                }
            }
            PlayerTurn = 1;
            isDay = !isDay;

            //Swaps the color cubes and texts that show the player turn
            GameObject.Find("TurnCube1").GetComponent<Renderer>().material = Resources.Load("River", typeof(Material)) as Material;
            GameObject.Find("TurnCube2").GetComponent<Renderer>().material = Resources.Load("River", typeof(Material)) as Material;
            GameObject.Find("TurnText1").GetComponent<TextMesh>().text = "P1 TURN";
            GameObject.Find("TurnText2").GetComponent<TextMesh>().text = "P1 TURN";


            //Sets the sun position
            SunLights.SetTimeCounter(0);

            UpdateTeamProperties();

            //Update Vision
            PaintBlack();
            PaintOwnUnits();
            PaintFog();
            PaintVision();

            if (!isDay)
            {
                GameObject.Find("Sun(Clone)").transform.GetComponent<Renderer>().material = Resources.Load("Moon", typeof(Material)) as Material;
                GameObject.Find("Directional Light").transform.eulerAngles = new Vector3(-40, -30, 0);
                GameObject.Find("Sun(Clone)").transform.GetChild(0).GetComponent<Light>().intensity = (GameObject.Find("Sun(Clone)").transform.GetChild(0).GetComponent<Light>().range / 2) * 0.8f;
                SelectedUnit = null;
                return;
            }
            else
            {
                GameObject.Find("Sun(Clone)").transform.GetComponent<Renderer>().material = Resources.Load("Sun", typeof(Material)) as Material;
                GameObject.Find("Directional Light").transform.eulerAngles = new Vector3(50, -30, 0);
                GameObject.Find("Sun(Clone)").transform.GetChild(0).GetComponent<Light>().intensity = (GameObject.Find("Sun(Clone)").transform.GetChild(0).GetComponent<Light>().range / 2) * 0.6f;
            }
            SelectedUnit = null;
            return;
        }
        

    }

    //Center the camera on your next unit available
    private void CameraOnNextUnit(int turn)
    {
        switch (turn)
        {
            case 1:
                foreach (Unit u in UnitsP1)
                {
                    if (u.ActionsRemaining != 0)
                    {
                        Camera.main.transform.position = new Vector3(u.CurrentX, 5, u.CurrentY - 3);
                        Camera.main.transform.eulerAngles = new Vector3(50, 0, 0);
                        return;
                    }
                }
                break;
            case 2:
                foreach (Unit u in UnitsP2)
                {
                    if (u.ActionsRemaining != 0)
                    {
                        Camera.main.transform.position = new Vector3(u.CurrentX, 5, u.CurrentY + 3);
                        Camera.main.transform.eulerAngles = new Vector3(130, 0, 180);
                        return;
                    }
                }
                break;
            default:
                break;
        }

        //No more units, Job's done!
        GameObject.Find("Main Camera").GetComponent<AudioSource>().Play();

    }

    //Hide every Square and unit
    private void PaintBlack()
    {
        for (int i = 0; i < MAP_WIDTH; i++)
        {
            for (int j = 0; j < MAP_HEIGHT; j++)
            {
                Casillas[i, j].Quad.GetComponent<MeshRenderer>().enabled = false;

                if(Units[i,j] != null)
                {
                    foreach (Transform child in Units[i, j].GameUnit.transform)
                    {
                        child.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
        }
    }

    //Paint the terrain spotted by the player so far
    private void PaintFog()
    {
        switch (PlayerTurn)
        {
            case 1:
                foreach (Casilla c in VisitedSquaresP1)
                {
                    
                    if (!c.Quad.GetComponent<MeshRenderer>().enabled)
                    {
                        c.Quad.GetComponent<MeshRenderer>().enabled = true;
                    }
                }
                break;

            case 2:
                foreach (Casilla c in VisitedSquaresP2)
                {
                    if (!c.Quad.GetComponent<MeshRenderer>().enabled)
                    {
                        c.Quad.GetComponent<MeshRenderer>().enabled = true;
                    }
                }
                break;
            default:
                break;

        }
    }

    //Paint enemy units in vision range of the player
    private void PaintVision()
    {
        switch (PlayerTurn)
        {
            case 1:
                foreach (Unit u in UnitsP2)
                {
                    foreach (Unit up1 in UnitsP1)
                    {
                        if (up1.VisionSquares.Contains(Casillas[u.CurrentX,u.CurrentY]))
                        {
                            foreach (Transform child in u.GameUnit.transform)
                            {
                                if (!child.GetComponent<MeshRenderer>().enabled)
                                {
                                    child.GetComponent<MeshRenderer>().enabled = true;
                                }
                            }
                        }
                    }
                }

                break;

            case 2:
                foreach (Unit u in UnitsP1)
                {
                    foreach (Unit up2 in UnitsP2)
                    {
                        if (up2.VisionSquares.Contains(Casillas[u.CurrentX, u.CurrentY]))
                        {
                            foreach (Transform child in u.GameUnit.transform)
                            {
                                if (!child.GetComponent<MeshRenderer>().enabled)
                                {
                                    child.GetComponent<MeshRenderer>().enabled = true;
                                }
                            }
                        }
                    }
                }

                break;

            default:
                break;
        }
    }

    //Used after moving a unit. If a unit that is giving vision of an enemy is moved away, it will no longer provide that vision
    private void UnpaintEnemies()
    {
        switch (PlayerTurn)
        {
            case 1:
                foreach (Unit u in UnitsP2)
                {
                    foreach (Transform child in u.GameUnit.transform)
                    {
                        if (child.GetComponent<MeshRenderer>().enabled)
                        {
                            child.GetComponent<MeshRenderer>().enabled = false;
                        }
                    }
                }

                break;

            case 2:
                foreach (Unit u in UnitsP1)
                {
                    foreach (Transform child in u.GameUnit.transform)
                    {
                        if (child.GetComponent<MeshRenderer>().enabled)
                        {
                            child.GetComponent<MeshRenderer>().enabled = false;
                        }
                    }
                }

                break;

            default:
                break;
        }
    }

    //Prints the units of the player at the start of his turn
    private void PaintOwnUnits()
    {
        switch (PlayerTurn)
        {
            case 1:

                foreach (Unit u in UnitsP1)
                {
                    foreach (Transform child in u.GameUnit.transform)
                    {
                        if (!child.GetComponent<MeshRenderer>().enabled)
                        {
                            child.GetComponent<MeshRenderer>().enabled = true;
                        }
                    }
                }

                break;

            case 2:
                foreach (Unit u in UnitsP2)
                {
                    foreach (Transform child in u.GameUnit.transform)
                    {
                        if (!child.GetComponent<MeshRenderer>().enabled)
                        {
                            child.GetComponent<MeshRenderer>().enabled = true;
                        }  
                    }
                }
                break;

            default:
                break;
        }
        
    }

    //Helper that shows if a unit is visible by the enemy
    private bool IsVisible(Unit unit)
    {
        switch (unit.Player)
        {
            case 1:
                foreach (Unit u in UnitsP2)
                {
                    if (u.VisionSquares.Contains(Casillas[unit.CurrentX, unit.CurrentY]))
                    {
                        return true;
                    }
                }
                break;
            case 2:
                foreach (Unit u in UnitsP1)
                {
                    if (u.VisionSquares.Contains(Casillas[unit.CurrentX, unit.CurrentY]))
                    {
                        return true;
                    }
                }
                break;
            default:
                break;
        }
        return false;
    }

    //Creates the decorative cubes and informative texts
    private void CreateScenery()
    {
        GameObject Scenery = new GameObject();
        Scenery.name = "Scenery";

        GameObject cubeP1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeP1.name = "CubeP1";
        cubeP1.transform.SetParent(GameObject.Find("Scenery").transform);

        cubeP1.transform.position = new Vector3(MAP_WIDTH / 2, cubeHeight / 2, MAP_HEIGHT + 5);
        cubeP1.transform.localScale = new Vector3(MAP_WIDTH + 2*lateralEdge, cubeHeight, cubeHeight);

        GameObject cubeP2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeP2.name = "CubeP2";
        cubeP2.transform.SetParent(GameObject.Find("Scenery").transform);

        cubeP2.transform.position = new Vector3(MAP_WIDTH / 2, cubeHeight / 2, -5);
        cubeP2.transform.localScale = new Vector3(MAP_WIDTH + 2 * lateralEdge, cubeHeight, cubeHeight); 

        GameObject textP1 = new GameObject();
        GameObject textP2 = new GameObject();

        textP1.name = "CheersP1";
        textP2.name = "CheersP2";
        textP1.transform.SetParent(GameObject.Find("Scenery").transform);
        textP2.transform.SetParent(GameObject.Find("Scenery").transform);

        textP1.AddComponent<TextMesh>();
        textP1.GetComponent<TextMesh>().text = "Go P1";
        textP2.AddComponent<TextMesh>();
        textP2.GetComponent<TextMesh>().text = "Go P2";

        textP1.transform.position = new Vector3((MAP_WIDTH / 2) - lateralEdge, (cubeHeight / 2) + 1, MAP_HEIGHT + lateralEdge);
        textP2.transform.position = new Vector3((MAP_WIDTH / 2) + lateralEdge, (cubeHeight / 2) + 1, -lateralEdge);
        textP2.transform.Rotate(0, 180, 0);

        textP1.transform.localScale = new Vector3(1.25f, 1.25f, 1);
        textP2.transform.localScale = new Vector3(1.25f, 1.25f, 1);

        textP1.GetComponent<TextMesh>().color = new Vector4(1, 0, 1, 1);
        textP2.GetComponent<TextMesh>().color = new Vector4(1, 0, 1, 1);

        cubeP1.GetComponent<Renderer>().material = Resources.Load("fondo", typeof(Material)) as Material;
        cubeP2.GetComponent<Renderer>().material = Resources.Load("fondo", typeof(Material)) as Material;





        //TURN CUBES-----------------------------------------------------------------------------
        
        GameObject turnCube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        turnCube1.name = "TurnCube1";
        turnCube1.transform.SetParent(GameObject.Find("Scenery").transform);

        turnCube1.transform.position = new Vector3(MAP_WIDTH + 5, turnCubeHeight / 2, MAP_HEIGHT / 2);
        turnCube1.transform.localScale = new Vector3(cubeHeight, turnCubeHeight, MAP_HEIGHT + 2*lateralEdge + 2* cubeHeight);
        

        GameObject turnCube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        turnCube2.name = "TurnCube2";
        turnCube2.transform.SetParent(GameObject.Find("Scenery").transform);

        turnCube2.transform.position = new Vector3(-5, turnCubeHeight/2 , MAP_HEIGHT / 2);
        turnCube2.transform.localScale = new Vector3(cubeHeight, turnCubeHeight, MAP_HEIGHT + 2 * lateralEdge + 2 * cubeHeight);
        

        GameObject turnText1 = new GameObject();
        GameObject turnText2 = new GameObject();

        turnText1.name = "TurnText1";
        turnText2.name = "TurnText2";
        turnText1.transform.SetParent(GameObject.Find("Scenery").transform);
        turnText2.transform.SetParent(GameObject.Find("Scenery").transform);

        turnText1.AddComponent<TextMesh>();
        turnText1.GetComponent<TextMesh>().text = "P1 TURN";
        turnText2.AddComponent<TextMesh>();
        turnText2.GetComponent<TextMesh>().text = "P1 TURN";


        turnText1.transform.position = new Vector3(MAP_WIDTH + lateralEdge, (turnCubeHeight / 2) + 1, (MAP_HEIGHT / 2) + 5.5f);
        turnText2.transform.position = new Vector3(-lateralEdge, (turnCubeHeight / 2) + 1, (MAP_HEIGHT / 2) - 5.5f);
        turnText1.transform.Rotate(0, 90, 0);
        turnText2.transform.Rotate(0, -90, 0);

        turnText1.transform.localScale = new Vector3(2, 2, 1);
        turnText2.transform.localScale = new Vector3(2, 2, 1);

        turnText1.GetComponent<TextMesh>().color = new Vector4(1, 1, 1, 1);
        turnText2.GetComponent<TextMesh>().color = new Vector4(1, 1, 1, 1);

        turnCube1.GetComponent<Renderer>().material = Resources.Load("River", typeof(Material)) as Material;
        turnCube2.GetComponent<Renderer>().material = Resources.Load("River", typeof(Material)) as Material;

    }

    //Helper that tracks the mouse position and stores it at selectionX/Y 
    private void UpdateSelection()
    {
        if (!Camera.main)
        {
            Debug.Log("No camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("MapPlane")))
        {
            //Debug.Log(hit.point);
            selectionX = (int)hit.point.x;
            selectionY = (int)hit.point.z;

        }
        else
        {
            selectionX = -1;
            selectionY = -1;
        }

    }

    //Interruption checker
    private void CheckChangeTurn()
    {
        if (changeTurn)
        {
            changeTurn = false;
            EndTurn();
        }
    }


    //Helper that generates the proper vector3 given the index x,y
    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;
        return origin;
    }
}

