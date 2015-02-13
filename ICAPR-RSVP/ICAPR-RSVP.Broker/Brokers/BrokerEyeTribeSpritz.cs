using ICAPR_RSVP.Misc;
using System;
using System.Collections.Generic;

namespace ICAPR_RSVP.Broker
{
    //<T> content message type
    public class BrokerEyeTribeRSVP<T> : Broker
    {
        private readonly int INDEX_EYE_TRIBE = 0;       //EyeTribe index in input list
        private readonly int INPUT_CLIENT = 1;          //Sprits index in input list
        private Queue<Eyes> _listCurrentEyes;           //Temporal list to store EyeData values
        private Eyes _currentEyesData;                  //Most recently EyeData read
        private DisplayItem<T> _currentWord;            //Most recently Word read
        private bool _isExpectingNewWord;       //Broker is expecting a new word
        private long _delayStartTimestamp = 0;  //Delay start timestamp. Used for delays between displayer items.

        public BrokerEyeTribeRSVP()
            : base() {/*...*/}

        protected override void Run()
        {
            //Merge input from Spritz and EyeTribe
            Item item;

            if (this._currentWord == null)
            {
                //No word has been receieved. Get a new word!
                if ((item = base._listInputPort[INPUT_CLIENT].GetItem()) != null)
                {
                    if (item.Type == ItemTypes.Config)
                        sendNewTrialConfigToOutput(item);
                    else
                        this._currentWord = (DisplayItem<T>)item.Value;
                }
            }
            else
            {
                //Get eye data if a word has been already receieved. 
                //If not null is because previous data belongs to the a new word an has not been processed, 
                //so add it to the new word.
                if (this._currentEyesData == null)
                {
                    item = base._listInputPort[INDEX_EYE_TRIBE].GetItem();
                    this._currentEyesData = (Eyes)item.Value;
                }

                //Eyes data belongs to the current word.
                if (this._currentEyesData.Timestamp >= this._currentWord.Timestamp
                    && this._currentEyesData.Timestamp < (this._currentWord.Timestamp + this._currentWord.Duration))
                {
                    //If word has just started, create delays
                    if (this._isExpectingNewWord)
                    {
                        sendToOutput(null);
                        this._isExpectingNewWord = false;
                    }
                }
                //Eye data does not belong to current word anymore. Attcach eye data to current word
                //and expect a new word.
                else if (this._currentEyesData.Timestamp >= (this._currentWord.Timestamp + this._currentWord.Duration))
                {
                    sendToOutput(_currentWord);
                    this._isExpectingNewWord = true;
                }

                //If word finished, wait for next iteration to process last eyes data
                //sendToOuptut sets it to null
                if (this._currentWord != null)
                {
                    this._listCurrentEyes.Enqueue(_currentEyesData);
                    _currentEyesData = null;
                }
            }
        }

        private void sendNewTrialConfigToOutput(Item item)
        {
            //A new config item appeared. Send to output current item to finis the trial 
            if (_currentWord != null)
            {
                sendToOutput(_currentWord);
                this._isExpectingNewWord = true;
            }

            //When new configuration is received, clean the broker
            this._listCurrentEyes = new Queue<Eyes>();
            this._currentEyesData = null;
            this._currentWord = null;
            this._isExpectingNewWord = true;
            this._delayStartTimestamp = 0;
            //Send configuraction to the core
            base.sendToOutput(item);
        }

        private void sendToOutput(DisplayItem<T> word)
        {
            //Create object to output
            DisplayItem<T> tmpWord = null;

            if (this._listCurrentEyes.Count > 0)
            {
                //Null = delay between words
                if (word == null)
                {
                    long start_timestamp = 0;
                    //Use the end of the previous word as start for delay.
                    //Do not create a delay if start_timestamp is 0 (Eye data collected before the trial started)
                    if (this._delayStartTimestamp != 0)
                    {
                        //Calculate delay duration and create delay (word value = null)
                        start_timestamp = this._delayStartTimestamp;
                        tmpWord = new DisplayItem<T>(start_timestamp, this._currentWord.Timestamp - start_timestamp, default(T));
                    }
                }
                else
                    tmpWord = word;

                //tmpWord is null when eye data was collected before the trial started.
                //In this case, do not send to the output, just clean.
                if (tmpWord != null)
                {
                    //Sent to output pipe the created item
                    DisplayItemAndEyes<T> wordAndEyes = new DisplayItemAndEyes<T>(new Queue<Eyes>(this._listCurrentEyes), tmpWord);
                    base.sendToOutput(new Bundle<DisplayItemAndEyes<T>>(ItemTypes.DisplayItemAndEyes, wordAndEyes));
                }
                this._listCurrentEyes.Clear();
            }

            //If not null (end of the word) set it to null and calculate new starting point for incoming delays
            if (word != null)
            {
                this._delayStartTimestamp = word.Timestamp + word.Duration;
                this._currentWord = null;
            }
        }
    }
}