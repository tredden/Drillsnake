using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController instance;
    public static AudioController GetInstance()
    {
        return instance;
    }
    [SerializeField]
    List<AudioClip> songs;
    [SerializeField]
    List<AudioClip> sfx;
    [SerializeField]
    AudioClip digsfx;
    AudioSource bgm;
    AudioSource oneshots;
    AudioSource digsource;
    bool isDigging;
    int currSong;
    void Awake()
    {
        // enforce singleton across scenes
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            AudioSource[] audioSources = GetComponents<AudioSource>();
            bgm = audioSources[0];
            bgm.loop=true;
            bgm.volume=0.5f;
            
            oneshots = audioSources[1];
            digsource = audioSources[2];
            
            digsource.clip = digsfx;
            digsource.loop = true;
        } else {
            GameObject.Destroy(this.gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDigSound(float digspeed){
        Debug.Log(digspeed);
        if(digspeed<=0){
            isDigging=false;
            digsource.Stop();
        }
        else {
            if(!isDigging){
                isDigging=true;
                digsource.Play();
            }
            digsource.pitch = Mathf.Log10(digspeed)/2.1f;
        }
    }

    public void PlaySound(string sound){
        switch(sound){
            case "explode":
                oneshots.PlayOneShot(sfx[0]);
                break;
        }
    }

    public void SetMusic(int music){
        if(music==0){
            currSong=0;
            bgm.Stop();
        }
        currSong=music;
        bgm.clip=songs[music-1];
        bgm.Play();
    }
}
