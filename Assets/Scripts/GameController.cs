using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeType
{
    DRILL_STRENGTH,
    MOVE_SPEED,
    TURN_SPEED,
    SALVAGE_RATE,
}

public class GameController : MonoBehaviour
{
    [System.Serializable]
    public struct UpgradeData {
        [System.Serializable]
        public struct Entry
        {
            public int price;
            public float value;
            public Entry(int _price, float _value)
            {
                price = _price;
                value = _value;
            }
        }
        
        public string name;
        public UpgradeType type;
        public List<Entry> entries;
        public int owned;

        static string[] romanNum = new string[11] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        public string GetShopName()
        {
            if (GetIsFullyOwned()) {
                return name + (entries.Count > 1 ? (" " + romanNum[owned - 1]) : romanNum[0]);
            } else {
                return name + (entries.Count > 1 ? (" " + romanNum[owned]) : romanNum[0]);
            }
        }
        public bool GetIsFullyOwned()
        {
            return owned == entries.Count;
        }
        public int GetNextCost()
        {
            if (GetIsFullyOwned()) {
                return entries[owned - 1].price;
            } else {
                return entries[owned].price;
            }
        }
        public float GetCurrentValue()
        {
            if (owned == 0) {
                return 0f;
            }
            return entries[owned - 1].value;
        }

        public UpgradeData(string _name, UpgradeType _type, int price, float value=1f, bool _owned=false){
            name=_name;
            type = _type;
            entries = new List<Entry>() { new Entry(price, value) };
            owned=_owned ? 0 : 1;
        }
        public UpgradeData(string _name, UpgradeType _type, List<Entry> _entries, int _owned = 0)
        {
            name = _name;
            type = _type;
            entries = _entries;
            owned = _owned;
        }
    }
    [SerializeField]
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
            
            upgradeList.Add(new UpgradeData("Move Speed", UpgradeType.MOVE_SPEED, 100));
            // upgradeList.Add(new UpgradeData("Scan Range",100));
            upgradeList.Add(new UpgradeData("Drill Strength", UpgradeType.DRILL_STRENGTH, new List<UpgradeData.Entry>() { new UpgradeData.Entry(200, .5f), new UpgradeData.Entry(300, .7f) }));

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
            item.transform.GetChild(0).GetComponent<TMP_Text>().text = data.GetShopName();
            item.transform.GetChild(1).GetComponent<TMP_Text>().text = "$"+data.GetNextCost();
            if(data.GetIsFullyOwned()){
                item.transform.GetChild(2).GetComponent<Button>().enabled = false;
                item.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "SOLD";
            } else {
                item.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "BUY";
            }
        }
    }
}
