using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneController : MonoBehaviour
{
    static SceneController instance;
    [SerializeField]
    string titleScene;
    [SerializeField]
    string gameScene;
    public string currScene;
    public static SceneController GetInstance()
    {
        return instance;
    }
    void Awake()
    {
        // enforce singleton across scenes
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            Scene s = SceneManager.GetActiveScene();
            if (s.name == titleScene) {
                AudioController audio = AudioController.GetInstance();
                if(audio!=null){
                    audio.SetMusic(2);
                }
            } else {
                AudioController audio = AudioController.GetInstance();
                if(audio!=null){
                    audio.SetMusic(1);
                }
            }
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
        if(Input.GetKeyDown(KeyCode.Escape)) {
            if (SceneManager.GetActiveScene().name != titleScene) {
                LoadTitle();
            }
        }
    }
    public void LoadTitle()
    {
        SceneManager.LoadScene(titleScene, LoadSceneMode.Single);
        currScene = "Title";
        AudioController audio = AudioController.GetInstance();
        if(audio!=null){
            StartCoroutine(DelayStop());
            audio.SetMusic(2);
        }
    }

    IEnumerator DelayStop(){
        yield return new WaitForSeconds(0.5f);
        AudioController audio = AudioController.GetInstance();
        audio.SetDigSound(0);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
        currScene = "Game";
        AudioController audio = AudioController.GetInstance();
        if(audio!=null){
            audio.SetMusic(1);
        }
    }

    
}
