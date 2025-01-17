﻿using ICA_SVP.Misc;
using ICA_SVP.Test.MockupImplementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ICA_SVP.Test
{
    [TestClass]
    public class TestBrokerEyeTribeSVP
    {
        private Port inputPortEyeTribe;
        private Port inputPortSVP;
        private Port outputPort;
        private Misc.Executors.ExecutorSingleThread broker;

        [TestInitialize]
        public void Initialize()
        {
            //Create inputs
            inputPortEyeTribe = new TestPortBlockingEyeTribe(false);
            inputPortSVP = new TestPortNonBlockingSVP();
            //Create Outputs
            outputPort = new Misc.PortBlockingDefaultImpl();

            //Start ports
            inputPortEyeTribe.Start();
            inputPortSVP.Start();
            outputPort.Start();

            //Create Broker
            broker = new Misc.Executors.BrokerEyeTribeSVP<String>();
            broker.AddInput(inputPortEyeTribe);
            broker.AddInput(inputPortSVP);
            broker.AddOutput(outputPort);
            broker.Start();
        }

        [TestMethod]
        public void TestBrokerDataMerging()
        {
            //Test input merging
            Item item;
            long wordTimestamp = 0;
            long wordDuration = 0;
            long oldTimestamp = 0;

            for(int i = 0;i < TestPortNonBlockingSVP.WORD_COUNT * TestPortNonBlockingSVP.NUMBER_TRIALS;i++)
            {
                item = this.outputPort.GetItem();
                if(item.Type == ItemTypes.DisplayItemAndEyes)
                {
                    DisplayItemAndEyes<String> wordAndEyes = (DisplayItemAndEyes<String>)item.Value;
                    Queue<Eyes> listEyes = wordAndEyes.Eyes;
                    DisplayItem<String> word = wordAndEyes.DisplayItem;

                    wordTimestamp = word.Timestamp;
                    wordDuration = word.Duration;
                    Assert.IsTrue(wordTimestamp >= oldTimestamp,"Word timestamps not ordered");
                    oldTimestamp = wordTimestamp;

                    foreach(Eyes eyes in listEyes)
                    {
                        //Assert ordered timestamps
                        Assert.IsTrue(eyes.Timestamp >= oldTimestamp,
                            "Eyes timestamp not greater or equal than previous " + eyes.Timestamp + " " + oldTimestamp);
                        Assert.IsTrue(eyes.Timestamp <= (oldTimestamp + wordDuration - 1),
                            "Eyes timestamp greater than word timing");
                        oldTimestamp = eyes.Timestamp;
                    }
                }
                else
                {
                    Assert.IsTrue(item.Type == ItemTypes.Config || item.Type == ItemTypes.EndOfTrial);
                    i--;
                }
            }
            broker.Stop();
        }
    }
}
