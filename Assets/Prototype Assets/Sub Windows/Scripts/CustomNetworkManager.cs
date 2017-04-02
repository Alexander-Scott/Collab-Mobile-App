﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using NetworkLib;
using System;

namespace Assets.Prototype_Assets
{
    public class CustomNetworkManager : NetworkManager {

        public Button startSessionBtn;
        public Button joinSessionBtn;
        public Button switchkeypad;
        public Canvas canvas;

        //public Button subControls;

        private void Start()
        {
            startSessionBtn.onClick.AddListener(StartSessionBtnClicked);
            joinSessionBtn.onClick.AddListener(JoinSessionBtnClicked);

            if (GlobalVariables.IPRadar == true && GlobalVariables.IPRadarSwitch == false) 
            {
                GlobalVariables.IPRadarSwitch = true;
                JoinSessionBtnClicked();
            }
            else if (GlobalVariables.IPRadar == true && GlobalVariables.IPRadarSwitch == true)
            {
                canvas.enabled = false;
            }
        }

        private void StartSessionBtnClicked()
        {
            NetworkManager.singleton.StartHost();
            canvas.enabled = false;
        }

        public void JoinSessionBtnClicked()
        {
            NetworkManager.singleton.networkAddress = GlobalVariables.ipAddress;
            NetworkManager.singleton.networkPort = 7777;
            NetworkManager.singleton.StartClient();

            canvas.enabled = false;
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            NetworkServer.DestroyPlayersForConnection(conn);
            //if (conn.lastError != NetworkError.Ok)
            //{
            //    if (LogFilter.logError)
            //    {
            //        Debug.LogError("ServerDisconnected due to error: " + conn.lastError);
            //    }
            //}
        }
    }
}
