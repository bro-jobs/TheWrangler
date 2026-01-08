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
using System.Linq;
using System.Text;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("FollowNPC")]

    public class FollowNPCTag : ProfileBehavior
    {
        #region Condition
        [XmlAttribute("Condition")]
        public string Condition { get; set; }
        public Func<bool> Conditional { get; set; }


        public void SetupConditional()
        {
            try
            {
                if (Conditional == null && !string.IsNullOrEmpty(Condition))
                {
                    Conditional = ScriptManager.GetCondition(Condition);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
                // Stop on syntax errors.
                TreeRoot.Stop();
                throw;
            }

        }
        #endregion

        private bool _done;


        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }

        [DefaultValue(1.5f)]
        [XmlAttribute("Randomness")]
        public float Randomness { get; set; }

        [DefaultValue(3)]
        [XmlAttribute("Distance")]
        public float Distance { get; set; }

        [DefaultValue(true)]
        [XmlAttribute("CheckNPC")]
        public bool CheckNPC { get; set; }

        public double GetRandomNumber(double minimum, double maximum)
        {
            return Core.Random.NextDouble() * (maximum - minimum) + minimum;
        }

        protected override void OnStart()
        {
            SetupConditional();

            if (Randomness > 0)
            {
                Distance += (float)GetRandomNumber(0, Randomness);
            }

            _npc = new FrameCachedObject<GameObject>(() => GameObjectManager.GetObjectByNPCId((uint)NpcId));
        }

        public override bool IsDone
        {
            get
            {
                if (IsQuestComplete)
                    return true;

                if (IsStepComplete)
                    return true;

                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }

                return false;
            }
        }

        private Vector3 _lastValid = Vector3.Zero;
        private Vector3 Position
        {
            get
            {
                if (_npc.Value != null)
                    _lastValid = _npc.Value.Location;
                return _lastValid;
            }
        }


        private FrameCachedObject<GameObject> _npc;

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(r=> CheckNPC && _npc.Value == null, new Action(r=> {LogError($"Npc with id {NpcId} could not be found");})),
                CommonBehaviors.MoveAndStop(ret => Position, r=> Distance, true,r => $"Following {_npc.Value.Name}")
                );
        }

        protected override void OnDone()
        {
            // Force a stop!
            Navigator.PlayerMover.MoveStop();
        }
    }
}
