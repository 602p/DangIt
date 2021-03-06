﻿using CrewFilesInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ippo
{
    /// <summary>
    /// Perks management window: shows the available kerbals,
    /// their perks, and allows the user to train a kerbal to increase
    /// their skill level.
    /// </summary>
    public class RosterWindow
    {
        delegate bool RosterFilter(ProtoCrewMember k);

        const int togglesWidth = 120;
        const int buttonsWidth = 200;
        const int windowWidth = togglesWidth + 3 * buttonsWidth;
        Rect rosterRect = new Rect(300, 100, windowWidth, 250);

        // State of the scroll that lists kerbals
        int kerbalSelectionIdx = 0;
        Vector2 kerbalScrollPos = new Vector2(0, 0);

        // State of the scroll that lists the perks of a kerbal
        int perkSelectionIdx = 0;
        Vector2 perksScrollPos = new Vector2(0, 0);

        // Filters that control what kerbals are shown in the list
        KerbalFilter activeFilters;
        KerbalFilter previousFilters;


        public bool Enabled { get; set; }


        public RosterWindow()
        {
            // Initialize the default filters
            activeFilters.Crew = HighLogic.LoadedSceneIsFlight;
            activeFilters.Assigned = !HighLogic.LoadedSceneIsFlight;
            activeFilters.Available = !HighLogic.LoadedSceneIsFlight;
            activeFilters.Applicants = false;

            previousFilters = activeFilters;
        }


        public void Draw()
        {
            rosterRect = GUILayout.Window("DangItRoster".GetHashCode(),
                                          this.rosterRect,
                                          this.WindowFcn,
                                          "Dang It! Crew management",
                                          GUILayout.ExpandHeight(true),
                                          GUILayout.ExpandWidth(true)); 
        }



        public void WindowFcn(int windowID)
        {        
            GUILayout.BeginHorizontal();

            // Each of these methods creates its own GUI components
            RosterFilter filter = CreateFilter();
            ProtoCrewMember kerbal = SelectKerbal(filter);

            if (kerbal != null)
                ListAndUpgradePerks(kerbal);

            GUILayout.EndHorizontal();
            
            GUI.DragWindow();
        }


        /// <summary>
        /// Draws the toggles that control what kerbals will be listed.
        /// Returns a function that selects the appropriate kerbals from the list of all kerbals.
        /// </summary>
        private RosterFilter CreateFilter()
        {
            GUILayout.BeginVertical(GUILayout.MaxWidth(togglesWidth));

            // Save state
            previousFilters = activeFilters;

            // crew is not available when not in flight (the toggle isn't drawn at all)
            activeFilters.Crew = (HighLogic.LoadedSceneIsFlight) ? GUILayout.Toggle(activeFilters.Crew, "Crew") : false;

            activeFilters.Assigned = GUILayout.Toggle(activeFilters.Assigned, "Assigned");
            activeFilters.Available = GUILayout.Toggle(activeFilters.Available, "Available");
            activeFilters.Applicants = GUILayout.Toggle(activeFilters.Applicants, "Applicants");

            GUILayout.EndVertical();

            // Create the closure
            return k =>
                (activeFilters.Crew && (HighLogic.LoadedSceneIsFlight) ? FlightGlobals.ActiveVessel.GetVesselCrew().Contains(k) : false)
             || (activeFilters.Assigned && k.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
             || (activeFilters.Available && HighLogic.CurrentGame.CrewRoster.Crew.Contains(k) && k.rosterStatus == ProtoCrewMember.RosterStatus.Available)
             || (activeFilters.Applicants && HighLogic.CurrentGame.CrewRoster.Applicants.Contains(k));
        }


        /// <summary>
        /// Draws the list of kerbals and returns the one that has been selected by the user.
        /// </summary>
        private ProtoCrewMember SelectKerbal(RosterFilter filter)
        {
            // Filter the roster using the filter selected by the user
            var allKerbals = HighLogic.CurrentGame.CrewRoster.Applicants.Concat(
                             HighLogic.CurrentGame.CrewRoster.Crew);
            var selectedKerbals = allKerbals.Where(k => filter(k));

            // The user has changed the toggles: reset the index to 0
            if (activeFilters != previousFilters)
                kerbalSelectionIdx = 0;

            if (selectedKerbals.Count() > 0)
            {
                kerbalScrollPos = GUILayout.BeginScrollView(kerbalScrollPos, HighLogic.Skin.scrollView, GUILayout.MaxWidth(buttonsWidth));

                if (kerbalSelectionIdx > selectedKerbals.Count()) kerbalSelectionIdx = 0;
                kerbalSelectionIdx = GUILayout.SelectionGrid(kerbalSelectionIdx,
                                                             selectedKerbals.Select(k => k.name).ToArray(),
                                                             xCount: 1);
                GUILayout.EndScrollView();

                return selectedKerbals.ElementAt(kerbalSelectionIdx);
            }
            else
            {
                GUILayout.Label("No kerbal matches your search", GUILayout.ExpandHeight(true), GUILayout.ExpandHeight(true));
                return null;
            }
        }


        /// <summary>
        /// Draws the list of perks and shows the button to upgrade them.
        /// </summary>
        private void ListAndUpgradePerks(ProtoCrewMember kerbal)
        {
            try 
	        {
                // Fetch the perks from crewfiles
                List<Perk> perks = kerbal.GetPerks();

                perksScrollPos = GUILayout.BeginScrollView(perksScrollPos, HighLogic.Skin.scrollView, GUILayout.MaxWidth(buttonsWidth));

                if (perkSelectionIdx > perks.Count) perkSelectionIdx = 0;
                perkSelectionIdx = GUILayout.SelectionGrid(perkSelectionIdx,
                                                           perks.Select(p => p.Specialty.ToString()).ToArray(),
                                                           xCount: 1);
                GUILayout.EndScrollView();

                // Make the upgrade button
                UpgradePerkButton(kerbal, perks, perkSelectionIdx);

	        }
	        catch (ServerNotInstalledException)
	        {
                GUILayout.Label("CrewFiles is not installed!");
                return;
	        }
            catch (ServerUnavailableException)
            {
                GUILayout.Label("Something is wrong with CrewFiles");
                return;
            }
            catch (Exception e)
            {
                GUILayout.Label("An exception occurred: " + e.Message + e.StackTrace);
                return;
            }
        }


        /// <summary>
        /// Draws the current level of the selected perk and shows a button to upgrade it, if applicable.
        /// </summary>
        private void UpgradePerkButton(ProtoCrewMember kerbal, List<Perk> perks, int idx)
        {
            GUILayout.BeginVertical(GUILayout.MaxWidth(buttonsWidth));

            // First, show a label (styled like a button) with the current level
            GUILayout.Label("Current:\n" + perks[idx].SkillLevel.ToString(),
                            HighLogic.Skin.button);


            // If the kerbal is not available to be trained, draw a greyed out button and return
            if ((HighLogic.CurrentGame.CrewRoster.Applicants.Contains(kerbal)            // applicants
                || kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Assigned))        // crew in flight
            {
                GUI.enabled = false;
                GUILayout.Button(kerbal.name + "\ncannot be trained\nright now.");
                GUI.enabled = true;
            }
            else // the kerbal can be trained: get the upgrade cost and display the button
            {
                SkillLevel nextLevel = GetNextLevel(perks[idx].SkillLevel);

                if (nextLevel == perks[idx].SkillLevel) // max level reached
                {
                    GUILayout.Label("Max level", HighLogic.Skin.button);
                }
                else // Create upgrade button
                {
                    TrainingCost cost = DangIt.Instance.trainingCosts.Where(t => t.Level == nextLevel).Single();

                    string btnLabel = "Upgrade to " + nextLevel.ToString() + "\n" +
                                      "Funds: " + cost.Funds + "\n" +
                                      "Science: " + cost.Science;

                    if (GUILayout.Button(btnLabel))
                    {
                        Debug.Log("Requested upgrade to " + nextLevel.ToString());

                        if (CheckOutAndSpendResources(cost))
                        {
                            perks[idx].SkillLevel++;
                            kerbal.SetPerks(perks);
                        }
                        else
                        {
                            DangIt.Broadcast("You cannot afford this training!", true);
                        }
                    }
                }
            }

            GUILayout.EndVertical();
        }


        /// <summary>
        /// Checks if the player can affor the training cost.
        /// If he can, spend the resources.
        /// </summary>
        private static bool CheckOutAndSpendResources(TrainingCost cost)
        {
            switch (HighLogic.CurrentGame.Mode)
            {
                case Game.Modes.CAREER:

                    if (Funding.Instance.Funds < cost.Funds) return false;
                    if (ResearchAndDevelopment.Instance.Science < cost.Science) return false;

                    Funding.Instance.AddFunds(-cost.Funds, TransactionReasons.RnDs);
                    ResearchAndDevelopment.Instance.AddScience(-cost.Science, TransactionReasons.RnDs);

                    return true;


                case Game.Modes.SCIENCE_SANDBOX:

                    if (ResearchAndDevelopment.Instance.Science < cost.Science) return false;
                    ResearchAndDevelopment.Instance.AddScience(-cost.Science, TransactionReasons.RnDs);

                    return true;


                case Game.Modes.SANDBOX:
                    return true;


                default:
                    return true;
            }
        }


        /// <summary>
        /// Finds the next Skill Level after the current one.
        /// If the current level is the maximum, the result will be equal to the argument.
        /// </summary>
        private static SkillLevel GetNextLevel(SkillLevel current)
        {
            int maxLevel = Enum.GetValues(typeof(SkillLevel)).Cast<int>().Max();

            // Sum 1, clamp to maxLevel, cast to SkillLevel
            SkillLevel nextLevel = (SkillLevel)(Math.Min((int)current + 1, maxLevel));

            return nextLevel;
        }
    }


    struct KerbalFilter : IEquatable<KerbalFilter>
    {
        public bool Crew;
        public bool Assigned;
        public bool Available;
        public bool Applicants;

        #region Standard overrides

        public bool Equals(KerbalFilter other)
        {
            return this.Crew == other.Crew &&
                   this.Assigned == other.Assigned &&
                   this.Available == other.Available &&
                   this.Applicants == other.Applicants;
        }

        public override int GetHashCode() { return base.GetHashCode(); }

        public override string ToString()
        {
            return String.Format("(Crew: {0}, Assigned: {1}, Hired: {2}, Applicants: {3})", Crew, Assigned, Available, Applicants);
        }

        public override bool Equals(object obj)
        {
            if (obj is KerbalFilter) { return Equals((KerbalFilter)obj); }
            return false;
        }

        public static bool operator ==(KerbalFilter a, KerbalFilter b) { return a.Equals(b); }
        public static bool operator !=(KerbalFilter a, KerbalFilter b) { return !(a == b); }

        #endregion
    }

}
