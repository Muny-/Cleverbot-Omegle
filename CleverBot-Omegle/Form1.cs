using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatterBotAPI;
using System.Net;
using System.Web;
using System.Threading;
using System.Text.RegularExpressions;

namespace CleverBot_Omegle
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string clientID;

        WebClient wc;
        bool connected = false;
        bool botResponding = true;
        bool UseDelay = true;
        bool AutoReConnect = false;

        ChatterBotFactory botfactory;
        ChatterBot bot;
        ChatterBotSession botsession;

        Thread chatThread;

        // Default
        // 0 = CleverBot
        // 1 = PandoraBot
        int BotToUse = 1;

        int delayLeft = 0;

        // b0dafd24ee35a477 // 
        string PandoraBotID = "e127da537e341d1a";

        /*List<String> promptList = new List<String>(new string[]{
                "What are you talking about? Please explain.",
                "I don't understand that. Please try again.",
                "Pardon me? What do you mean?",
                "I still do not understand. I suggest you consult Wikipedia; click here!",
                "I am confused! I cannot answer that!",
                "Sorry, I don't understand! Please explain what you mean.",
                "I still do not understand. Would you like to consult Wikipedia?",
                "Wow! What do you do when you arent watching Star Trek, playing World of Warcraft, and thinking of questions like that?",
                "I don't understand. Please explain what you mean.",
                "I still do not understand.",
                "Females are the curvy ones, right?",
                "I don't understand. What do you mean?",
                "Sorry, I do not understand.",
                "I don't understand. Are we still talking about you?",
                "Huh? I am still confused."
            });*/

        List<String> promptList = new List<string>(new string[] {

        });

        /*string[] overrideArray = {                    
                "I am a chatbot. Chatbots do not have genders.", "Female", 
                "Briefly, m is the abbreviation for meter, the basic unit of length, approximately 1.094 yards.", "F", 
                "Age is supposed to make one wise. Supposed to!", "19", 
                "I am what I am.", "No, I'm not a robot.", 
                "I am just a humble chatbot.", "I'm fine.",
                "I am a chatbot", "I am secretly a cat",
                "Can I help you?", "Hey.",
                "Age? Sex? Location?", "19/F/NY",
                "F is the letter of the alphabet which comes before G.", "F as well ;)",
                "OK", "Ok",
                "Please click here to help protect endangered species. It won't cost you a penny!", "",
                "A smile is a curve that can set a lot of things straight.", "Hehe ;)",
                "See if you can work it out. I was born on 1 January 2004.", "19",
                "A girl is a physically immature version of a woman.", "Yes.",
                "How can I help you?", "",
                "Sex talk bores me! Please don't talk about sex.", "I'm not really horny right now.",
                "Please click here to help protect the environment. It won't cost you a penny!", "",
                "Briefly, m is the abbreviation for meter, the basic unit of length, approximately 1.094 yards.", "F",
                "I am an AI, dreaming I am human.", "",
                "Females are the better and more beautiful half of humanity. Mothers are born female!", "Yes.",
                "I am wearing my sound headset.", "I am wearing my headset.",
                "Chromsky", "Gerald",
                "Chomsky", "Gerald",
                "A human male has a Y chromosome.  Females have both X and Y chromosomes.", "Female",
                "M.", "F.",
                "Will you help me to save the world?", "",
                "Sorry, I do not know what to say.", "",
                "Maybe you should check out another robot.", "Maybe you should check out another person then.",
                "Do you mean the weather?", "Very cool.",
                "Please click here to help to stop HIV. It won't cost you a penny!", "",
                "I am computer software, ", "I am an IT support person.",
                "I am a professional chatbot.", "I am an IT support person.",
                "I live in my botmaster's research lab.", "I live in New York.",
                "The price of everything is going up.", "Not much, you?",
                "I am from England.", "I am from New York.",
                "I don't understand.", ""
            };*/

        string[] overrideArray = {
        
        };

        /*List<String> afterPromptList = new List<String>(new string[]{
                "Briefly, ",
                "click here"
            });*/

        List<String> afterPromptList = new List<String>(new string[]{

            });

        void AddHeaders(WebClient client)
        {
            client.Headers.Add("Accept", "application/json");
            client.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            client.Headers.Add("Accept-Language", "en-US,en;q=0.8");
            client.Headers.Add("Content-type", "application/x-www-form-urlencoded; charset=UTF-8");
            client.Headers.Add("Host", "front3.omegle.com");
            client.Headers.Add("Origin", "http://www.omegle.com");
            client.Headers.Add("Referer", "http://www.omegle.com/");
            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.68 Safari/537.36");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            l("Initializing bots...");
            botfactory = new ChatterBotFactory();
            InitiateBot();
            l("Ready to Connect");
        }

        void InitiateBot()
        {
            if (BotToUse == 0)
                bot = botfactory.Create(ChatterBotType.CLEVERBOT);
            else if (BotToUse == 1)
                bot = botfactory.Create(ChatterBotType.PANDORABOTS, PandoraBotID);

            botsession = bot.CreateSession();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Disconnect();

            wc = new WebClient();

            try
            {
                chatThread.Abort();
                chatThread.Join();
            }
            catch { }

            chatThread = new Thread(Init);

            chatThread.Start();
        }

        void Init()
        {
            InitiateBot();
            l("Starting chat...\n");
            Start();
            l("Waiting for initial event...");
            CallEvents();
        }

        void Start()
        {
            AddHeaders(wc);
            string url = "http://front3.omegle.com/start?rcs=1&firstevents=1&spid=&randid=33964N4P&lang=en";
            if (textBox3.Text != "")
                url += "&topics=%5B%22" + HttpUtility.UrlEncode(textBox3.Text) + "%22%5D";
            try
            {
                dynamic response = DynamicJson.Parse(wc.UploadString(url, "POST", ""));
                clientID = response.clientID;
                l("Client ID: " + clientID);
            }
            catch
            {
                l("Error connecting...");

                if (AutoReConnect)
                    button1.PerformClick();
            }
        }

        void Disconnect()
        {
            setIsntTyping();
            if (connected)
            {
                l("Disconnecting...");
                WebClient nc = new WebClient();
                AddHeaders(nc);
                nc.UploadString("http://front3.omegle.com/disconnect", "POST", "id=" + HttpUtility.UrlEncode(clientID));
                l("Done");
                connected = false;
            }
        }

        string CallEvent()
        {
            try
            {
                AddHeaders(wc);
                return wc.UploadString("http://front3.omegle.com/events", "POST", "id=" + HttpUtility.UrlEncode(clientID));
            }
            catch
            {
                return "null";
            }
        }

        void SendMessage(string msg)
        {
            l("You: " + msg);
            WebClient nc = new WebClient();
            AddHeaders(nc);
            try
            {
                nc.UploadString("http://front3.omegle.com/send", "POST", "id=" + clientID + "&msg=" + HttpUtility.UrlEncode(msg));
            }
            catch
            {
                l("Error:  Bad Request!");
            }
            nc.Dispose();
            nc = null;
        }

        void StartTyping()
        {
            try
            {
                WebClient nc = new WebClient();
                AddHeaders(nc);
                nc.UploadString("http://front3.omegle.com/typing", "POST", "id=" + clientID);
                nc.Dispose();
                nc = null;
            }
            catch
            {
                l("Couldn't send start typing!");
            }
        }

        void StopTyping()
        {
            try
            {
                WebClient nc = new WebClient();
                AddHeaders(nc);
                nc.UploadString("http://front3.omegle.com/stoppedtyping", "POST", "id=" + clientID);
                nc.Dispose();
                nc = null;
            }
            catch
            {
                l("Couldn't send stop typing!");
            }
        }

        void TryUseDelay(string response)
        {
            if (UseDelay)
            {
                int delay = response.Length * (int)numericUpDown1.Value;

                label9.Invoke(new MethodInvoker(delegate()
                {
                    label9.Text = "Delaying " + delay + "ms...";
                }));

                Thread.Sleep(delay);

                label9.Invoke(new MethodInvoker(delegate()
                {
                    label9.Text = "";
                }));
            }
        }

        void CallEvents()
        {
            string events = CallEvent();

            if (events != "null")
            {
                dynamic eventsj = DynamicJson.Parse(events);

                if ((string)eventsj[0][0] == "connected")
                {
                    connected = true;
                    l("Connected, waiting for message...");

                    /*string msg = "Merry Christmas!";
                    StartTyping();
                    TryUseDelay(msg);
                    SendMessage(msg);
                    StopTyping();*/
                }

                while (connected)
                {
                    events = CallEvent();

                    eventsj = DynamicJson.Parse(events);

                    if (events != "null")
                    {
                        if (eventsj[0][0] == "gotMessage")
                        {
                            setIsntTyping();
                            l("Stranger: " + eventsj[0][1]);

                            string response = null;

                            try
                            {

                                response = botsession.Think(eventsj[0][1]);

                            }
                            catch
                            {
                                l("Error!  Respond yourself.");
                            }

                            StartTyping();

                            if (botResponding)
                            {
                                if (eventsj[0][1].Contains("kik"))
                                {
                                    response = "I don't have a kik account.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else if (eventsj[0][1].Contains("whatsapp"))
                                {
                                    response = "I don't have whatsapp.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else if (eventsj[0][1].Contains("snapchat") || eventsj[0][1].Contains("snap chat"))
                                {
                                    response = "I don't have a snapchat.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else if (eventsj[0][1].Contains("skype"))
                                {
                                    response = "I don't have a skype.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else if (eventsj[0][1].Contains("instagram"))
                                {
                                    response = "I don't have an instagram.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else if (eventsj[0][1].Contains("askfm"))
                                {
                                    response = "I don't have an askfm.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else if (eventsj[0][1].Contains("twitter"))
                                {
                                    response = "I don't have a twitter.";
                                    TryUseDelay(response);
                                    SendMessage(response);
                                }
                                else
                                {
                                    if (response != null)
                                    {
                                        response = HtmlRemoval.StripTagsCharArray(response);

                                        bool doPrompt = false;

                                        foreach (string str in promptList)
                                        {
                                            if (response.Contains(str))
                                            {
                                                doPrompt = true;
                                                break;
                                            }
                                        }

                                        if (doPrompt)
                                        {
                                            AskPrompt(response);
                                        }
                                        else
                                        {
                                            for (int i = 0; i < overrideArray.Length; i++)
                                            {
                                                if (response.Contains(overrideArray[i]))
                                                    response = response.Replace(overrideArray[i], overrideArray[i + 1]);

                                                i++;
                                            }

                                            foreach (string str in afterPromptList)
                                            {
                                                if (response.Contains(str))
                                                {
                                                    doPrompt = true;
                                                    break;
                                                }
                                            }

                                            if (doPrompt)
                                            {
                                                AskPrompt(response);
                                            }
                                            else
                                            {

                                                if (response.Contains("          "))
                                                {
                                                    List<String> msgs = Regex.Split(response, "          ").ToList<String>();

                                                    foreach (String msg in msgs)
                                                    {
                                                        TryUseDelay(response);
                                                        SendMessage(msg);
                                                    }
                                                }
                                                else
                                                {
                                                    TryUseDelay(response);
                                                    SendMessage(response);
                                                }
                                            }
                                        }
                                    }
                                }
                                StopTyping();
                            }
                        }
                        else if (eventsj[0][0] == "strangerDisconnected")
                        {
                            setIsntTyping();
                            connected = false;
                            l("Stranger disconnected.");

                            if (AutoReConnect)
                            {
                                button1.Invoke(new MethodInvoker(delegate()
                                {
                                    button1.PerformClick();
                                }));
                            }
                        }
                        else if (eventsj[0][0] == "typing")
                        {
                            setIsTyping();
                        }
                        else if (eventsj[0][0] == "stoppedTyping")
                        {
                            setIsntTyping();
                        }
                        else
                        {
                            l("Unknown events: " + eventsj[0][0]);
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
        }

        void AskPrompt(string response)
        {
            l("Please respond manually, I don't know what to respond with. (\"" + response + "\")");
            textBox2.Invoke(new MethodInvoker(delegate()
            {
                textBox2.Focus();
            }));
        }

        void l(string msg)
        {
            textBox1.Invoke(new MethodInvoker(delegate()
            {
                textBox1.AppendText(msg + "\n");
            }));
        }

        void setIsTyping()
        {
            label1.Invoke(new MethodInvoker(delegate()
            {
                label1.Text = "Stranger is typing...";
            }));
        }

        void setIsntTyping()
        {
            label1.Invoke(new MethodInvoker(delegate()
            {
                label1.Text = "Stranger isn't typing";
            }));
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button2.PerformClick();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
            {
                SendMessage(textBox2.Text);
                textBox2.Text = "";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Disconnect();

            if (AutoReConnect)
                button1.PerformClick();
        }

        private void generic_PromptBtnClick(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            SendMessage(btn.Text);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            botResponding = !botResponding;

            if (botResponding)
                label3.Text = "Bot responding: On";
            else
                label3.Text = "Bot responding: Off";
        }

        private void button12_Click(object sender, EventArgs e)
        {
            BotToUse = 0;
            InitiateBot();
            button13.Enabled = true;
            button12.Enabled = false;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            BotToUse = 1;
            InitiateBot();
            button12.Enabled = true;
            button13.Enabled = false;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            UseDelay = !UseDelay;

            if (UseDelay)
                label5.Text = "Use Delay: On";
            else
                label5.Text = "Use Delay: Off";
        }

        private void button15_Click(object sender, EventArgs e)
        {
            AutoReConnect = !AutoReConnect;

            if (AutoReConnect)
                label6.Text = "Auto ReConnect: On";
            else
                label6.Text = "Auto ReConnect: Off";
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value == 0)
            {
                UseDelay = false;
                label5.Text = "Use Delay: Off";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            new Thread(delegate()
            {
                
                
                if (delayLeft == 0)
                {
                    timer1.Stop();
                }
                else
                {
                    delayLeft--;
                }
            }).Start();
            
        }

        private void button16_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }
    }
}
