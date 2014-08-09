﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using KSP.IO;
using System.Text;

namespace ippo
{
    public partial class DangIt : ScenarioModule
    {
        public List<string> LeakBlackList;
        public DangSettings Settings;

        public static DangIt Instance { get; private set; }
        public bool IsReady { get; private set; }        

        public string SettingsFilePath
        {
            get { return IOUtils.GetFilePathFor(this.GetType(), "DangIt.cfg"); }
        }


        public DangIt()
        {      
            // Load the resource blacklist from the file
            LeakBlackList = new List<string>();
            ConfigNode blackListNode = ConfigNode.Load(SettingsFilePath).GetNode("BLACKLIST");
            foreach (string item in blackListNode.GetValues("ignore"))
                LeakBlackList.Add(item);

            Instance = this;
            this.IsReady = false;
        }


        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("SETTINGS"))
                this.Settings = new DangSettings(node.GetNode("SETTINGS"));
            else
            {
                this.Settings = new DangSettings();
                Debug.Log("[DangIt] WARNING: No settings node to load, creating default one");
            }

            this.IsReady = true;
        }


        public override void OnSave(ConfigNode node)
        {
            Debug.Log("[DangIt] Saving settings...");
            node.AddNode(Settings.ToNode());
        }

    }
}