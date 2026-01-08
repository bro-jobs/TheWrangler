//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;
namespace ff14bot.NeoProfiles.Tags
{

    [XmlElement("Log")]
    public class Log : ProfileBehavior
    {
        [XmlAttribute("Message")]
        public string Message { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }


        public Color Color { get; set; } = Colors.LightGreen;

        [XmlAttribute("Color")]
        public string ColorString
        {
            get => Color.ToString();

            set
            {
                try
                {
                    Color = (Color)ColorConverter.ConvertFromString(value);
                }
                catch (Exception ex)
                {
                    LogSoftError(ex.Message + " - Using default color, are you missing the '#'?");
                }
            }
        }

        private bool _isdone;
        public override bool IsDone => _isdone;

        public override bool HighPriority => true;
        protected override void OnResetCachedDone()
        {
            _isdone = false;
        }

        protected override void OnStart()
        {
        }

        protected override void OnDone()
        {
        }

        protected void execute()
        {

            if (!string.IsNullOrWhiteSpace(Name))
            {
                if (!string.IsNullOrWhiteSpace(Message))
                {
                    LogName(Color, Name, Message);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(Message))
                {
                    Log(Color, Message);
                }
            }

            _isdone = true;
        }

        protected override Composite CreateBehavior()
        {
            return new Action(r => execute());
        }

    }
}
