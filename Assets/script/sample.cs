using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using System.Text;


public class sample : MonoBehaviour, IStoreListener {

	public string hostname = "api.magmakick.io";
	public string appFingerPrint;

	public Text uuidText;
	public GameObject inputArea, purchaseBtns;
	public Text inputText;

	public Text mTxtConsole;

	private string token="";

	private StringBuilder consoleText = new StringBuilder("console\n");  

	// Use this for initialization
	void Start () {
		//UUID 출력.
		uuidText.text = SystemInfo.deviceUniqueIdentifier;

		//기기등록 체크.
		StartCoroutine(GetToken(appFingerPrint, uuidText.text, GetUserInfo));
	}

	//입력 enter 버튼과 연결되는 메서드.
	public void AddUserID()
	{
		StartCoroutine(PostAddUserID(appFingerPrint, uuidText.text, inputText.text, "ko-kr", GetUserInfo));
	}

	private void GetUserInfo()
	{
		StartCoroutine(GetUserInfo(token));
	}

	int CheckError(string JSONstr, out Hashtable decodeJSON) 
	{
		PrintConsole(JSONstr);
		decodeJSON = (Hashtable)MiniJSON.jsonDecode(JSONstr);
		int code = System.Convert.ToInt32(decodeJSON["result"]);
		switch(code) 
		{
			case 80101:
				PrintConsole("기기가 미등록된 상태입니다.");
				break;
			case 80202:
				PrintConsole("ID를 입력 후 사용자 등록이 필요합니다.");
				break;
			case 80204:
				PrintConsole("3~12글자 아이디를 입력하세요.");
				break;
			case 90101:
				PrintConsole("MagmaKick에 등록된 app이 아닙니다. appFingerPrint를 확인하세요.");
				break;
			case 966:
				PrintConsole("payload가 유효하지 않습니다.");
				break;
			case 967:
				PrintConsole("이미 사용된 payload입니다.");
				break;
			case 968:
				PrintConsole("developerPayload를 payload 필드로 첨부하세요");
				break;
		}
		return code;
	}

	void PrintConsole(string addText)
	{
		Debug.Log(addText);
		consoleText.AppendFormat("\n{0}",addText);
		mTxtConsole.text = consoleText.ToString();
	}

	void DoErrorAction(int code) 
	{
		switch(code) 
		{
			case 80101:
				StartCoroutine(PostAddDevice(appFingerPrint, uuidText.text, 0));
				break;
			case 80202:
				inputArea.SetActive(true);
				break;
		}
	}

	IEnumerator GetToken(string appFingerPrint, string uuid, Action getTokenCallback) 
	{
		PrintConsole("API:GetToken");
		string url = string.Format("{0}/device/token/{1}/{2}", hostname, appFingerPrint, uuid);
		WWW www = new WWW(url);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
				DoErrorAction(errCode);
			else 
			{
				token = (string)decodeJSON["token"];
				getTokenCallback();
			}
		}
	}

	IEnumerator PostAddDevice(string appFingerPrint, string uuid, int deviceType)
	{
		PrintConsole("API:PostAddDevice");
		string url = string.Format("{0}/device/add/{1}", hostname, appFingerPrint);
		WWWForm form = new WWWForm();
		form.AddField("UUID", uuid);
		form.AddField("DeviceType", deviceType);

		WWW www = new WWW(url, form);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
				DoErrorAction(errCode);
			else
			{
				inputArea.SetActive(true);
			}
		}
	}

	IEnumerator PostAddUserID(string appFingerPrint, string uuid, string UserID, string locale, Action getTokenCallback)
	{
		PrintConsole("API:PostAddUserID");
		string url = string.Format("{0}/user/add/{1}", hostname, appFingerPrint);
		WWWForm form = new WWWForm();
		form.AddField("UUID", uuid);
		form.AddField("NickName", UserID);
		form.AddField("Locale", locale);

		WWW www = new WWW(url, form);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
				DoErrorAction(errCode);
			else 
			{
				token = (string)decodeJSON["token"];
				getTokenCallback();
			}
		}
	}

	IEnumerator GetUserInfo(string token) 
	{
		PrintConsole("API:GetUserInfo");
		string url = string.Format("{0}/user/info", hostname);
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = new Dictionary<string,string>();
		headers["Authorization"] = token;

		WWW www = new WWW(url, null, headers);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
				DoErrorAction(errCode);
			else
			{
				inputArea.SetActive(false);//아이디 입력 부분 비활성화.
				purchaseBtns.SetActive(true);//결제 버튼 활성화.
			}
				
		}
	}


	//
	// --- purchase
	//
	private static IStoreController m_StoreController;          // The Unity Purchasing system.
	private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.

	/// <summary>
    /// 소모성 인앱 상품명. 
    /// </summary>
	public string kProductIDConsumable; 

	private string mPayload;

	/// <summary>
    /// 결제 가능하도록 초기화한다. 
    /// </summary>
	public void InitializePurchasing() 
	{
		// If we have already connected to Purchasing ...
		if (IsInitialized())
		{
			return;
		}
		
		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
		
	
		builder.AddProduct(kProductIDConsumable, ProductType.Consumable);
		
		UnityPurchasing.Initialize(this, builder);
	}

	/// <summary>
    /// 실제 결제를 진행한다. 
    /// </summary>
	public void BuyConsumableProduct()
	{
		#if UNITY_ANDROID
		StartCoroutine(GetPayload(token, (payload)=>{
			BuyProductID(kProductIDConsumable, payload);
		}));
		#else
			BuyProductID(kProductIDConsumable);
		#endif
	}
	
	
	private bool IsInitialized()
	{
		// Only say we are initialized if both the Purchasing references are set.
		return m_StoreController != null && m_StoreExtensionProvider != null;
	}


	 void BuyProductID(string productId, string developerPayload=null)
	{
		if (IsInitialized() == false)
		{
			PrintConsole("BuyProductID FAIL. Not initialized.");
			return;
		}

		Product product = m_StoreController.products.WithID(productId);
			 
		if (product != null && product.availableToPurchase)
		{
			PrintConsole(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
			
			if(developerPayload != null)
				m_StoreController.InitiatePurchase(product, developerPayload);
			else
				m_StoreController.InitiatePurchase(product);
		}
		else
		{
			PrintConsole("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
		}
	}
	

	/// <summary>
    /// MagmaKick에 Developer Payload를 발급요청한다. 
    /// </summary>
    /// <param name="token"></param>
    /// <param name="getPayloadCallback"></param>
    /// <returns></returns>
	IEnumerator GetPayload(string token, Action<string> getPayloadCallback) 
	{
		PrintConsole("API:GetPayload");
		string url = string.Format("{0}/payload", hostname);
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = new Dictionary<string,string>();
		headers["Authorization"] = token;

		WWW www = new WWW(url, null, headers);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
				DoErrorAction(errCode);
			else
			{
				mPayload = (string)decodeJSON["payload"];
				PrintConsole(string.Format("API:GetPayload result _ {0}", mPayload));
				getPayloadCallback(mPayload);
			}
				
		}
	}

	IEnumerator PostPayloadValidation(string token, string payload, Action postPayloadValidationCallback)
	{
		PrintConsole("API:PostPayloadValidation");
		string url = string.Format("{0}/payload/validation", hostname);
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = new Dictionary<string,string>();
		headers["Authorization"] = token;

		form.AddField("payload", payload);
		byte[] rawData = form.data;
		WWW www = new WWW(url, rawData, headers);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
				DoErrorAction(errCode);
			else
			{
				PrintConsole("API:PostPayloadValidation success");
				postPayloadValidationCallback();
			}
				
		}
	}

	IEnumerator PostReceiptValidation(string token, string receipt, Action postReceiptValidationCallback)
	{
		PrintConsole("API:PostReceiptValidation");
		string marketName = "googleiab";
		#if UNITY_IOS
		marketName = "appleiap";
		#endif
		string url = string.Format("{0}/receipt/validation/{1}", hostname, marketName);
		WWWForm form = new WWWForm();
		form.AddField("RawReceipt", receipt);
		Dictionary<string,string> headers = new Dictionary<string,string>();
		headers["Authorization"] = token;

		WWW www = new WWW(url, form.data, headers);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			PrintConsole(www.error);
		}
		else 
		{
			Hashtable decodeJSON;
			int errCode = CheckError(www.text, out decodeJSON);
			if(errCode != 0)
			{
				PrintConsole("API:PostReceiptValidation fail");
			}
			else
			{
				PrintConsole("API:PostReceiptValidation success");
				postReceiptValidationCallback();
			}
				
		}
	}

	//  
	// --- IStoreListener
	//
	
	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
	// Purchasing has succeeded initializing. Collect our Purchasing references.
	PrintConsole("OnInitialized: PASS");
	
	// Overall Purchasing system, configured with products for this application.
		m_StoreController = controller;
		// Store specific subsystem, for accessing device-specific store features.
		m_StoreExtensionProvider = extensions;
	}
        
        
	public void OnInitializeFailed(InitializationFailureReason error)
	{
		// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
		PrintConsole("OnInitializeFailed InitializationFailureReason:" + error);
	}

	string mStrReceiptJSON;

	private void ValidSuccessDeveloperPayloadCallback()
	{
		uuidText.text = "developerPayload is valid";
		StartCoroutine(PostReceiptValidation(token, mStrReceiptJSON, ()=>{
			uuidText.text = "Receipt is valid";
			PrintConsole("Receipt is valid");
		}));
	}
	
	
	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) 
	{
		//args.purchasedProduct.receipt

		PurchaseReceipt tempReceipt = JsonUtility.FromJson<PurchaseReceipt>(args.purchasedProduct.receipt); 
		
		#if UNITY_ANDROID
		GooglePayload tempGooglelPayload = JsonUtility.FromJson<GooglePayload>(tempReceipt.Payload);
		mStrReceiptJSON = tempGooglelPayload.json;
		GooglePayloadJson tempGooglePayloadJson = JsonUtility.FromJson<GooglePayloadJson>(tempGooglelPayload.json);
		if(tempGooglePayloadJson.developerPayload!=null)
		{
			StartCoroutine(PostPayloadValidation(token,tempGooglePayloadJson.developerPayload, ValidSuccessDeveloperPayloadCallback));
		}
		else 
		{
			StartCoroutine(PostReceiptValidation(token, mStrReceiptJSON, ()=>{
				PrintConsole("Receipt is valid");
			}));
		}
		#elif UNITY_IOS
		StartCoroutine(PostReceiptValidation(token, tempReceipt.Payload, ()=>{
			PrintConsole("Receipt is valid");
		}));
		#endif
		
		// A consumable product has been purchased by this user.
		// if (String.Equals(args.purchasedProduct.definition.id, kProductIDConsumable, StringComparison.Ordinal))
		// {
		//     PrintConsole(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
		//     // The consumable item has been successfully purchased, add 100 coins to the player's in-game score.
		//     ScoreManager.score += 100;
		// }
		// // Or ... a non-consumable product has been purchased by this user.
		// else if (String.Equals(args.purchasedProduct.definition.id, kProductIDNonConsumable, StringComparison.Ordinal))
		// {
		//     PrintConsole(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
		//     // TODO: The non-consumable item has been successfully purchased, grant this item to the player.
		// }
		// // Or ... a subscription product has been purchased by this user.
		// else if (String.Equals(args.purchasedProduct.definition.id, kProductIDSubscription, StringComparison.Ordinal))
		// {
		//     PrintConsole(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
		//     // TODO: The subscription item has been successfully purchased, grant this to the player.
		// }
		// // Or ... an unknown product has been purchased by this user. Fill in additional products here....
		// else 
		// {
		//     PrintConsole(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
		// }

		// Return a flag indicating whether this product has completely been received, or if the application needs 
		// to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
		// saving purchased products to the cloud, and when that save is delayed. 
		return PurchaseProcessingResult.Complete;
	}
	
	
	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
	{
		// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
		// this reason with the user to guide their troubleshooting actions.
		PrintConsole(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
	}
}

[Serializable]
public class PurchaseReceipt
{
    public string Store;
    public string TransactionID;
    public string Payload;
}

[Serializable]
public class GooglePayload
{
    public string json;
    public string signature;
}

[Serializable]
public class GooglePayloadJson
{
    public string developerPayload;
}