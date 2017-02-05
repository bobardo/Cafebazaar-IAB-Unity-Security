using UnityEngine;
using System.Collections;
using System;
using SimpleJSON;

public class CheckIABValidate : MonoBehaviour {

    public string packageName;
    public string client_id;
    public string client_secret;
    public string refresh_token;

    private Purchase currentPurchase;
    private Action<bool, string, validateResult> callback;

    public void check(Purchase purchase, Action<bool, string, validateResult> mCallback)
    {
        if(purchase == null)
        {
            callback(false, "purchase is null", null);
            return;
        }

        currentPurchase = purchase;
        callback = mCallback;

        try
        {
            WWWForm form = new WWWForm();
            form.AddField("grant_type", "refresh_token");
            form.AddField("client_id", client_id);
            form.AddField("client_secret", client_secret);
            form.AddField("refresh_token", refresh_token);

            WWW postRequest = new WWW("https://pardakht.cafebazaar.ir/devapi/v2/auth/token/", form);
            StartCoroutine(waitForRefreshCode(postRequest));

        }
        catch
        {
            callback(false, "error requesting access code", null);
        }
    }

    IEnumerator waitForRefreshCode(WWW www)
    {
        yield return www;

        if (www.error == null)
        {
            string resultJSON = www.text;
            JSONNode json = JSON.Parse(resultJSON);

            String accessToken = json["access_token"].Value.ToString();

            string checkURL = "https://pardakht.cafebazaar.ir/devapi/v2/api/validate/" + packageName + "/inapp/"
                + currentPurchase.productId 
                + "/purchases/" + currentPurchase.orderId 
                + "/?access_token=" + accessToken;
            WWW www1 = new WWW(checkURL);
            StartCoroutine(waitForRequest(www1));
        }
        else
        {
            callback(false, "error getting access code", null);
        }
    }

    IEnumerator waitForRequest(WWW www)
    {
        yield return www;
        
        if (www.error == null)
        {
            string resultJSON = www.text;
            JSONNode json = JSON.Parse(resultJSON);

            if (json["developerPayload"].Value.ToString() == currentPurchase.payload)
            {
                validateResult result = new validateResult();
                result.isConsumed = json["consumptionState"].AsInt == 0;
                result.isRefund = json["purchaseState"].AsInt == 1;
                result.kind = json["kind"].Value.ToString();
                result.payload = json["developerPayload"].Value.ToString();
                result.time = json["purchaseTime"].Value.ToString();

                callback(true, "purchase is valid", result);
            }
            else
            {
                callback(false, "error validating purchase. payload is not valid.", null);
            }

        }
        else
        {
            callback(false, "error validating purchase. " + www.error, null);
        }
    }
}

public class validateResult
{
    public bool isConsumed;
    public bool isRefund;
    public string kind;
    public string payload;
    public string time;
}
