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
using SKYPE4COMLib;
using System.Threading;

namespace CleverBot_Omegle
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public Skype skype;

        ChatterBotFactory botfactory;
        ChatterBot bot;

        string PandoraBotID = "e127da537e341d1a";

        bool BotResponding = true;
        bool UseDelay = true;

        Dictionary<String, ChatterBotSession> botSessions = new Dictionary<string, ChatterBotSession>();

        private void Form2_Load(object sender, EventArgs e)
        {
            botfactory = new ChatterBotFactory();

            bot = botfactory.Create(ChatterBotType.PANDORABOTS, PandoraBotID);

            skype = new Skype();
            skype.Attach(8, true);
            skype.MessageStatus += skype_MessageStatus;
        }

        void skype_MessageStatus(ChatMessage pMessage, TChatMessageStatus Status)
        {
            if (Status == TChatMessageStatus.cmsReceived && listBox1.Items.Contains(pMessage.Sender.Handle))
            {
                string response = null;

                try
                {
                    response = botSessions[pMessage.Sender.Handle].Think(pMessage.Body);
                }
                catch
                {
                    //pMessage.Chat.SendMessage("What do you mean?");
                }

                if (BotResponding)
                {

                    if (response != null)
                        SendMessage(response, pMessage.Chat);
                    else
                        SendMessage("What do you mean?", pMessage.Chat);
                }
            }
        }

        void SendMessage(string msg, Chat chat)
        {
            new Thread(delegate()
            {
                if (UseDelay)
                {
                    System.Threading.Thread.Sleep(msg.Length * (int)numericUpDown1.Value);
                }
                chat.SendMessage(msg);
            }).Start();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                listBox1.Items.Remove(listBox1.SelectedItem);
                botSessions.Remove((string)listBox1.SelectedItem);
            }
            catch
            {

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!listBox1.Items.Contains(textBox1.Text))
            {
                listBox1.Items.Add(textBox1.Text);
                botSessions.Add(textBox1.Text, bot.CreateSession());
                textBox1.Text = "";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BotResponding = !BotResponding;

            if (BotResponding)
                label2.Text = "Bot Responding: On";
            else
                label2.Text = "Bot Responding: Off";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UseDelay = !UseDelay;

            if (UseDelay)
                label3.Text = "Use Delay: On";
            else
                label3.Text = "Use Delay: Off";
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value == 0)
            {
                UseDelay = false;
                label3.Text = "Use Delay: Off";
            }
        }
    }
}
