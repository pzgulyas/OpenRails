﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ORTS {
    /// <summary>
    /// User may specify an automatic pause in the replay at a time measured from the end of the replay.
    /// </summary>
    public enum ReplayPauseState {
        Before,
        Due,        // Set by CommandLog.Replay(), tested by Viewer.Update()
        During,
        Done
    };

    public class CommandLog {

        public List<ICommand> CommandList = new List<ICommand>();
        public Viewer3D Viewer { get; set; }        // Needed so Update() can get Viewer.CameraReplaySuspended
        public Simulator Simulator { get; set; }    // Needed so CommandAdd() and Update() can get Simulator.ClockTime and Update() can get Simulator.Settings.ReplayPauseBeforeEndS
        public bool ReplayComplete { get; set; }
        public double ReplayEndsAt { get; set; }
        public ReplayPauseState PauseState { get; set; }
        
        private double completeTime;
        private DateTime? resumeTime;
        private const double completeDelayS = 2;

        /// <summary>
        /// Preferred constructor.
        /// </summary>
        public CommandLog( Viewer3D viewer ) {
            Viewer = viewer;
            Simulator = viewer.Simulator;  // The Simulator is needed for its ClockTime and Settings properties.
        }

        /// <summary>
        /// Use this version if the Simulator does not yet exist then.
        /// Don't forget to set the Simulator property as soon as it's known.
        /// </summary>
        public CommandLog( ) {
        }

        /// <summary>
        /// When a command is created, it adds itself to the log.
        /// </summary>
        /// <param name="Command"></param>
        public void CommandAdd( ICommand command ) {
            command.Time = Simulator.ClockTime; // Note time that command was issued
            CommandList.Add( command );
        }
        
        /// <summary>
        /// Replays any commands that have become due.
        /// Issues commands from the replayCommandList at the same time that they were originally issued.
        /// <para>
        /// Assumes replayCommandList is already sorted by time.
        /// </para>
        /// </summary>
        public void Update( List<ICommand> replayCommandList ) {
            if( Viewer == null ) return;  // Update can get called before Viewer is assigned

            double elapsedTime = Simulator.ClockTime;

            if( PauseState == ReplayPauseState.Before ) {
                if( elapsedTime > ReplayEndsAt - Simulator.Settings.ReplayPauseBeforeEndS ) {
                    PauseState = ReplayPauseState.Due;  // For Viewer.Update() to detect and pause.
                }
            }

            if( replayCommandList.Count > 0 ) {
                var c = replayCommandList[0];
                // Without a small margin, an activity event can pause simulator just before the ResumeActicityCommand is due, 
                // so resume never happens.
                double margin = (Simulator.Paused) ? 0.5 : 0;   // margin of 0.5 seconds
                if( elapsedTime >= c.Time - margin ) {
                    if( c is ActivityCommand ) {
                        // Wait for the right duration and then action the command.
                        // ActivityCommands need dedicated code as the clock is no longer advancing.
                        if( resumeTime == null ) {
                            var resumeCommand = (ActivityCommand)c;
                            resumeTime = DateTime.Now.AddSeconds(resumeCommand.PauseDurationS );
                        } else {
                            if( DateTime.Now >= resumeTime ) {
                                resumeTime = null;  // cancel trigger
                                ReplayCommand( elapsedTime, replayCommandList, c );
                            }
                        }
                    } else {
                        // When the player uses a camera command during replay, replay continues but any camera commands in the 
                        // replayCommandList are skipped until the player pauses and exit from the Quit Menu.
                        // This allows some editing of the camera during a replay.
                        if(!(( c is UseCameraCommand || c is MoveCameraCommand ) && Viewer.CameraReplaySuspended )) {
                            ReplayCommand( elapsedTime, replayCommandList, c );
                        }
                        completeTime = elapsedTime + completeDelayS;  // Postpone the time for "Replay complete" message
                    }
                }
            } else {
                if( completeTime != 0 && elapsedTime > completeTime ) {
                    completeTime = 0;       // Reset trigger so this only happens once
                    ReplayComplete = true;  // Flag seen by Viewer3D which announces "Replay complete".
                }
            }
        }

        private void ReplayCommand( double elapsedTime, List<ICommand> replayCommandList, ICommand c ) {
            c.Redo();                           // Action the command
            CommandList.Add( c );               // Add to the log of commands
            replayCommandList.RemoveAt( 0 );    // Remove it from the head of the replay list
        }

        /// <summary>
        /// Copies the command objects from the log into the file specified, first creating the file.
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveLog( string filePath ) {
            Stream stream = null;
            try {
                stream = new FileStream( filePath, FileMode.Create );
                BinaryFormatter formatter = new BinaryFormatter();
                // Re-sort based on time as tests show that some commands are deferred.
                CommandList.Sort( ( x, y ) => x.Time.CompareTo( y.Time ) );
                formatter.Serialize( stream, CommandList );
            } catch( IOException ) {
                // Do nothing but warn, ignoring errors.
                Trace.TraceWarning( "SaveLog error writing command log " + filePath );
            } finally {
                if( stream != null ) { stream.Close(); }
            }
            ReportReplayCommands( CommandList );
        }

        /// <summary>
        /// Copies the command objects from the file specified into the log, replacing the log's contents.
        /// </summary>
        /// <param name="fullFilePath"></param>
        public void LoadLog( string filePath ) {
            Stream stream = null;
            try {
                stream = new FileStream( filePath, FileMode.Open );
                BinaryFormatter formatter = new BinaryFormatter();
                CommandList = (List<ICommand>)formatter.Deserialize( stream );
            } catch( IOException ) {
                // Do nothing but warn, ignoring errors.
                Trace.TraceWarning( "LoadLog error reading command log " + filePath );
            } finally {
                if( stream != null ) { stream.Close(); }
            }
        }

        public void ReportReplayCommands( List<ICommand> list ) {
            Trace.WriteLine( "\nList of commands to replay:" );
            foreach( var c in list ) { c.Report(); }
        }
    }
}