using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Net;
using Newtonsoft.Json.Converters;

namespace EightDigits {

    public static class PhoneApplicationPageHitExtender {

        private static Dictionary<PhoneApplicationPage, Hit> hits = new Dictionary<PhoneApplicationPage, Hit>();

        public static void PrintURL(this PhoneApplicationPage page) {
            System.Diagnostics.Debug.WriteLine("External: " + page.NavigationService.CurrentSource);
        }

        public static Hit GetHit(this PhoneApplicationPage page) {
            Hit hit;
            if (!hits.TryGetValue(page, out hit)) {
                hit = new Hit(page);
                hits[page] = hit;
            }
            return hit;
        }

        public static void StartHit(this PhoneApplicationPage page) {
            Hit hit = page.GetHit();
            if (hit.EndDate != new DateTime(0001, 01, 01)) {
                hits.Remove(page);
                hit = page.GetHit();
            }
            hit.Start();
        }

        public static void EndHit(this PhoneApplicationPage page) {
            Hit hit = page.GetHit();
            hit.End();

            if (hits.ContainsKey(page)) {
                hits.Remove(page);
            }
        }

    }

    public class Hit {

        public Hit() {
            Events = new List<Event>();
        }

        public Hit(String title, String path) {
            this.Title = title;
            this.Path = path;

            Events = new List<Event>();
        }

        public Hit(PhoneApplicationPage page) {
            this.Title = page.Title;
            this.Path = page.GetType().Name;

            Events = new List<Event>();
        }

        public List<Event> Events {
            get;
            private set;
        }

        private String hitCode;
        public String HitCode {
            get {
                if (hitCode == null) {
                    Guid hitCodeID = Guid.NewGuid();
                    hitCode = hitCodeID.ToString().Substring(0,8);
                }
                return hitCode;
            }
            private set {
                hitCode = HitCode;
            }
        }

        public Visit Visit {
            get {
                return Visit.Current;
            }
        }

        public String Title {
            get;
            set;
        }

        public String Path {
            get;
            set;
        }

        public Boolean Registered {
            get;
            private set;
        }

        public DateTime StartDate {
            get;
            set;
        }

        public DateTime EndDate {
            get;
            set;
        }

        public delegate void HitHandler(Hit sender, EventArgs e);
        public event HitHandler OnHitStarting;
        public event HitHandler OnHitStarted;
        public event HitHandler OnHitEnding;
        public event HitHandler OnHitEnded;

        public void Start() {

            Visit.RegisterHit(this);

            if (Visit.Logging) {
                Debug.WriteLine("8digits: Hit will start: " + Title + ", " + Path + ", " + HitCode);
            }

            Registered = false;
            StartDate = DateTime.Now;

            if (OnHitStarting != null) {
                OnHitStarting(this, EventArgs.Empty);
            }
            
            if (Visit.Authorised) {
                RequestStart();
            }

        }

        public void End() {

            if (Visit.Logging) {
                Debug.WriteLine("8digits: Hit will end: " + Title + ", " + Path + ", " + HitCode);
            }

            EndDate = DateTime.Now;

            if (!Registered) {
                return;
            }

            if (Events.Count > 0) {
                foreach (Event anEvent in Events) {
                    anEvent.Trigger();
                }
                return;
            }

            if (OnHitEnding != null) {
                OnHitEnding(this, EventArgs.Empty);
            }

            if (Visit.Authorised) {
                RequestEnd();
            }

        }

        private void RequestStart() {

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + Visit.VisitorCode 
                + "&trackingCode=" + Visit.TrackingCode + "&sessionCode=" + Visit.SessionCode
                + "&pageTitle=" + Title + "&path=" + Path + "&hitCode=" + HitCode;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/hit/create?" + postData);
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

                            //Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {
                                HitCode = dictObject["data"].Value<String>("hitCode");
                                Registered = true;

                                if (OnHitStarted != null) {
                                    OnHitStarted(this, EventArgs.Empty);
                                }

                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Hit did start: " + Title + ", " + Path + ", " + HitCode);
                                }

                                foreach (Event anEvent in Events) {
                                    anEvent.Trigger();
                                }

                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Hit failed to start: " + Title + ", " + Path + ", " + HitCode + ", reason: " + dictObject["result"]["message"]);
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Hit failed to start: " + Title + ", " + Path + ", " + HitCode + ", reason: " + e.Message);
                        }
                    }

                }, request);

            }, request);

        }

        private bool ending;
        private void RequestEnd() {

            if (ending) {
                return;
            }

            ending = true;
            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + Visit.VisitorCode
                + "&trackingCode=" + Visit.TrackingCode + "&sessionCode=" + Visit.SessionCode 
                + "&hitCode=" + HitCode;
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/hit/end?" + postData);
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
                            ending = false;
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                            //Debug.WriteLine(jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {

                                if (OnHitEnded != null) {
                                    OnHitEnded(this, EventArgs.Empty);
                                }

                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Hit did end: " + Title + ", " + Path + ", " + HitCode);
                                }

                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Hit did fail to end: " + Title + ", " + Path + ", " + HitCode + ", reason: " + dictObject["result"]["message"]);
                                }
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Hit did fail to end: " + Title + ", " + Path + ", " + HitCode + ", reason: " + e.Message);
                        }
                    }

                }, request);

            }, request);
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

            if (Events.Count == 0 && EndDate != new DateTime(0001, 01, 01)) {
                if (OnHitEnding != null) {
                    OnHitEnding(this, EventArgs.Empty);
                }
                RequestEnd();
            }

        }
            

    }
}
