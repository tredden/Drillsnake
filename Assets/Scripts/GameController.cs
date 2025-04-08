using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public enum UpgradeType
{
    FUEL_AMOUNT,
    DRILL_STRENGTH,
    MOVE_SPEED,
    TURN_SPEED,
    SALVAGE_AMOUNT,
    SPEED_CONTROL,
    HEAT_CAPACITY,
    MAX_SPEED,
    ALCHEMY,
}

public delegate void OnUpgradePurchased(int cost, UpgradeType type, float newValue);
public delegate void OnShopEnter();
public delegate void OnShopExit();

public class GameController : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeData {
        [System.Serializable]
        public struct Entry
        {
            public int price;
            public float value;
            public string description;
            public Entry(int _price, float _value, string _description)
            {
                price = _price;
                value = _value;
                description = _description;
            }
        }
        
        public string name;
        public string fullPurchaseDescription;
        public UpgradeType type;
        public List<Entry> entries;
        public float startingValue;
        public int owned;
        public bool hidden;

        static string[] romanNum = new string[11] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        public string GetShopName()
        {
            if (GetIsFullyOwned()) {
                return name + (entries.Count > 1 ? (" " + romanNum[owned]) : romanNum[0]);
            } else {
                return name + (entries.Count > 1 ? (" " + romanNum[owned + 1]) : romanNum[0]);
            }
        }
        public string GetDescription()
        {
            if (GetIsFullyOwned()) {
                return fullPurchaseDescription;
            } else {
                return entries[owned].description;
            }
        }
        public bool GetIsFullyOwned()
        {
            return owned >= entries.Count;
        }
        public int GetNextCost()
        {
            if (entries.Count == 0) {
                return 0;
            }
            if (GetIsFullyOwned()) {
                return entries[owned - 1].price;
            } else {
                return entries[owned].price;
            }
        }
        public float GetCurrentValue()
        {
            if (entries.Count == 0 || owned == 0) {
                return startingValue;
            }
            return entries[owned - 1].value;
        }
    }
    [SerializeField]
    public List<UpgradeData> upgradeList =  new();
    [SerializeField]
    TextMeshProUGUI descText;

    static GameController instance;
    public GameObject shopGUI;
    public GameObject shopItem;

    public OnUpgradePurchased onUpgradePurchased;
    public OnShopEnter onShopEnter;
    public OnShopExit onShopExit;

    bool isShowingShop = false;

    int playerGold = 0;

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
        //if (Input.GetKeyDown(KeyCode.Q)){
        //    PopulateShop();
        //    shopGUI.SetActive(!shopGUI.activeSelf);
        //}
        if (isShowingShop) {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;

            List<RaycastResult> raycastResultList = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResultList);
            foreach (RaycastResult r in raycastResultList) {
                TooltipTarget tt = r.gameObject.GetComponent<TooltipTarget>();
                if (tt != null) {
                    descText.text = tt.text;
                    break;
                }
            }
        }
        
    }

    void MarkItemHover(UpgradeData data)
    {
        Debug.Log("Mark hover: " + data.GetShopName());
        descText.text = data.GetDescription();
    }

    void MarkItemNoHover(UpgradeData data)
    {
        Debug.Log("Mark end hover: " + data.GetShopName());
        //if (activeHover == data) {
        //    tooltip.Hide();
        //    activeHover = null;
        //}
    }

    public void SetDescText(string text)
    {
        descText.text = text;
    }

    Color buyGreen = new Color(0.4156863f, 0.7450981f, 0.1882353f, 1f);
    Color buyRed = new Color(0.7450981f, 0.2853141f, 0.1882353f, 1f);
    void PopulateShop(){
        upgradeList.Sort((UpgradeData a, UpgradeData b) => {
            if (a.GetIsFullyOwned() != b.GetIsFullyOwned()) {
                return a.GetIsFullyOwned() ? 1 : -1;
            }
            int dcost = a.GetNextCost() - b.GetNextCost();
            if (dcost != 0) {
                return dcost;
            }
            return a.GetShopName().CompareTo(b.GetShopName());
        });
        Transform shopContent = shopGUI.transform.GetChild(0).GetChild(0).GetChild(0);
        foreach(Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }
        foreach(UpgradeData data in upgradeList){
            if (data.hidden || data.entries.Count == 0) { 
                continue; 
            }
            GameObject item = Instantiate(shopItem,shopContent);

            //EventTrigger et = item.GetComponent<EventTrigger>();
            //EventTrigger.Entry onMouseOver = new EventTrigger.Entry();
            //onMouseOver.eventID = EventTriggerType.PointerEnter;
            //onMouseOver.callback.AddListener((BaseEventData _) => { MarkItemHover(data); });
            //et.triggers.Add(onMouseOver);
            //onMouseOver.eventID = EventTriggerType.Select;
            //onMouseOver.callback.AddListener((BaseEventData _) => { MarkItemHover(data); });
            //et.triggers.Add(onMouseOver);
            //EventTrigger.Entry onMouseExit = new EventTrigger.Entry();
            //onMouseOver.eventID = EventTriggerType.PointerExit;
            //onMouseOver.callback.AddListener((BaseEventData _) => { MarkItemNoHover(data); });
            //et.triggers.Add(onMouseExit);
            item.GetComponent<TooltipTarget>().text = data.GetDescription();

            item.transform.GetChild(0).GetComponent<TMP_Text>().text = data.GetShopName();
            int cost = data.GetNextCost();
            TMP_Text costText = item.transform.GetChild(1).GetComponent<TMP_Text>();
            bool canAfford = cost <= playerGold;
            costText.text = "$"+cost;
            costText.color = canAfford ? buyGreen : buyRed;
            // TODO: set tooltip to description (even if purchased, for vanity text)
            string description = data.GetDescription();
            if (data.GetIsFullyOwned()){
                item.transform.GetChild(2).GetComponent<Button>().enabled = false;
                costText.alpha = 0f;
                item.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "SOLD";
            } else {
                Button b = item.transform.GetChild(2).GetComponent<Button>();
                b.interactable = canAfford;
                b.onClick.AddListener(() => { GameController.GetInstance().BuyUpgrade(data.type); }); 
                item.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "BUY";
            }
        }
    }

    public void OpenShop(int goldAmount)
    {
        playerGold = goldAmount;
        PopulateShop();
        isShowingShop = true;
        shopGUI.SetActive(true);
        onShopEnter.Invoke();
    }

    public void ExitShop()
    {
        shopGUI.SetActive(false);
        isShowingShop = false;
        onShopExit.Invoke();
    }

    public void BuyUpgrade(UpgradeType upgradeType)
    {
        Debug.Log("Attempt Buy Upgrade: " + upgradeType.ToString());
        UpgradeData data = GetDataForUpgradeType(upgradeType);
        int cost = data.GetNextCost();
        data.owned++;
        float value = data.GetCurrentValue();
        AudioController.GetInstance().PlaySound("buy");
        this.onUpgradePurchased.Invoke(cost, upgradeType, value);
        playerGold -= cost;
        PopulateShop();
    }

    UpgradeData GetDataForUpgradeType(UpgradeType upgradeType)
    {
        foreach (UpgradeData upgrade in upgradeList) {
            if (upgrade.type == upgradeType) {
                return upgrade;
            }
        }
        Debug.LogError("No upgrades found for upgrade type " + upgradeType.ToString());
        return null;
    }

    public float GetValueForUpgradeType(UpgradeType upgradeType)
    {
        return GetDataForUpgradeType(upgradeType).GetCurrentValue();
    }
}
