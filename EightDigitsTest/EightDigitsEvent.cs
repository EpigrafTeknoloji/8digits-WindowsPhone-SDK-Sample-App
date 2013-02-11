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

    public static class PhoneApplicationPageEventExtender {

        public static void TriggerEvent(this PhoneApplicationPage page, String key, String value) {
            Event anEvent = new Event(key, value, page.GetHit());
            anEvent.Trigger();
        }

    }
    
    public class Event {

        public Event() {
            Timestamp = DateTime.Now;
        }

        public Event(String key, String value) {
            Key = key;
            Value = value;

            Timestamp = DateTime.Now;
        }

        public Event(String key, String value, Hit hit) {
            Key = key;
            Value = value;
            Hit = hit;

            Timestamp = DateTime.Now;
        }
        
        public String Key {
            get;
            set;
        }
        
        public String Value {
            get;
            set;
        }

        public DateTime Timestamp {
            get;
            private set;
        }

        private bool registering;

        public Hit Hit {
            get;
            set;
        }

        public String HitCode {
            get {
                if (Hit != null) {
                    return Hit.HitCode;
                }
                return null;
            }
        }

        public Visit Visit {
            get {
                if (Hit != null) {
                    return Hit.Visit;
                }
                return Visit.Current;
            }
        }

        public delegate void EventHandler(Event theEvent, EventArgs e);
        public event EventHandler OnEventTriggering;
        public event EventHandler OnEventTriggered;

        public void Trigger() {

            if (registering) {
                return;
            }

            if (Visit.Logging) {
                Debug.WriteLine("8digits: Event will trigger: " + Key + ", " + Value + ", " + (HitCode != null ? HitCode : "no hitcode"));
            }

            if (Hit != null) {
                Hit.RegisterEvent(this);
            }
            else {
                Visit.RegisterEvent(this);
            }

            if (OnEventTriggering != null) {
                OnEventTriggering(this, EventArgs.Empty);
            }

            if ((Hit != null && !Hit.Registered) || (Hit == null && !Visit.Authorised)) {
                return;
            }

            registering = true;

            String postData = "authToken=" + Visit.AuthToken + "&visitorCode=" + Visit.VisitorCode 
                + "&trackingCode=" + Visit.TrackingCode + "&sessionCode=" + Visit.SessionCode
                + "&key=" + Key + "&value=" + Value;

            if (HitCode != null) {
                postData += "&hitCode=" + HitCode;
            }
            //Debug.WriteLine(postData);
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Visit.URLPrefix + "/event/create?" + postData);
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
                            registering = false;
                            String jsonString = reader.ReadToEnd();
                            Newtonsoft.Json.Linq.JObject dictObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);

                            // Debug.WriteLine("Event result: " + jsonString);

                            if (dictObject["result"].Value<int>("code") == 0) {

                                if (OnEventTriggered != null) {
                                    OnEventTriggered(this, EventArgs.Empty);
                                }

                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Event did trigger: " + Key + ", " + Value + ", " + (HitCode != null ? HitCode : "no hitcode"));
                                }

                            }

                            else {
                                if (Visit.Logging) {
                                    Debug.WriteLine("8digits: Event did trigger: " + Key + ", " + Value + ", " + (HitCode != null ? HitCode : "no hitcode") + ", reason: " + dictObject["result"]["message"]);
                                }
                            }

                            if (OnEventTriggered != null) {
                                OnEventTriggered(this, EventArgs.Empty);
                            }

                        }
                    }

                    catch (Exception e) {
                        if (Visit.Logging) {
                            Debug.WriteLine("8digits: Event did trigger: " + Key + ", " + Value + ", " + (HitCode != null ? HitCode : "no hitcode") + ", reason: " + e.Message);
                        }
                    }

                }, request);

            }, request);

        }

    }
}
