﻿using System;
using Fleck;
using ICAPR_RSVP.Misc;
using Newtonsoft.Json;

namespace ICAPR_RSVP.Broker
{
    public class Network
    {
        private Port brokerPort;               //Broker input port
        private volatile bool _isConnected;    //Is server running?
        private int _port;                     //Server port
        private String _host;                  //Host address
        private String _path;                  //URL path
        private String _fullServerUrl;         //Full server URL 
        private WebSocketServer _serverSocket; //Server web socket

        public Network(Port brokerPort, String host, String path, int port)
        {
            this.brokerPort = brokerPort;
            this._port = port;
            this._host = host;
            this._path = path;
            this._isConnected = false;

            this._fullServerUrl = getFullServerUrl(this._host, this._path, this._port);

            System.Console.WriteLine("Network will open server at : " + this._fullServerUrl);
        }

        private String getFullServerUrl(String host, String path, int port)
        {
            if (host.EndsWith("/"))
                host = host.Remove(host.Length - 1);

            if (path.EndsWith("/"))
                path = path.Remove(path.Length - 1);

            if (path.StartsWith("/"))
                path = path.Substring(1, path.Length - 1);

            return "ws://" + host + ":" + this._port + "/" + path;
        }

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
        }

        public void startNetwork()
        {
            stopNetwork();
            System.Console.WriteLine("Starting network...");
            setUpNetwork();
        }

        public void stopNetwork()
        {
            System.Console.WriteLine("Stopping network...");
            if (this._isConnected)
                if (_serverSocket != null)
                    _serverSocket.Dispose();

            this._isConnected = false;
        }

        private void setUpNetwork()
        {
            _serverSocket = new WebSocketServer(this._fullServerUrl);

            _serverSocket.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Client connected : " + socket.ConnectionInfo.ClientIpAddress);
                    this._isConnected = true;
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Client disconnected : " + socket.ConnectionInfo.ClientIpAddress);
                    this._isConnected = false;
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine("Client sent : " + message);
                    //TODO remove the echo at some point
                    socket.Send(message);
                    Word<String> word = JsonConvert.DeserializeObject<Word<String>>(message);
                    Console.WriteLine(word.Value + "  " + word.Timestamp + "  " + word.Duration);
                    Bundle<Word<String>> bundle = new Bundle<Word<String>>(ItemTypes.Word, word);
                    this.brokerPort.PushItem(bundle);
                };
            });
        }
    }
}