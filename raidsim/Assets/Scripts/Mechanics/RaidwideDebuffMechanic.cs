using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.raidsim.Characters;
using dev.susybaka.raidsim.Core;
using dev.susybaka.raidsim.StatusEffects;
using dev.susybaka.raidsim.UI;
using dev.susybaka.Shared;
using static dev.susybaka.raidsim.Core.GlobalData;
using static dev.susybaka.raidsim.StatusEffects.StatusEffectData;

namespace dev.susybaka.raidsim.Mechanics
{
    public class RaidwideDebuffMechanic : FightMechanic
    {
        public PartyList party;
        public bool autoFindParty = false;
        public bool randomizeParty = true;
        public StatusEffectInfo effect;
        public bool ignoreRoles = true;
        public bool cleansEffect = false;
        public bool killsEffect = false;
        public bool incrementalTag = false;

        List<CharacterState> partyMembers;

        private void Awake()
        {
            if (party == null && autoFindParty)
                party = FightTimeline.Instance.partyList;
        }

        public override void TriggerMechanic(ActionInfo actionInfo)
        {
            if (!CanTrigger(actionInfo))
                return;

            CharacterState from = null;

            if (actionInfo.source != null)
                from = actionInfo.source;
            else if (actionInfo.target != null)
                from = actionInfo.target;

            if (party == null && autoFindParty)
                party = FightTimeline.Instance.partyList;

            if (party != null && party.members.Count > 0)
            {
                partyMembers = new List<CharacterState>(); // Copy the party members list

                /*for (int i = 0; i < party.members.Count; i++)
                {
                    partyMembers.Add(party.members[i].characterState);
                }*/

                partyMembers = party.GetActiveMembers();

                if (randomizeParty)
                    partyMembers.Shuffle();

                int currentTag = effect.tag;

                // Iterate through each status effect
                for (int i = 0; i < partyMembers.Count; i++)
                {
                    // Find a suitable party member for the effect
                    CharacterState target = partyMembers[i];

                    if (!ignoreRoles || !cleansEffect || !killsEffect)
                    {
                        target = FindSuitableTarget(effect.data, partyMembers);
                    }

                    // If no suitable target found, apply to a random member
                    if (target == null)
                        target = partyMembers[Random.Range(0, partyMembers.Count)];

                    if (killsEffect)
                    {
                        if (target.HasEffect(effect.data.statusName, currentTag))
                        {
                            target.ModifyHealth(new Damage(100, true, true, Damage.DamageType.unique, Damage.ElementalAspect.unaspected, Damage.PhysicalAspect.none, Damage.DamageApplicationType.percentageFromMax, Utilities.InsertSpaceBeforeCapitals(effect.data.statusName)), true);
                        }
                    }

                    if (!cleansEffect && !killsEffect)
                    {
                        // Apply the effect to the target
                        target.AddEffect(effect.data, from, false, currentTag, effect.stacks);
                    }
                    else if (cleansEffect && !killsEffect)
                    {
                        // Remove the effect to the target
                        target.RemoveEffect(effect.data, false, from, currentTag, effect.stacks);
                    }

                    if (incrementalTag)
                        currentTag++;

                    // Remove the effect and player from the possible options
                    partyMembers.Remove(target);
                    i--;
                }
            }
            else if (actionInfo.source != null)
            {
                if (killsEffect)
                {
                    if (actionInfo.source.HasEffect(effect.data.statusName, effect.tag))
                    {
                        actionInfo.source.ModifyHealth(new Damage(100, true, true, Damage.DamageType.unique, Damage.ElementalAspect.unaspected, Damage.PhysicalAspect.none, Damage.DamageApplicationType.percentageFromMax, Utilities.InsertSpaceBeforeCapitals(effect.data.statusName)), true);
                    }
                }

                if (!cleansEffect && !killsEffect)
                {
                    // Apply the effect to the target
                    actionInfo.source.AddEffect(effect.data, from, false, effect.tag, effect.stacks);
                }
                else if (!killsEffect)
                {
                    // Remove the effect to the target
                    actionInfo.source.RemoveEffect(effect.data, false, from, effect.tag, effect.stacks);
                }
            }
        }

        private CharacterState FindSuitableTarget(StatusEffectData effect, List<CharacterState> candidates)
        {
            foreach (Role role in effect.assignedRoles)
            {
                // Create a copy of the candidates list
                List<CharacterState> candidatesCopy = new List<CharacterState>(candidates);

                // Iterate through the copy of candidates
                for (int i = 0; i < candidatesCopy.Count; i++)
                {
                    var candidate = candidatesCopy[i];
                    if (candidate.role == role)
                    {
                        candidates.Remove(candidate); // Remove the candidate from the original list
                        return candidate; // Return the suitable candidate
                    }
                }
            }
            return null; // No suitable candidate found
        }
    }
}