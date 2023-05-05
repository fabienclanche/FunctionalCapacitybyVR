using StudyStore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class TestAPI : MonoBehaviour
{ 
    public bool onlineAPI = true;
    private API api;

    public bool sendRequest = false;

    public bool loggedIn = false;

    public string login, password;

    public void Update()
    {
        loggedIn = false;

        if (onlineAPI && api as OnlineAPI == null) api = new OnlineAPI();
        if (!onlineAPI && api as DummyAPI == null) api = new DummyAPI();

        if (api == null) return;

        loggedIn = api.CurrentSubject != null;

        if (!sendRequest) return;

        sendRequest = false;

        if ((login ?? "").Length > 0 && (password ?? "").Length > 0)
            api.Login(login, password, u => Debug.Log(JSONSerializer.ToJSON(u)), err => Debug.Log(err));
    }
}
