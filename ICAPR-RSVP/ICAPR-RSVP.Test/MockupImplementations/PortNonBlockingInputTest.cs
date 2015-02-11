﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICAPR_RSVP.Broker;
using ICAPR_RSVP.Misc;
using System.Threading;

namespace ICAPR_RSVP.Test.MockupImplementations
{
    public class PortNonBlockingInputTest : PortNonBlocking
    {
        public static readonly int NUMBER_TRIALS = 1;
        public static readonly int WORD_COUNT = 20;
        private Thread _workerThread;
        private bool _isRunning;

        #region Properties
        public override bool IsRunning { get { return _isRunning; } }
        #endregion

        public PortNonBlockingInputTest()
            : base()
        {
            _workerThread = new Thread(DoWork);
        }

        #region Protected methods
        protected override void OnStart()
        {
            _workerThread.Start();
            this._isRunning = true;
        }

        protected override void OnStop()
        {
            _workerThread.Join();
            this._isRunning = false;
        }
        #endregion

        private void DoWork()
        {
            //Main testing method
            Random rnd = new Random();
            Misc.DisplayItem<String> word;
            int timestamp = 0;

            for (int j = 0; j < NUMBER_TRIALS; j++)
            {
                ExperimentConfig trial = new ExperimentConfig();
                trial.Trial = "trial " + j;
                trial.UserName = "name";
                trial.UserAge = "age";
                trial.FileName = "file_name";
                trial.ItemTime = "item_time";
                trial.DelayTime = "delay_time";
                trial.FontSize = "font_size";
                trial.FontColor = "font_color";
                trial.AppBackground = "app_bg";
                trial.BoxBackground = "box_bg";
                trial.SaveLog = true;

                base.PushItem(new Bundle<ExperimentConfig>(ItemTypes.Config, trial));

                for (int i = 0; i < WORD_COUNT; i++)
                {
                    timestamp += rnd.Next(1000, 2000);
                    word = new DisplayItem<String>(timestamp, 1000, "test");
                    base.PushItem(new Bundle<DisplayItem<String>>(ItemTypes.DisplayItem, word));
                }
            }
        }
    }
}
