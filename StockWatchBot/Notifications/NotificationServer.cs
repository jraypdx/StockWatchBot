using Newtonsoft.Json;
using PubnubApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockWatchBot.Notifications
{
    class NotificationServer
    {
        Pubnub pubnub;
        private readonly string ChannelName = "stock-notifications";

        public NotificationServer()
        {
            Init();
        }

        public void Init()
        {
            //Init
            PNConfiguration pnConfiguration = new PNConfiguration
            {
                PublishKey = PubNubKeys.Publish,
                SubscribeKey = PubNubKeys.Subscribe,
                Secure = true
            };
            pubnub = new Pubnub(pnConfiguration);
            //Subscribe
            pubnub.Subscribe<string>()
           .Channels(new string[] {
               ChannelName
           })
           .WithPresence()
           .Execute();
        }

        //Publish a message
        public void Publish(string type, string message)
        {
            JsonMsg Person = new JsonMsg
            {
                Name = type,
                Description = message,
                Date = DateTime.Now.ToString()
            };
            //Convert to string
            string arrayMessage = JsonConvert.SerializeObject(Person);
            pubnub.Publish()
                .Channel(ChannelName)
                .Message(arrayMessage)
                .Execute(new PNPublishResultExt((result, status) => { }));
        }
    }

    class JsonMsg
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
    }
}
