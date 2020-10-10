using Newtonsoft.Json;
using PubnubApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NotificationReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Pubnub pubnub;
        private readonly string ChannelName = "stock-notifications";

        public MainWindow()
        {
            InitializeComponent();

            //Init
            PNConfiguration pnConfiguration = new PNConfiguration
            {
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

            Listen();
        }

        public void Listen()
        {
            SubscribeCallbackExt listenerSubscribeCallack = new SubscribeCallbackExt(
            (pubnubObj, message) => {
                //Call the notification windows from the UI thread
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    //Show the message as a WPF window message like WIN-10 toast
                    //NotificationWindow ts = new NotificationWindow();
                    //Convert the message to JSON
                    JsonMsg bsObj = JsonConvert.DeserializeObject<JsonMsg>(message.Message.ToString());
                    string messageBoxText = "Name: " + bsObj.Name + "\n" + "Description: " + bsObj.Description + "\n" + "Date: " + bsObj.Date;
                    MessageBox.Show(messageBoxText);
                    //ts.NotifText.Text = messageBoxText;
                    //ts.Show();
                }));
            },
            (pubnubObj, presence) => {
                // handle incoming presence data
            },
            (pubnubObj, status) => {
                // the status object returned is always related to subscribe but could contain
                // information about subscribe, heartbeat, or errors
                // use the PNOperationType to switch on different options
            });

            pubnub.AddListener(listenerSubscribeCallack);
        }
    }

    class JsonMsg
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
    }
}
