﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NetworkLib;
using System;
using System.Collections;
using UnityEngine.Networking;

namespace Assets.Prototype_Assets
{
    public class MenuButtons : MonoBehaviour
    {
        public Button subButton;
        public Button controlsButton;
        public Button qrScannerButton;
        public Button miniSubButton;
        public Button startSceneButton;

        public Button connectButton;

        public InputField ipAddressText;
        public Dropdown dropDownList;

        public Text infoText;

        private bool awaitingResponse = false;
        private bool clientCreated = false;
        private bool updateUI = false;

        private Text connectButtonText;

        private float connectionTimer = 0f;

        private void Start()
        {
            subButton.onClick.AddListener(SubButtonClicked);
            controlsButton.onClick.AddListener(ControlsButtonClicked);
            qrScannerButton.onClick.AddListener(QRButtonClicked);
            miniSubButton.onClick.AddListener(MiniSubButtonClicked);
            startSceneButton.onClick.AddListener(StartSceneButtonClicked);

            connectButton.onClick.AddListener(ConnectButtonClicked);

            ipAddressText.text = GlobalVariables.ipAddress;
            dropDownList.value = GlobalVariables.playerNumber;

            connectButtonText = connectButton.GetComponentInChildren<Text>();

            // If we're returing to this scene from the sub controls then the player might still be registered
            if (GlobalVariables.mobilePlayerRegistered)
            {
                connectButtonText.text = "Disconnect";
                controlsButton.gameObject.GetComponent<Image>().color = Color.green;
                qrScannerButton.gameObject.GetComponent<Image>().color = Color.green;
                dropDownList.enabled = false;
                clientCreated = true;
                AddPacketObservers();
            }
            else
            {
                connectButtonText.text = "Connect";
                controlsButton.gameObject.GetComponent<Image>().color = Color.gray;
                qrScannerButton.gameObject.GetComponent<Image>().color = Color.gray;
                dropDownList.enabled = true;
            }
        }

        private void Update()
        {
            // Since the Server runs on a seperate thread we can't directly update this stuff. 
            // This is a hack to make modifcations to gameobjects/components such as text
            if (updateUI)
            {
                updateUI = false;

                if (GlobalVariables.mobilePlayerRegistered)
                {
                    connectButtonText.text = "Disconnect";
                    controlsButton.gameObject.GetComponent<Image>().color = Color.green;
                    qrScannerButton.gameObject.GetComponent<Image>().color = Color.green;
                    dropDownList.enabled = false;
                    infoText.text = "Successfully connected!";
                }
                else
                {
                    connectButtonText.text = "Connect";
                    controlsButton.gameObject.GetComponent<Image>().color = Color.gray;
                    qrScannerButton.gameObject.GetComponent<Image>().color = Color.gray;
                    dropDownList.enabled = true;
                    infoText.text = "Someone else has taken this player!";
                }
            }

            if (clientCreated)
            {
                connectionTimer += Time.deltaTime;

                // If we haven't recieved a message from the server in ~2 secs it must have been ended
                if (connectionTimer > 2f)
                {
                    // ASSUME HOST IS DEAD
                    NetworkLib.Client.stop();

                    clientCreated = false;
                    awaitingResponse = false;

                    GlobalVariables.mobilePlayerRegistered = false;
                    infoText.text = "Unable to connect to host";
                    connectButtonText.text = "Connect";
                    dropDownList.enabled = true;
                    controlsButton.gameObject.GetComponent<Image>().color = Color.gray;
                    qrScannerButton.gameObject.GetComponent<Image>().color = Color.gray;

                    connectionTimer = 0f; 
                }
            }
        }

        void OnApplicationQuit()
        {
            if (GlobalVariables.mobilePlayerRegistered)
            {
                // Deregister our player
                Packet p = new Packet((int)PacketType.PlayerUnRegister, ((GlobalVariables.Direction)GlobalVariables.playerNumber).ToString());
                p.generalData.Add(((GlobalVariables.Direction)GlobalVariables.playerNumber));
                Client.SendPacket(p);

                NetworkLib.Client.stop();
            }
        }

        private void ConnectButtonClicked()
        {
            if (!GlobalVariables.mobilePlayerRegistered)
            {
                if (!clientCreated)
                {
                    // This is the only place that the client gets created on the mobile app. It stays alive so other scenes can use it.
                    NetworkLib.Client.connect(GlobalVariables.ipAddress, LibProtocolType.UDP);
                    AddPacketObservers();

                    clientCreated = true;
                }

                awaitingResponse = true;

                // Ask the server if we can take this player number
                Packet p = new Packet((int)PacketType.PlayerTryRegister, ((GlobalVariables.Direction)GlobalVariables.playerNumber).ToString());
                p.generalData.Add(((GlobalVariables.Direction)GlobalVariables.playerNumber));
                Client.SendPacket(p);

                infoText.text = "Attempting to connect...";
            }
            else
            {
                // Tell the server we are giving up our player number
                Packet p = new Packet((int)PacketType.PlayerUnRegister, ((GlobalVariables.Direction)GlobalVariables.playerNumber).ToString());
                p.generalData.Add(((GlobalVariables.Direction)GlobalVariables.playerNumber));
                Client.SendPacket(p);

                NetworkLib.Client.stop();

                clientCreated = false;
                awaitingResponse = false;

                GlobalVariables.mobilePlayerRegistered = false;
                infoText.text = "Disconnected";
                connectButtonText.text = "Connect";
                dropDownList.enabled = true;
                controlsButton.gameObject.GetComponent<Image>().color = Color.gray;
                qrScannerButton.gameObject.GetComponent<Image>().color = Color.gray;
            }
        }

        private void AddPacketObservers()
        {
            NetworkLib.Client.ClientPacketObserver.AddObserver((int)PacketType.PlayerTryRegisterResult, PlayerTryRegisterResult);
            NetworkLib.Client.ClientPacketObserver.AddObserver((int)PacketType.UpdateAllEscapeStatesOnClients, UpdateEscapeState);
            NetworkLib.Client.ClientPacketObserver.AddObserver((int)PacketType.CheckClientAlive, CheckClientAlive);
            NetworkLib.Client.ClientPacketObserver.AddObserver((int)PacketType.UpdateSingleEscapeStateOnClients, UpdateSingleEscapeState);
        }

        private void CheckClientAlive(Packet p)
        {
            connectionTimer = 0f;
        }

        private void PlayerTryRegisterResult(Packet p)
        {
            if (awaitingResponse) // Only enter this if we are expecting this packet. It may be destined for another client.
            {
                awaitingResponse = false;
                updateUI = true;

                if (bool.Parse(p.generalData[1].ToString())) // This player number is free
                {
                    Packet pack = new Packet((int)PacketType.PlayerRegister, ((GlobalVariables.Direction)GlobalVariables.playerNumber).ToString());
                    pack.generalData.Add(((GlobalVariables.Direction)GlobalVariables.playerNumber));
                    Client.SendPacket(pack);

                    GlobalVariables.mobilePlayerRegistered = true;

                    CheckEscapeState();
                }
                else // It is already taken
                {
                    GlobalVariables.mobilePlayerRegistered = false;
                }
            }
        }

        // Asks the server if the escape has started yet
        private void CheckEscapeState()
        {
            Packet p = new Packet((int)PacketType.CheckEscapeState, PacketType.CheckEscapeState.ToString());
            Client.SendPacket(p);
        }

        private void UpdateEscapeState(Packet p)
        {
            int escapeStatesCount = Enum.GetNames(typeof(GlobalVariables.EscapeState)).Length;

            for (int i = 0; i < escapeStatesCount; i++)
            {
                GlobalVariables.UpdateProgression((GlobalVariables.EscapeState)i, bool.Parse(p.generalData[i].ToString()));
            }
        }

        private void UpdateSingleEscapeState(Packet p)
        {
            GlobalVariables.EscapeState escapeState = (GlobalVariables.EscapeState)Enum.Parse(typeof(GlobalVariables.EscapeState), p.generalData[0].ToString());
            bool progression = bool.Parse(p.generalData[1].ToString());

            GlobalVariables.UpdateProgression(escapeState, progression);
        }

        #region Button click events

        private void SubButtonClicked()
        {
            if (GlobalVariables.mobilePlayerRegistered)
            {
                Packet p = new Packet((int)PacketType.PlayerUnRegister, ((GlobalVariables.Direction)GlobalVariables.playerNumber).ToString());
                p.generalData.Add(((GlobalVariables.Direction)GlobalVariables.playerNumber));
                Client.SendPacket(p);

                //NetworkLib.Client.stop();

                GlobalVariables.mobilePlayerRegistered = false;
            }

            SceneManager.LoadScene("Test");
        }

        private void ControlsButtonClicked()
        {
            if (GlobalVariables.mobilePlayerRegistered && GlobalVariables.CheckProgression(GlobalVariables.EscapeState.EscapeStarted))
            {
                SceneManager.LoadScene("Controls");

                // If this is the first time we enter this scene update the escape state
                if (!GlobalVariables.CheckProgression(GlobalVariables.EscapeState.SubControlsEnabled))
                {
                    Packet p = new Packet((int)PacketType.UpdateSingleEscapeStateOnServer, PacketType.UpdateSingleEscapeStateOnServer.ToString());
                    p.generalData.Add(GlobalVariables.EscapeState.SubControlsEnabled);
                    p.generalData.Add(true);

                    Client.SendPacket(p);
                }
            }
            else if (GlobalVariables.mobilePlayerRegistered && !GlobalVariables.CheckProgression(GlobalVariables.EscapeState.EscapeStarted))
            {
                infoText.text = "Please start the game first!";
            }
            else
            {
                infoText.text = "Please connect first!";
            }
        }

        private void QRButtonClicked()
        {
            if (GlobalVariables.mobilePlayerRegistered && GlobalVariables.CheckProgression(GlobalVariables.EscapeState.EscapeStarted))
            {
                SceneManager.LoadScene("QRScanner");
            }
            else if (GlobalVariables.mobilePlayerRegistered && !GlobalVariables.CheckProgression(GlobalVariables.EscapeState.EscapeStarted))
            {
                infoText.text = "Please start the game first!";
            }
            else
            {
                infoText.text = "Please connect first!";
            }
        }

        private void MiniSubButtonClicked()
        {
            if (GlobalVariables.mobilePlayerRegistered)
            {
                Packet p = new Packet((int)PacketType.PlayerUnRegister, ((GlobalVariables.Direction)GlobalVariables.playerNumber).ToString());
                p.generalData.Add(((GlobalVariables.Direction)GlobalVariables.playerNumber));
                Client.SendPacket(p);

                //NetworkLib.Client.stop();

                GlobalVariables.mobilePlayerRegistered = false;

                NetworkManager.singleton.networkAddress = GlobalVariables.ipAddress;
                NetworkManager.singleton.networkPort = 7777;
                NetworkManager.singleton.StartClient();
            }

            SceneManager.LoadScene("VRTest");
        }

        private void StartSceneButtonClicked()
        {
            SceneManager.LoadScene("StartScene");
        }

        public void IPAddressChanged(string ipaddress)
        {
            GlobalVariables.ipAddress = ipaddress;

            PlayerPrefs.SetString("IPAddress", ipaddress); // Save the new ip address locally on the device
            PlayerPrefs.Save();
        }

        public void PlayerSelectChanged(int val)
        {
            GlobalVariables.playerNumber = val;
        }

        #endregion
    }
}
