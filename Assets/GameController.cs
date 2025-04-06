using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    public struct UpgradeData {
        public string name;
        public int price;
        public bool owned;
        public UpgradeData(string _name,int _price,bool _owned=false){
            name=_name;
            price=_price;
            owned=_owned;
        }
    }
    public List<UpgradeData> upgradeList =  new();
    
    static GameController instance;
    public GameObject shopGUI;
    public GameObject shopItem;
    public static GameController GetInstance()
    {
        return instance;
    }
    void Awake()
    {
        // enforce singleton across scenes
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            upgradeList.Add(new UpgradeData("Move Speed",100));
            upgradeList.Add(new UpgradeData("Scan Range",100));
            upgradeList.Add(new UpgradeData("Drill Strength I",200));
            upgradeList.Add(new UpgradeData("Drill Strength II",300));

            shopGUI = GameObject.Find("Shop");
            shopGUI.SetActive(false);
            PopulateShop();

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
        if(Input.GetKeyDown(KeyCode.Q)){
            shopGUI.SetActive(!shopGUI.activeSelf);
        }
    }

    void PopulateShop(){
        Transform shopContent = shopGUI.transform.GetChild(0).GetChild(0).GetChild(0);
        foreach(Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }
        foreach(UpgradeData data in upgradeList){
            GameObject item = Instantiate(shopItem,shopContent);
            item.transform.GetChild(0).GetComponent<TMP_Text>().text = data.name;
            item.transform.GetChild(1).GetComponent<TMP_Text>().text = "$"+data.price.ToString();
            if(data.owned){
                item.transform.GetChild(2).GetComponent<Button>().enabled = false;
                item.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "SOLD";
            } else {
                item.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "BUY";
            }
        }
    }
}
