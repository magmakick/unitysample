using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class sample : MonoBehaviour {

	public string hostname = "api.magmakick.io";
	public string appFingerPrint;

	public Text uuidTex;
	public GameObject inputArea;
	public Text inputText;

	private string token="";

	// Use this for initialization
	void Start () {
		//UUID 출력.
		uuidTex.text = SystemInfo.deviceUniqueIdentifier;

		//기기등록 체크.
		StartCoroutine(GetToken(appFingerPrint, uuidTex.text, GetUserInfo));
	}

	//입력 enter 버튼과 연결되는 메서드.
	public void AddUserID()
	{
		StartCoroutine(PostAddUserID(appFingerPrint, uuidTex.text, inputText.text, "ko-kr", GetUserInfo));
	}

	private void GetUserInfo()
	{
		StartCoroutine(GetUserInfo(token));
	}

	void CheckError(string JSONstr) 
	{
		Debug.Log(JSONstr);
		Hashtable decodeJSON = (Hashtable)MiniJSON.jsonDecode(JSONstr);
		int code = System.Convert.ToInt32(decodeJSON["result"]);
		switch(code) 
		{
			case 80101:
				Debug.Log("기기가 미등록된 상태입니다.");
				StartCoroutine(PostAddDevice(appFingerPrint, uuidTex.text, 0));
				break;
			case 80202:
				Debug.Log("ID를 입력 후 사용자 등록이 필요합니다.");
				inputArea.SetActive(true);
				break;
			case 80204:
				Debug.Log("3~12글자 아이디를 입력하세요.");
				break;
		}
	}

	IEnumerator GetToken(string appFingerPrint, string uuid, Action getTokenCallback) 
	{
		Debug.Log("API:GetToken");
		string url = string.Format("{0}/device/token/{1}/{2}", hostname, appFingerPrint, uuid);
		WWW www = new WWW(url);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			Debug.Log(www.error);
		}
		else 
		{
			Hashtable decodeJSON = (Hashtable)MiniJSON.jsonDecode(www.text);
			int code = System.Convert.ToInt32(decodeJSON["result"]);
			if(code != 0)
				CheckError(www.text);
			else 
			{
				token = (string)decodeJSON["token"];
				getTokenCallback();
			}
		}
	}

	IEnumerator PostAddDevice(string appFingerPrint, string uuid, int deviceType)
	{
		Debug.Log("API:PostAddDevice");
		string url = string.Format("{0}/device/add/{1}", hostname, appFingerPrint);
		WWWForm form = new WWWForm();
		form.AddField("UUID", uuid);
		form.AddField("DeviceType", deviceType);

		WWW www = new WWW(url, form);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			Debug.Log(www.error);
		}
		else 
		{
			Hashtable decodeJSON = (Hashtable)MiniJSON.jsonDecode(www.text);
			int code = System.Convert.ToInt32(decodeJSON["result"]);
			if(code != 0)
				CheckError(www.text); 
		}
	}

	IEnumerator PostAddUserID(string appFingerPrint, string uuid, string UserID, string locale, Action getTokenCallback)
	{
		Debug.Log("API:PostAddUserID");
		string url = string.Format("{0}/user/add/{1}", hostname, appFingerPrint);
		WWWForm form = new WWWForm();
		form.AddField("UUID", uuid);
		form.AddField("NickName", UserID);
		form.AddField("Locale", locale);

		WWW www = new WWW(url, form);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			Debug.Log(www.error);
		}
		else 
		{
			Hashtable decodeJSON = (Hashtable)MiniJSON.jsonDecode(www.text);
			int code = System.Convert.ToInt32(decodeJSON["result"]);
			if(code != 0)
				CheckError(www.text);
			else 
			{
				token = (string)decodeJSON["token"];
				getTokenCallback();
			}
		}
	}

	IEnumerator GetUserInfo(string token) 
	{
		Debug.Log("API:GetUserInfo");
		string url = string.Format("{0}/user/info", hostname);
		WWWForm form = new WWWForm();
		Dictionary<string,string> headers = new Dictionary<string,string>();
		headers["Authorization"] = token;

		WWW www = new WWW(url, null, headers);

		yield return www;

		if(!string.IsNullOrEmpty(www.error)) 
		{
			Debug.Log(www.error);
		}
		else 
		{
			Hashtable decodeJSON = (Hashtable)MiniJSON.jsonDecode(www.text);
			int code = System.Convert.ToInt32(decodeJSON["result"]);
			if(code != 0)
				CheckError(www.text);
			else
			{
				inputArea.SetActive(false);
				Debug.Log(www.text);
			}
				
		}
	}
}
