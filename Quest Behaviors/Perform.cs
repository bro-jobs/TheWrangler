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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    /*
<?xml version="1.0" encoding="UTF-8"?>
<Profile>
   <Name>Perform music test</Name>
   <KillRadius>1</KillRadius>
   <Order>
      <Performance>
         <PerformNotes>
            <!--Delay attribute is optional, defaults to 250ms -->
            <PerformNote Delay="300" Note="C+1" />
            <PerformNote Delay="300" Note="C+1" />
            <PerformNote Delay="300" Note="C+1" />
            <PerformNote Delay="300" Note="C+1" />
            <PerformNote Delay="300" Note="G#" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="C+1" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="C+1" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="A" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="A" />
            <PerformNote Delay="300" Note="A" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="E" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="D" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="A" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="A" />
            <PerformNote Delay="300" Note="A" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="F" />
            <PerformNote Delay="300" Note="G" />
            <PerformNote Delay="300" Note="B♭" />
            <PerformNote Delay="300" Note="C+1" />
         </PerformNotes>
      </Performance>
   </Order>
   <GrindAreas />
   <CodeChunks />
</Profile>
*/

    [XmlElement("Performance")]
    [XmlElement(XmlEngine.GENERIC_BODY)]
    class Performance : ProfileBehavior
    {

        [XmlElement("PerformNote")]
        public class PerformNote
        {
            [DefaultValue(250)]
            [XmlAttribute("Delay")]
            public int Delay { get; set; }

            [DefaultValue("")]
            [XmlAttribute("Note")]
            public string Note { get; set; }
        }

        static Performance()
        {
            foreach (var noteaction in NoteActionMapping.ToArray())
            {
                foreach (var charmap in RemapedCharactersDictionary)
                {
                    if (noteaction.Key.Contains(charmap.Key))
                    {
                        NoteActionMapping[noteaction.Key.Replace(charmap.Key, charmap.Value)] = noteaction.Value;
                    }
                }
            }

        }

        internal static Dictionary<string, uint> NoteActionMapping = new Dictionary<string, uint>()
        {

            {"C(-1)"     ,1     },
            {"C♯(-1)"    ,2     },
            {"D(-1)"     ,3     },
            {"E♭(-1)"    ,4     },
            {"E(-1)"     ,5     },
            {"F(-1)"     ,6     },
            {"F♯(-1)"    ,7     },
            {"G(-1)"     ,8     },
            {"G♯(-1)"    ,9     },
            {"A(-1)"     ,10    },
            {"B♭(-1)"    ,11    },
            {"B(-1)"     ,12    },
            {"C"      ,13   },
            {"C♯"      ,14  },
            {"D"      ,15   },
            {"E♭"      ,16  },
            {"E"      ,17   },
            {"F"      ,18   },
            {"F♯"      ,19  },
            {"G"      ,20   },
            {"G♯"      ,21  },
            {"A"      ,22   },
            {"B♭"      ,23  },
            {"B"      ,24   },
            {"C(+1)"     ,25    },
            {"C♯(+1)"    ,26    },
            {"D(+1)"     ,27    },
            {"E♭(+1)"    ,28    },
            {"E(+1)"     ,29    },
            {"F(+1)"     ,30    },
            {"F♯(+1)"    ,31    },
            {"G(+1)"     ,32    },
            {"G♯(+1)"    ,33    },
            {"A(+1)"     ,34    },
            {"B♭(+1)"    ,35    },
            {"B(+1)"     ,36    },
            {"C(+2)"     ,37    },

            //Without ( )
            {"C-1"     ,1     },
            {"C♯-1"    ,2     },
            {"D-1"     ,3     },
            {"E♭-1"    ,4     },
            {"E-1"     ,5     },
            {"F-1"     ,6     },
            {"F♯-1"    ,7     },
            {"G-1"     ,8     },
            {"G♯-1"    ,9     },
            {"A-1"     ,10    },
            {"B♭-1"    ,11    },
            {"B-1"     ,12    },
            {"C+1"     ,25    },
            {"C♯+1"    ,26    },
            {"D+1"     ,27    },
            {"E♭+1"    ,28    },
            {"E+1"     ,29    },
            {"F+1"     ,30    },
            {"F♯+1"    ,31    },
            {"G+1"     ,32    },
            {"G♯+1"    ,33    },
            {"A+1"     ,34    },
            {"B♭+1"    ,35    },
            {"B+1"     ,36    },
            {"C+2"     ,37    },

        };



        internal static Dictionary<char, char> RemapedCharactersDictionary = new Dictionary<char, char>()
        {
            {'♯' ,'#'} ,
            {'♭','b'},
        };

        [XmlElement("PerformNotes")]
        public List<PerformNote> PerformNotes { get; set; }


        #region done
        public override bool IsDone
        {
            get
            {
                return _isdone;
            }
        }

        private bool _isdone;
        protected override void OnResetCachedDone()
        {
            _isdone = false;

        }
        #endregion

        public async Task<bool> DoSettings()
        {

            foreach (var note in PerformNotes)
            {

                uint actionId;
                if (NoteActionMapping.TryGetValue(note.Note, out actionId))
                {
                    Log("Playing note: {0} then sleeping for {1}", note.Note, note.Delay);
                    ActionManager.DoMusic(actionId);
                    await Coroutine.Sleep(note.Delay);
                }
                else
                {
                    Logging.Write(Colors.Red, "Couldnt find note: {0}", note.Note);
                }
            }

            _isdone = true;
            return false;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r => DoSettings());
        }

        protected override void OnStart()
        {
        }

        protected override void OnDone()
        {
        }
    }
}
