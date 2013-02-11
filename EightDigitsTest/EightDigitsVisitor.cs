using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Net;
using Newtonsoft.Json.Converters;

namespace EightDigits {

    public class VisitorEventArgs : EventArgs {

        public VisitorEventArgs(String error) {
            Error = error;
        }

        public String Error {
            get;
            set;
        }
    }
    
    public sealed class Visitor {

        public static int ScoreNotLoaded = int.MaxValue;

        private static readonly Visitor currentVisitor = new Visitor();
        private Visitor() {
        }
        public static Visitor Current {
            get {
                return currentVisitor;
            }
        }

        public Visit Visit {
            get {
                return Visit.Current;
            }
        }

        public String VisitorCode {
            get {
                return Visit.VisitorCode;
            }
        }

        public delegate void VisitorHandler(Visitor sender, VisitorEventArgs e);
        public event VisitorHandler OnBadgesLoaded;
        public event VisitorHandler OnScoreLoaded;
        public event VisitorHandler OnScoreIncreased;
        public event VisitorHandler OnScoreDecreased;
        public event VisitorHandler OnAttributeSet;

        private Dictionary<String, String> visitorAttributes;
        public Dictionary<String, String> VisitorAttributes {
            get {
                if (visitorAttributes == null) {

                    IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                    if (!appSettings.TryGetValue("EightDigitsVisitorAttributes", out visitorAttributes)) {
                        visitorAttributes = new Dictionary<String, String>();
                        appSettings["EightDigitsVisitorAttributes"] = visitorAttributes;
                        appSettings.Save();
                    }
                }
                return visitorAttributes;
            }
        }
        private void saveVisitorAttributes() {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            appSettings["EightDigitsVisitorAttributes"] = VisitorAttributes;
            appSettings.Save();
        }

        public Uri BadgeImageURI(String badgeID) {
            return new Uri(Visit.URLPrefix + "/badge/image/" + badgeID);
        }

        public List<String> Badges {
            get;
            private set;
        }

        public void LoadBadges() {

            String error = null;

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + VisitorCode 
                + "&trackingCode=" + Visit.TrackingCode;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/visitor/badges?" + postData);
            request.Method = "POST";

            request.ContentLength = postBytes.Length;
            request.BeginGetRequestStream((callback) => {
                System.IO.Stream stream = request.EndGetRequestStream(callback);
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                request.BeginGetResponse((responseCallback) => {

                    try {

                        HttpWebResponse response = request.EndGetResponse(responseCallback) as HttpWebResponse;

                        using (var reader = new System.IO.StreamReader(response.GetResponseStream())) {
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                           // Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                Newtonsoft.Json.Linq.JArray badgeArray = dictObject["data"].Value<Newtonsoft.Json.Linq.JArray>("badges");
                                Badges = badgeArray.ToObject<List<String>>();
                                if (OnBadgesLoaded != null) {
                                    OnBadgesLoaded(this, new VisitorEventArgs(null));
                                }
                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Badges did fail to load for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                                error = dictObject["result"]["message"].ToString();
                                if (OnBadgesLoaded != null) {
                                    OnBadgesLoaded(this, new VisitorEventArgs(error));
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Badges did fail to load for " + VisitorCode + ", reason: " + e.Message);
                        }
                        error = e.Message;
                        if (OnBadgesLoaded != null) {
                            OnBadgesLoaded(this, new VisitorEventArgs(error));
                        }
                    }

                }, request);

            }, request);

        }

        private int score = ScoreNotLoaded;
        public int Score {
            get {
                return score;
            }
            private set {
                score = value;
            }
        }

        public void LoadScore() {
            String error = null;

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + VisitorCode
                + "&trackingCode=" + Visit.TrackingCode;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/visitor/score?" + postData);
            request.Method = "POST";

            request.ContentLength = postBytes.Length;
            request.BeginGetRequestStream((callback) => {
                System.IO.Stream stream = request.EndGetRequestStream(callback);
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                request.BeginGetResponse((responseCallback) => {

                    try {

                        HttpWebResponse response = request.EndGetResponse(responseCallback) as HttpWebResponse;

                        using (var reader = new System.IO.StreamReader(response.GetResponseStream())) {
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                            // Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                Score = dictObject["data"].Value<int>("score");
                                if (OnScoreLoaded != null) {
                                    OnScoreLoaded(this, new VisitorEventArgs(null));
                                }
                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Score did fail to load for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                                error = dictObject["result"]["message"].ToString();
                                if (OnScoreLoaded != null) {
                                    OnScoreLoaded(this, new VisitorEventArgs(error));
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Score did fail to load for " + VisitorCode + ", reason: " + e.Message);
                        }
                        error = e.Message;
                        if (OnScoreLoaded != null) {
                            OnScoreLoaded(this, new VisitorEventArgs(error));
                        }
                    }

                }, request);

            }, request);
        }

        public void IncreaseScore(int increaseDelta) {
            String error = null;

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + VisitorCode
                + "&trackingCode=" + Visit.TrackingCode + "&value=" + increaseDelta.ToString();
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/score/increase?" + postData);
            request.Method = "POST";

            request.ContentLength = postBytes.Length;
            request.BeginGetRequestStream((callback) => {
                System.IO.Stream stream = request.EndGetRequestStream(callback);
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                request.BeginGetResponse((responseCallback) => {

                    try {

                        HttpWebResponse response = request.EndGetResponse(responseCallback) as HttpWebResponse;

                        using (var reader = new System.IO.StreamReader(response.GetResponseStream())) {
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                            // Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                Score = dictObject["data"].Value<int>("score");
                                if (OnScoreIncreased != null) {
                                    OnScoreIncreased(this, new VisitorEventArgs(null));
                                }
                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Score did fail to increase for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                                error = dictObject["result"]["message"].ToString();
                                if (OnScoreIncreased != null) {
                                    OnScoreIncreased(this, new VisitorEventArgs(error));
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Score did fail to increase for " + VisitorCode + ", reason: " + e.Message);
                        }
                        error = e.Message;
                        if (OnScoreIncreased != null) {
                            OnScoreIncreased(this, new VisitorEventArgs(error));
                        }
                    }

                }, request);

            }, request);
        }

        public void DecreaseScore(int decreaseDelta) {
            String error = null;

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + VisitorCode
                + "&trackingCode=" + Visit.TrackingCode + "&value=" + decreaseDelta.ToString();
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/score/decrease?" + postData);
            request.Method = "POST";

            request.ContentLength = postBytes.Length;
            request.BeginGetRequestStream((callback) => {
                System.IO.Stream stream = request.EndGetRequestStream(callback);
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                request.BeginGetResponse((responseCallback) => {

                    try {

                        HttpWebResponse response = request.EndGetResponse(responseCallback) as HttpWebResponse;

                        using (var reader = new System.IO.StreamReader(response.GetResponseStream())) {
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                            // Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                Score = dictObject["data"].Value<int>("score");
                                if (OnScoreDecreased != null) {
                                    OnScoreDecreased(this, new VisitorEventArgs(null));
                                }
                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Score did fail to decrease for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                                error = dictObject["result"]["message"].ToString();
                                if (OnScoreDecreased != null) {
                                    OnScoreDecreased(this, new VisitorEventArgs(error));
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Score did fail to decrease for " + VisitorCode + ", reason: " + e.Message);
                        }
                        error = e.Message;
                        if (OnScoreDecreased != null) {
                            OnScoreDecreased(this, new VisitorEventArgs(error));
                        }
                    }

                }, request);

            }, request);
        }

        public void SetVisitorAttribute(String key, String value) {
            String error = null;

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + VisitorCode
                + "&trackingCode=" + Visit.TrackingCode + "&key=" + key + "&value=" + value;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/visitor/setAttribute?" + postData);
            request.Method = "POST";

            request.ContentLength = postBytes.Length;
            request.BeginGetRequestStream((callback) => {
                System.IO.Stream stream = request.EndGetRequestStream(callback);
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                request.BeginGetResponse((responseCallback) => {

                    try {

                        HttpWebResponse response = request.EndGetResponse(responseCallback) as HttpWebResponse;
                        using (var reader = new System.IO.StreamReader(response.GetResponseStream())) {
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                            // Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                VisitorAttributes[key] = value;
                                saveVisitorAttributes();
                                if (OnAttributeSet != null) {
                                    OnAttributeSet(this, new VisitorEventArgs(null));
                                }
                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Did fail to set visitor attribute for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                                error = dictObject["result"]["message"].ToString();
                                if (OnAttributeSet != null) {
                                    OnAttributeSet(this, new VisitorEventArgs(error));
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Did fail to set visitor attribute for " + VisitorCode + ", reason: " + e.Message);
                        }
                        error = e.Message;
                        if (OnAttributeSet != null) {
                            OnAttributeSet(this, new VisitorEventArgs(error));
                        }
                    }

                }, request);

            }, request);

        }

    }
}
