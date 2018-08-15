using UnityEngine;

public class SunLights : MonoBehaviour {

    float timeCounter = 0;

    public float speed;
    public float width;
    public float height;

    public Vector3 offset;

    public bool playing = false;

    public string mat;

    

    // Use this for initialization
    void Start () {
        
        speed = 0.05f;
        width = (MapManager.MAP_WIDTH + 2*MapManager.lateralEdge + 2*MapManager.cubeHeight + 2*MapManager.turnCubeHeight)/2;
        height = (MapManager.MAP_HEIGHT + 2*MapManager.lateralEdge + 2*MapManager.cubeHeight + 2*MapManager.turnCubeHeight) / 2;
        offset = new Vector3(MapManager.MAP_WIDTH/2, 0, MapManager.MAP_HEIGHT/2);

        transform.GetChild(0).GetComponent<Light>().range = Mathf.Max(width, height) + 5;
        transform.GetChild(0).GetComponent<Light>().intensity = (transform.GetChild(0).GetComponent<Light>().range / 2)*0.6f;


        transform.GetComponent<Renderer>().material = Resources.Load("Sun", typeof(Material)) as Material;
        mat = "sun";
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        timeCounter += Time.deltaTime * speed;

        float x = width*Mathf.Cos(timeCounter);
        float y = height*Mathf.Sin(timeCounter);
        float z = 0;

        float xRot = Mathf.Acos(x/width);
        xRot = xRot * Mathf.Rad2Deg;

        transform.position = new Vector3(x, y, z) + offset;
        transform.GetChild(0).transform.eulerAngles = new Vector3(xRot, -90, 0);
        

        if (y >= width - 0.0001f)
        {
            NextCycle();
        }


        if(y <= 0)
        {
            EndCicle();
        }
		
	}

    private void NextCycle()
    {
        //GameObject.Find("Main Camera").GetComponent<AudioSource>().Play();
        
    }



    private void EndCicle()
    {
        // GameObject.Find("Main Camera").GetComponent<AudioSource>().Play();
        
        
        timeCounter = 0;

        if(mat == "sun")
        {
            
            transform.GetComponent<Renderer>().material = Resources.Load("Moon", typeof(Material)) as Material;
            mat = "moon";
            GameObject.Find("Directional Light").transform.eulerAngles = new Vector3(-40, -30, 0);
            transform.GetChild(0).GetComponent<Light>().intensity = (transform.GetChild(0).GetComponent<Light>().range / 2) * 0.8f;
        } else
        {
            transform.GetComponent<Renderer>().material = Resources.Load("Sun", typeof(Material)) as Material;
            mat = "sun";
            GameObject.Find("Directional Light").transform.eulerAngles = new Vector3(50, -30, 0);
            transform.GetChild(0).GetComponent<Light>().intensity = (transform.GetChild(0).GetComponent<Light>().range / 2) * 0.6f;

        }
    }
}
