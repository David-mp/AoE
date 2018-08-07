using UnityEngine;

public class FollowUnit : MonoBehaviour
{
    public AudioClip MusicClip;
    public AudioSource MusicSource;

    private void Start()
    {
        GameObject Map = new GameObject();
        Map.AddComponent<MapManager>();
        Map.name = "Map";
        
        GameObject.Find("Main Camera").transform.eulerAngles = new Vector3(50, 0, 0);

        GameObject.Find("Main Camera").AddComponent<AudioSource>();
        
        string mystring = "Jobs Done";
        MusicClip = (AudioClip)Resources.Load(mystring, typeof(AudioClip));

        MusicSource = GameObject.Find("Main Camera").GetComponent<AudioSource>();

        MusicSource.clip = MusicClip;

        
    }

    
}
