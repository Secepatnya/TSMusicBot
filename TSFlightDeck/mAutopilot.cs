﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace Razzle
{
    class mAutopilot : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /* Toggle trackers */
        public ObservableCollection<eventItem> events;
        DispatcherTimer timer;

        private string _apState;
        public string apState
        {
            get { return _apState; }
            private set { if (_apState != value) { _apState = value; NotifyPropertyChanged("apState"); } }
        }



        /* Declare Controllers */
        private mControllerPanel cs;

        /* Constructor */
        public mAutopilot(mControllerPanel targetPanel)
        {
            apState = "OFF";
            cs = targetPanel;
            events = new ObservableCollection<eventItem>();

            apRoutines();
            //mainMusicStartOnly(); // Build for Cherie's TS 
            autoStart(); // Build for CHTEA TS
        }

        public void apAddRoutine(string name, Action command, int hour, int minute, DayOfWeek dayName)
        {
            events.Add(new eventItem
            {
                name = name,
                command = command,
                startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 00),
                dayName = dayName,
                hasFired = false
            });

        }

        public void apRoutines()
        {
            apAddRoutine("Saturday Main Program PREP", preMusicStart, 15, 30, DayOfWeek.Saturday);
            apAddRoutine("Saturday Main Program START", mainProgramStart, 15, 55, DayOfWeek.Saturday);
            apAddRoutine("Saturday Main Program FINISH", mainProgramFinish, 17, 03, DayOfWeek.Saturday);

            apAddRoutine("Saturday Night Program START", nightlyReplay, 22, 00, DayOfWeek.Saturday);

            apAddRoutine("SUNDAY Main Program PREP", preMusicStart, 13, 30, DayOfWeek.Sunday);
            apAddRoutine("SUNDAY Main Program START", mainProgramStart, 13, 55, DayOfWeek.Sunday);
            apAddRoutine("SUNDAY Main Program FINISH", mainProgramFinish, 15, 03, DayOfWeek.Sunday);

            apAddRoutine("SUNDAY Night Program START", nightlyReplay, 22, 00, DayOfWeek.Sunday);

        }

        public void pollServiceStart() 
        {
            timer = new DispatcherTimer();
            timer.Tick += TimerOnTick;
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Start();
        }

        public void pollServiceStop()
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }

        void TimerOnTick(object sender, EventArgs args)
        {
            foreach (eventItem item in events)
            {
                if (item.startTime.Hour == DateTime.Now.Hour && item.startTime.Minute == DateTime.Now.Minute && item.dayName == DateTime.Now.DayOfWeek)
                {
                    if (!item.hasFired)
                    {
                        item.hasFired = true;
                        item.command();
                    }
                }
                else
                {
                    item.hasFired = false;
                }
            }
        }

        /* Individual subroutines */
        void preMusicStart()
        {
            cs.satellite.Stop();
            cs.player2.Stop();
            cs.player1.selectedPlayMode = sourcePlayer.playModes.Continous;
            cs.player1.Start(true);
            cs.player1.SetVolume(1f);
        }

        void mainProgramStart()
        {
            new Thread(() =>
            {
                cs.satellite.RecordStart();
                
                cs.satellite.Stop();
                cs.player1.Stop();

                cs.player2.Start(false);
                cs.satellite.Start();
            }).Start();
        }

        void mainProgramFinish()
        {
            cs.satellite.RecordStop();
            cs.satellite.Stop();
        }

        void nightlyReplay()
        {
            new Thread(() =>
            {
                cs.satellite.Stop();
                cs.player1.Stop();
                cs.player2.Stop();
                cs.player3.Stop();

                cs.player2.SetVolume(1f);
                cs.player2.Start(false);

                cs.player3.SetVolume(1f);
                cs.player3.Start(false);
            }).Start();
        }



        /* Master Switch */
        public void autoStart()
        {
            if (apState != "ON")
            {
                pollServiceStart();
            }
            apState = "ON";
        }

        public void autoStop()
        {
            pollServiceStop();
            apState = "OFF";
        }

    }
}
