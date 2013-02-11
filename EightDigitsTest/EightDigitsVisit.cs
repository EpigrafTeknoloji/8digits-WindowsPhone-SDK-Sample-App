using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using System.Net.NetworkInformation;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Net;
using Newtonsoft.Json.Converters;

namespace EightDigits {

    public sealed class Visit {

        private static readonly Visit currentEvent = new Visit();

        private Visit() {
            Hits = new List<Hit>();
            NonRegisteredHits = new List<Hit>();
            Events = new List<Event>();
        }

        public static Visit Current {
            get {
                return currentEvent;
            }
        }

        public Boolean Logging {
            get;
            set;
        }

        private String urlPrefix;
        public String URLPrefix {
            get {
                return urlPrefix;
            }
            set {
                if (!value.StartsWith("http://") && !value.StartsWith("https://")) {
                    value = "http://" + value;
                }
                if (value.EndsWith("/")) {
                    value = value.Substring(0, value.Length - 1);
                }
                urlPrefix = value;
            }
        }

        public String TrackingCode {
            get;
            set;
        }

        public String AuthToken {
            get;
            private set;
        }

        private String visitorCode;
        public String VisitorCode {
            get {
                if (visitorCode == null) {

                    IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                    if (!appSettings.TryGetValue("EightDigitsVisitorCode", out visitorCode)) {
                        Guid visitorCodeGuid = Guid.NewGuid();
                        visitorCode = visitorCodeGuid.ToString().Substring(0, 8);
                        appSettings["EightDigitsVisitorCode"] = visitorCode;
                        appSettings.Save();
                    }

                }
                return visitorCode;
            }
        }

        public String SessionCode {
            get;
            private set;
        }

        public String Username {
            get;
            set;
        }

        public String Password {
            get;
            set;
        }

        public Boolean Authorised {
            get;
            private set;
        }

        public List<Hit> Hits {
            get;
            private set;
        }

        public List<Hit> NonRegisteredHits {
            get;
            private set;
        }

        public List<Event> Events {
            get;
            private set;
        }

        private DateTime startDate;
        private DateTime endDate;
        private Boolean authorising;

        public void Start() {
            if (URLPrefix == null || TrackingCode == null || Username == null || Password == null) {
                Debug.WriteLine("8digits: Visit did fail to start for " + VisitorCode + ", reason: username, password, tracking code or url prefix not set");
                return;
            }

            startDate = DateTime.Now;
            Authorise();
        }

        public void Start(String username, String password, String trackingCode, String urlPrefix) {
            this.Username = username;
            this.Password = password;
            this.TrackingCode = trackingCode;
            this.URLPrefix = urlPrefix;
            Start();
        }

        public void Start(String authToken) {
            startDate = DateTime.Now;
            authorising = false;
            Authorised = true;
            AuthToken = authToken;

            RequestStart();
        }

        public void End() {
            endDate = DateTime.Now;

            RequestEnd();

            if (Logging) {
                Debug.WriteLine("8digits: Visit will end for " + VisitorCode);
            }
        }

        private void Authorise() {
            
            if (authorising) {
                return;
            }

            if (Logging) {
                Debug.WriteLine("8digits: Visit will start for " + VisitorCode);
            }

            HttpWebRequest request = HttpWebRequest.CreateHttp(URLPrefix + "/auth?username=" + Username + "&password=" + Password);
            request.Method = "POST";

            String postData = "username=" + Username + "&password=" + Password;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

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

                            if (dictObject["result"].Value<int>("code") == 0) {
                                AuthToken = dictObject["data"].Value<String>("authToken");
                                RequestStart();
                            }

                            else {
                                if (Logging) {
                                    Debug.WriteLine("8digits: Visit did fail to start for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                            }

                        }

                    }

                    catch (Exception e) {
                        Debug.WriteLine("8digits: Visit did fail to authorise for " + VisitorCode + ", reason: " + e.Message);
                    }

                }, request);

            }, request);
            
        }

        private void RequestStart() {

            String postData = "authToken=" + AuthToken + "&visitorCode=" + VisitorCode + "&trackingCode=" + TrackingCode;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(URLPrefix + "/visit/create?" + postData);
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
                                SessionCode = dictObject["data"].Value<String>("sessionCode");
                                Succeed();
                            }

                            else {
                                if (Logging) {
                                    Debug.WriteLine("8digits: Visit did fail to start for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Logging) {
                            Debug.WriteLine("8digits: Visit did fail to start for " + VisitorCode + ", reason: " + e.Message);
                        }
                    }

                }, request);

            }, request);

        }

        private void RequestEnd() {
            String postData = "authToken=" + AuthToken + "&visitorCode=" + VisitorCode 
                + "&trackingCode=" + TrackingCode + "&sessionCode=" + SessionCode;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(URLPrefix + "/visit/end?" + postData);
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

                            Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                if (Logging) {
                                    Debug.WriteLine("8digits: Visit did end for " + VisitorCode);
                                }
                            }

                            else {
                                if (Logging) {
                                    Debug.WriteLine("8digits: Visit did fail to end for " + VisitorCode + ", reason: " + dictObject["result"]["message"]);
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Logging) {
                            Debug.WriteLine("8digits: Visit did fail to end for " + VisitorCode + ", reason: " + e.Message);
                        }
                    }

                }, request);

            }, request);
        }

        private void Succeed() {
            Authorised = true;
            authorising = false;

            foreach (Hit hit in Hits) {
                hit.End();
            }

            foreach (Event anEvent in Events) {
                anEvent.Trigger();
            }

            foreach (Hit hit in NonRegisteredHits) {
                hit.Start();
            }

            if (Logging) {
                Debug.WriteLine("8digits: Visit did start for " + VisitorCode);
            }

        }

        public void RegisterHit(Hit hit) {
            hit.OnHitStarting += hit_OnHitStarting;
            hit.OnHitStarted += hit_OnHitStarted;
            hit.OnHitEnding += hit_OnHitEnding;
            hit.OnHitEnded += hit_OnHitEnded;
        }

        public void DeregisterHit(Hit hit) {
            hit.OnHitStarting -= hit_OnHitStarting;
            hit.OnHitStarted -= hit_OnHitStarted;
            hit.OnHitEnding -= hit_OnHitEnding;
            hit.OnHitEnded -= hit_OnHitEnded;
        }

        void hit_OnHitStarting(Hit sender, EventArgs e) {

            if (!NonRegisteredHits.Contains(sender)) {
                NonRegisteredHits.Add(sender);
            }

            if (Hits.Contains(sender)) {
                Hits.Remove(sender);
            }

        }

        void hit_OnHitStarted(Hit sender, EventArgs e) {

            if (!Hits.Contains(sender)) {
                Hits.Add(sender);
            }

            if (NonRegisteredHits.Contains(sender)) {
                NonRegisteredHits.Remove(sender);
            }

            if (sender.EndDate != new DateTime(0001, 01, 01)) {
                sender.End();
            }

        }

        void hit_OnHitEnding(Hit sender, EventArgs e) {
        }

        void hit_OnHitEnded(Hit sender, EventArgs e) {

            if (Hits.Contains(sender)) {
                Hits.Remove(sender);
            }

            if (NonRegisteredHits.Contains(sender)) {
                NonRegisteredHits.Remove(sender);
            }

            DeregisterHit(sender);

        }

        public void RegisterEvent(Event theEvent) {
            theEvent.OnEventTriggering += theEvent_OnEventTriggering;
            theEvent.OnEventTriggered += theEvent_OnEventTriggered;
        }

        public void DeregisterEvent(Event theEvent) {
            theEvent.OnEventTriggering -= theEvent_OnEventTriggering;
            theEvent.OnEventTriggered -= theEvent_OnEventTriggered;
        }

        void theEvent_OnEventTriggering(Event theEvent, EventArgs e) {
            if (!Events.Contains(theEvent)) {
                Events.Add(theEvent);
            }
        }

        void theEvent_OnEventTriggered(Event theEvent, EventArgs e) {
            DeregisterEvent(theEvent);
            if (Events.Contains(theEvent)) {
                Events.Remove(theEvent);
            } 
        }

        public void TriggerEvent(String key, String value) {
            Event anEvent = new Event(key, value);
            anEvent.Trigger();
        }

    }
}
