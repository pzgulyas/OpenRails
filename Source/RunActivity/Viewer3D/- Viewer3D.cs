﻿// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

/* 3D Viewer

    /// This a 3D viewer.  It connects to a simulator engine, rendering the route content and
    /// rolling stock.
    /// 
    /// When the 3D viewer is constructed its passed a reference to the simulator engine, and a flag
    /// indicating if it should operate in fullscreen mode or windowed mode.   After construction, 
    /// LookAt attaches the viewer a TrainCar in the simulator.
    /// 
 *  
 *  The Viewer class actually represents the screen window on which the camera is rendered.
 * 
 * TODO, add note re abandoning Viewer.Components
 *      - control over render order - ie sorting by material to minimize state changes
 *      - multitasking issues
 *      - multipass techniques, such as shadow mapping
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;
using ORTS.MultiPlayer;
namespace ORTS
{
    public class Viewer3D
    {
        // User setups.
        public readonly UserSettings Settings;
        // Multi-threaded processes
        public LoaderProcess LoaderProcess;
        public UpdaterProcess UpdaterProcess;
        public RenderProcess RenderProcess;
        public SoundProcess SoundProcess;
        // Access to the XNA Game class
        public GraphicsDeviceManager GDM;
        public GraphicsDevice GraphicsDevice;
        public SharedTextureManager TextureManager;
        public SharedMaterialManager MaterialManager;
        public SharedShapeManager ShapeManager;
        public Point DisplaySize;
        // Components
        public readonly Simulator Simulator;
        public World World;
        /// <summary>
        /// Monotonically increasing time value (in seconds) for the game/viewer. Starts at 0 and only ever increases, at real-time.
        /// </summary>
        public double RealTime = 0;
        InfoDisplay InfoDisplay;
        public WindowManager WindowManager = null;
        public MessagesWindow MessagesWindow; // Game message window (special, always visible)
        public PauseWindow PauseWindow; // Game paused window (special)
        public HelpWindow HelpWindow; // F1 window
        public TrackMonitorWindow TrackMonitorWindow; // F4 window
        public HUDWindow HUDWindow; // F5 hud
        public SwitchWindow SwitchWindow; // F8 window
        public TrainOperationsWindow TrainOperationsWindow; // F9 window
        public NextStationWindow NextStationWindow; // F10 window
        public CompassWindow CompassWindow; // 0 window
        public ActivityWindow ActivityWindow; // pop-up window
		public TracksDebugWindow TracksDebugWindow; // Control-Alt-F6
        public SignallingDebugWindow SignallingDebugWindow; // Control-Alt-F11 window
        // Route Information
        public TileManager Tiles;
        public ENVFile ENVFile;
        public SIGCFGFile SIGCFG;
        public TTypeDatFile TTypeDatFile;
        public bool MilepostUnitsMetric;
        // Cameras
        public Camera Camera; // Current camera
        Camera AboveGroundCamera; // Previous camera for when automatically switching to cab.
        private CabCamera CabCamera; // Camera 1
        private HeadOutCamera HeadOutForwardCamera; // Camera 1+Up
        private HeadOutCamera HeadOutBackCamera; // Camera 2+Down
		private TrackingCamera FrontCamera; // Camera 2
		private TrackingCamera BackCamera; // Camera 3
        private TracksideCamera TracksideCamera; // Camera 4
        private PassengerCamera PassengerCamera; // Camera 5
        private BrakemanCamera BrakemanCamera; // Camera 6
        private List<Camera> WellKnownCameras; // Providing Camera save functionality by GeorgeS
        private int PlayerTrainLength = 0; // re-activate cameras when this changes
        public TrainCarViewer PlayerLocomotiveViewer = null;  // we are controlling this loco, or null if we aren't controlling any
        private MouseState originalMouseState;      // Current mouse coordinates.

        // This is the train we are controlling
        public TrainCar PlayerLocomotive { get { return Simulator.PlayerLocomotive; } set { Simulator.PlayerLocomotive = value; } }
        public Train PlayerTrain { get { if (PlayerLocomotive == null) return null; else return PlayerLocomotive.Train; } }

		private Train selectedTrain = null; // the train currently cameras focus on
		public Train SelectedTrain { 
			get { if (selectedTrain == null || selectedTrain.Cars == null || selectedTrain.Cars.Count == 0) { selectedTrain = PlayerTrain;} return selectedTrain;  } 
			set { selectedTrain = value; } }

        // Mouse visibility by timer - GeorgeS
        private bool isMouseShouldVisible = false;
        private bool isMouseTimerVisible = false;
        private double MouseShownAtRealTime = 0;

        public bool SaveActivityThumbnail;
        public string SaveActivityFileStem;
        private BinaryReader inf;   // (In File) = Null indicates not resuming from a save.

		public bool DebugViewerEnabled = false;
        /// <summary>
        /// Construct a viewer.  At this time background processes are not running
        /// and the graphics device is not ready to accept content.
        /// </summary>
        /// <param name="simulator"></param>
        [CallOnThread("Render")]
        public Viewer3D(Simulator simulator)
        {
            Simulator = simulator;
            Settings = simulator.Settings;

            WellKnownCameras = new List<Camera>();
            WellKnownCameras.Add(CabCamera = new CabCamera(this));
			WellKnownCameras.Add(FrontCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Front));
			WellKnownCameras.Add(BackCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Rear));
            WellKnownCameras.Add(PassengerCamera = new PassengerCamera(this));
            WellKnownCameras.Add(BrakemanCamera = new BrakemanCamera(this));
            WellKnownCameras.Add(HeadOutForwardCamera = new HeadOutCamera(this, HeadOutCamera.HeadDirection.Forward));
            WellKnownCameras.Add(HeadOutBackCamera = new HeadOutCamera(this, HeadOutCamera.HeadDirection.Backward));
            WellKnownCameras.Add(TracksideCamera = new TracksideCamera(this));

            SharedMaterialManager.ViewingDistance = Settings.ViewingDistance = (int)Math.Min(Simulator.TRK.ORTRKData.MaxViewingDistance, Settings.ViewingDistance);

            Trace.Write(" ENV");
            ENVFile = new ENVFile(Simulator.RoutePath + @"\ENVFILES\" + Simulator.TRK.Tr_RouteFile.Environment.ENVFileName(Simulator.Season, Simulator.Weather));

            Trace.Write(" SIGCFG");
            SIGCFG = new SIGCFGFile(Simulator.RoutePath + @"\sigcfg.dat");

            Trace.Write(" TTYPE");
            TTypeDatFile = new TTypeDatFile(Simulator.RoutePath + @"\TTYPE.DAT");

            Tiles = new TileManager(Simulator.RoutePath + @"\TILES\");
            MilepostUnitsMetric = Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric;
        }

        public void Save(BinaryWriter outf, string fileStem)
        {
            outf.Write(Simulator.Trains.IndexOf(PlayerTrain));
            outf.Write(PlayerTrain.Cars.IndexOf(PlayerLocomotive));

            WindowManager.Save(outf);

            outf.Write(WellKnownCameras.IndexOf(Camera));
            foreach (var camera in WellKnownCameras)
                camera.Save(outf);
            Camera.Save(outf);

            // Set these so RenderFrame can use them when its thread gets control.
            SaveActivityFileStem = fileStem;
            SaveActivityThumbnail = true;
        }

        public void Restore(BinaryReader inf)
        {
            Train playerTrain = Simulator.Trains[inf.ReadInt32()];
            PlayerLocomotive = playerTrain.Cars[inf.ReadInt32()];

            WindowManager.Restore(inf);

            var cameraToRestore = inf.ReadInt32();
            foreach (var camera in WellKnownCameras)
                camera.Restore(inf);
            if (cameraToRestore == -1)
                new FreeRoamCamera(this, Camera).Activate();
            else
                WellKnownCameras[cameraToRestore].Activate();
            Camera.Restore(inf);
        }

        [ThreadName( "Render" )]
        public void Run( BinaryReader inf )
        {
            this.inf = inf;
            LoaderProcess = new LoaderProcess( this );
            UpdaterProcess = new UpdaterProcess( this );
            RenderProcess = new RenderProcess( this );
            RenderProcess.Run();
        }

        /// <summary>
        /// Called once after the graphics device is ready
        /// to load any static graphics content, background 
        /// processes haven't started yet.
        /// </summary>
        [CallOnThread("Render")]
        internal void Initialize()
        {
            GraphicsDevice = RenderProcess.GraphicsDevice;
            DisplaySize.X = GraphicsDevice.Viewport.Width;
            DisplaySize.Y = GraphicsDevice.Viewport.Height;
            if (Settings.ShaderModel == 0)
                Settings.ShaderModel = GraphicsDevice.GraphicsDeviceCapabilities.PixelShaderVersion.Major;
            else if (Settings.ShaderModel < 2)
                Settings.ShaderModel = 2;
            else if (Settings.ShaderModel > 3)
                Settings.ShaderModel = 3;
            if (Settings.ShadowMapDistance == 0)
                Settings.ShadowMapDistance = Settings.ViewingDistance / 2;

            PlayerLocomotive = Simulator.InitialPlayerLocomotive();

            TextureManager = new SharedTextureManager(GraphicsDevice);
            MaterialManager = new SharedMaterialManager(this);
            ShapeManager = new SharedShapeManager(this);
            UserInput.Initialize();
            InfoDisplay = new InfoDisplay(this);

            WindowManager = new WindowManager(this);
            MessagesWindow = new MessagesWindow(WindowManager);
            PauseWindow = new PauseWindow(WindowManager);
            HelpWindow = new HelpWindow(WindowManager);
            TrackMonitorWindow = new TrackMonitorWindow(WindowManager);
            HUDWindow = new HUDWindow(WindowManager);
            SwitchWindow = new SwitchWindow(WindowManager);
            TrainOperationsWindow = new TrainOperationsWindow(WindowManager);
            NextStationWindow = new NextStationWindow(WindowManager);
            CompassWindow = new CompassWindow(WindowManager);
            ActivityWindow = new ActivityWindow(WindowManager);
            TracksDebugWindow = new TracksDebugWindow(WindowManager);
            SignallingDebugWindow = new SignallingDebugWindow(WindowManager);
            WindowManager.Initialize();

            World = new World(this);

            SoundProcess = new SoundProcess(this);
            Simulator.Confirmer = new Confirmer( Settings.SuppressConfirmations, World, 1.5 );

            // Now everything is ready to use, changed to saved values if available. 
            if (inf != null)
                Restore(inf);

            if (Camera == null)
                FrontCamera.Activate();
            else
                Camera.Activate();

            // Prepare the world to be loaded and then load it from the correct thread for debugging/tracing purposes.
            // This ensures that a) we have all the required objects loaded when the 3D view first appears and b) that
            // all loading is porformed on a single thread that we can handle in debugging and tracing.
            World.LoadPrep();
            LoaderProcess.StartLoad();
            LoaderProcess.WaitTillFinished();

            // MUST be after loading is done! (Or we try and load shapes on the main thread.)
            PlayerLocomotiveViewer = World.Trains.GetViewer(PlayerLocomotive);

            if (Settings.FullScreen)
                RenderProcess.ToggleFullScreen();
        }

        string adapterDescription;
        public string AdapterDescription { get { return adapterDescription; } }

        uint adapterMemory = 0;
        public uint AdapterMemory { get { return adapterMemory; } }

        [CallOnThread("Updater")]
        internal void UpdateAdapterInformation(GraphicsAdapter graphicsAdapter)
        {
            adapterDescription = graphicsAdapter.Description;
            try
            {
                // Note that we might find multiple adapters with the same
                // description; however, the chance of such adapters not having
                // the same amount of video memory is very slim.
                foreach (ManagementObject videoController in new ManagementClass("Win32_VideoController").GetInstances())
                    if (((string)videoController["Description"] == adapterDescription) && (videoController["AdapterRAM"] != null))
                        adapterMemory = (uint)videoController["AdapterRAM"];
            }
            catch (ManagementException error)
            {
                Trace.WriteLine(error);
            }
        }

        internal void ProcessReportError(Exception error)
        {
            // Log the error first in case we're burning.
            Trace.WriteLine(error);
            // Stop the world!
            RenderProcess.Stop();
            // Show the user that it's all gone horribly wrong.
            if (Settings.ShowErrorDialogs)
                System.Windows.Forms.MessageBox.Show(error.ToString());
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            World.Load();
        }

        [CallOnThread("Updater")]
        public void Update(float elapsedRealTime, RenderFrame frame)
        {
            RealTime += elapsedRealTime;
            var elapsedTime = new ElapsedTime(Simulator.GetElapsedClockSeconds(elapsedRealTime), elapsedRealTime);

            Simulator.Update(elapsedTime.ClockSeconds);
            HandleUserInput(elapsedTime);
            UserInput.Handled();

            // Mute sound when paused
            if (Simulator.Paused)
                ALSoundSource.MuteAll();
            else
                ALSoundSource.UnMuteAll();

            if (ScreenHasChanged())
            {
                Camera.ScreenChanged();
                RenderProcess.InitializeShadowMapLocations(RenderProcess.Viewer);
            }

            // Update camera first...
            Camera.Update(elapsedTime);

            // No above camera means we're allowed to auto-switch to cab view.
            if ((AboveGroundCamera == null) && Camera.IsUnderground)
            {
                AboveGroundCamera = Camera;
                CabCamera.Activate();
            }
			else if (AboveGroundCamera != null && Camera.AttachedCar.Train == Simulator.PlayerLocomotive.Train)
			{
				// Make sure to keep the old camera updated...
				AboveGroundCamera.Update(elapsedTime);
				// ...so we can tell when to come back to it.
				if (!AboveGroundCamera.IsUnderground)
				{
					// But only if the user hasn't selected another camera!
					if (Camera == CabCamera)
						AboveGroundCamera.Activate();
					AboveGroundCamera = null;
				}
			}

            World.Update(elapsedTime);

            // Every 250ms, check for new things to load and kick off the loader.
            if (LastLoadRealTime + 0.25 < RealTime && LoaderProcess.Finished)
            {
                LastLoadRealTime = RealTime;
                World.LoadPrep();
                LoaderProcess.StartLoad();
            }

            Camera.PrepareFrame(frame, elapsedTime);
            frame.PrepareFrame(elapsedTime);
            World.PrepareFrame(frame, elapsedTime);
            InfoDisplay.PrepareFrame(frame, elapsedTime);
            // TODO: This is not correct. The ActivityWindow's PrepareFrame is already called by the WindowManager!
            if (Simulator.ActivityRun != null) ActivityWindow.PrepareFrame(elapsedTime, true);

            WindowManager.PrepareFrame(frame, elapsedTime);
        }
        double LastLoadRealTime = 0;

        [CallOnThread("Updater")]
        void HandleUserInput(ElapsedTime elapsedTime)
        {
            Camera.HandleUserInput(elapsedTime);

            if (PlayerLocomotiveViewer != null)
                PlayerLocomotiveViewer.HandleUserInput(elapsedTime);

            InfoDisplay.HandleUserInput(elapsedTime);
            WindowManager.HandleUserInput(elapsedTime);

            // Check for game control keys
            if (UserInput.IsPressed(UserCommands.GameQuit)) { Stop(); return; }
            if (UserInput.IsPressed(UserCommands.GameFullscreen)) { RenderProcess.ToggleFullScreen(); }
            if (UserInput.IsPressed(UserCommands.GamePause)) Simulator.Paused = !Simulator.Paused;
            if( UserInput.IsPressed( UserCommands.DebugSpeedUp ) ) {
                Simulator.GameSpeed *= 1.5f;
                Simulator.Confirmer.ConfirmWithPerCent( CabControl.SimulationSpeed, CabSetting.Increase, Simulator.GameSpeed * 100 );
            }
            if( UserInput.IsPressed( UserCommands.DebugSpeedDown ) ) {
                Simulator.GameSpeed /= 1.5f;
                Simulator.Confirmer.ConfirmWithPerCent( CabControl.SimulationSpeed, CabSetting.Decrease, Simulator.GameSpeed * 100 );
            }
            if( UserInput.IsPressed( UserCommands.DebugSpeedReset ) ) {
                Simulator.GameSpeed = 1;
                Simulator.Confirmer.ConfirmWithPerCent( CabControl.SimulationSpeed, CabSetting.Off, Simulator.GameSpeed * 100 );
            }
            if (UserInput.IsPressed(UserCommands.GameSave)) { Program.Save(); }
            if (UserInput.IsPressed(UserCommands.DisplayHelpWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) HelpWindow.TabAction(); else HelpWindow.Visible = !HelpWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayTrackMonitorWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) TrackMonitorWindow.TabAction(); else TrackMonitorWindow.Visible = !TrackMonitorWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayHUD)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) HUDWindow.TabAction(); else HUDWindow.Visible = !HUDWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplaySwitchWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) SwitchWindow.TabAction(); else SwitchWindow.Visible = !SwitchWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayTrainOperationsWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) TrainOperationsWindow.TabAction(); else TrainOperationsWindow.Visible = !TrainOperationsWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayNextStationWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) NextStationWindow.TabAction(); else NextStationWindow.Visible = !NextStationWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayCompassWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) CompassWindow.TabAction(); else CompassWindow.Visible = !CompassWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DebugTracks)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) TracksDebugWindow.TabAction(); else TracksDebugWindow.Visible = !TracksDebugWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DebugSignalling)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) SignallingDebugWindow.TabAction(); else SignallingDebugWindow.Visible = !SignallingDebugWindow.Visible;

            if (UserInput.IsPressed(UserCommands.GameLocomotiveSwap))
            {
                Simulator.Confirmer.Confirm( CabControl.SwitchLocomotive, CabSetting.On );
                Simulator.PlayerLocomotive.Train.LeadNextLocomotive();
                Simulator.PlayerLocomotive = Simulator.PlayerLocomotive.Train.LeadLocomotive;
                Simulator.PlayerLocomotive.Train.CalculatePositionOfCars(0);  // fix the front traveller
                Simulator.PlayerLocomotive.Train.RepositionRearTraveller();    // fix the rear traveller
                PlayerLocomotiveViewer = World.Trains.GetViewer(Simulator.PlayerLocomotive);
                PlayerTrainLength = 0;
				if (MPManager.IsMultiPlayer()) MPManager.LocoChange(Simulator.PlayerLocomotive.Train, Simulator.PlayerLocomotive);
            }
			
			if (UserInput.IsPressed(UserCommands.CameraCab) && CabCamera.IsAvailable) CabCamera.Activate();
			if (UserInput.IsPressed(UserCommands.CameraOutsideFront)) FrontCamera.Activate();
			if (UserInput.IsPressed(UserCommands.CameraJumpingTrains)) RandomSelectTrain(); //hit 9 key, random selected train to have 2 and 3 camera attached to
			if (UserInput.IsPressed(UserCommands.CameraJumpBackPlayer)) { SelectedTrain = PlayerTrain; Camera.Activate(); } //hit ctl-9 key, get back to player train
			if (UserInput.IsPressed(UserCommands.CameraOutsideRear)) BackCamera.Activate();
            if (UserInput.IsPressed(UserCommands.CameraTrackside)) TracksideCamera.Activate();
            if (UserInput.IsPressed(UserCommands.CameraPassenger) && PassengerCamera.IsAvailable) PassengerCamera.Activate();
            if (UserInput.IsPressed(UserCommands.CameraBrakeman)) BrakemanCamera.Activate();
            if (UserInput.IsPressed(UserCommands.CameraFree)) new FreeRoamCamera(this, Camera).Activate();
            if (UserInput.IsPressed(UserCommands.CameraHeadOutForward) && HeadOutForwardCamera.IsAvailable) HeadOutForwardCamera.Activate();
            if (UserInput.IsPressed(UserCommands.CameraHeadOutBackward) && HeadOutBackCamera.IsAvailable) HeadOutBackCamera.Activate();

            if (UserInput.IsPressed(UserCommands.GameSwitchAhead)) Simulator.SwitchTrackAhead(PlayerTrain);
            if (UserInput.IsPressed(UserCommands.GameSwitchBehind)) Simulator.SwitchTrackBehind(PlayerTrain);
            if (UserInput.IsPressed(UserCommands.DebugLocomotiveFlip)) { Simulator.PlayerLocomotive.Flipped = !Simulator.PlayerLocomotive.Flipped; Simulator.PlayerLocomotive.SpeedMpS *= -1; }
			if (UserInput.IsPressed(UserCommands.DebugResetSignal))
			{
				if (MPManager.IsMultiPlayer() && !MPManager.IsServer()) MPManager.Instance().RequestSignalReset();
				else PlayerTrain.ResetSignal(true);
			}
			if (UserInput.IsPressed(UserCommands.ControlMultiPlayerDispatcher)) { DebugViewerEnabled = !DebugViewerEnabled; return; }

            if (!Simulator.Paused && UserInput.IsDown(UserCommands.GameSwitchWithMouse))
            {
                isMouseShouldVisible = true;
                if (UserInput.MouseState.LeftButton == ButtonState.Pressed && UserInput.Changed)
                {
                    TryThrowSwitchAt();
                    UserInput.Handled();
                }
            }
            else if (!Simulator.Paused && UserInput.IsDown(UserCommands.GameUncoupleWithMouse))
            {
                isMouseShouldVisible = true;
                if (UserInput.MouseState.LeftButton == ButtonState.Pressed && UserInput.Changed)
                {
                    TryUncoupleAt();
                    UserInput.Handled();
                }
            }
            else
            {
                isMouseShouldVisible = false;
            }

            if (PlayerTrain != null && PlayerTrainLength != PlayerTrain.Cars.Count)
            {
                PlayerTrainLength = PlayerTrain.Cars.Count;
                if (!Camera.IsAvailable)
                    FrontCamera.Activate();
                else
                    Camera.Activate();
            }

            RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;

            if (UserInput.RDState != null)
                UserInput.RDState.Handled();

            MouseState currentMouseState = Mouse.GetState();

            // Handling mouse movement and timing - GeorgeS
            if (currentMouseState.X != originalMouseState.X ||
                currentMouseState.Y != originalMouseState.Y)
            {
                isMouseTimerVisible = true;
                MouseShownAtRealTime = RealTime;
                RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
            }
            else if (isMouseTimerVisible && MouseShownAtRealTime + .5 < RealTime)
            {
                isMouseTimerVisible = false;
                RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
            }

            originalMouseState = currentMouseState;
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            InfoDisplay.Mark();
            WindowManager.Mark();
        }

        [CallOnThread("Render")]
        public void Unload(RenderProcess renderProcess)
        {
            SoundProcess.RemoveAllSources();
        }

        public void Stop()
        {
			//the dispatcher viewer in MP mode is on, close it first, then wait for the next ESC
			if (MPManager.IsMultiPlayer())
			{
				if (DebugViewerEnabled == true) { DebugViewerEnabled = false; return; }
			}
            InfoDisplay.Stop();
            RenderProcess.Stop();
        }

        /// <summary>
        /// Return true if the screen has changed dimensions
        /// </summary>
        /// <returns></returns>
        bool ScreenHasChanged()
        {
            if (RenderProcess.GraphicsDeviceManager.IsFullScreen != isFullScreen)
            {
                isFullScreen = RenderProcess.GraphicsDeviceManager.IsFullScreen;
                return true;
            }
            return false;
        }

		private int trainCount = 0;
		void RandomSelectTrain()
		{
			Train old = SelectedTrain;
			try
			{
				SortedList<double, Train> users = new SortedList<double, Train>();
				foreach (var t in Simulator.Trains)
				{
					if (t == null || t.Cars == null || t.Cars.Count == 0) continue;
					var d = WorldLocation.GetDistanceSquared(t.RearTDBTraveller.WorldLocation, PlayerTrain.RearTDBTraveller.WorldLocation);
					users.Add(d + Program.Random.NextDouble(), t);
				}
				trainCount++;
				if (trainCount >= users.Count) trainCount = 0;

				SelectedTrain = users.ElementAt(trainCount).Value;
				if (SelectedTrain.Cars == null || SelectedTrain.Cars.Count == 0) SelectedTrain = PlayerTrain;

				//if (SelectedTrain.LeadLocomotive == null) SelectedTrain.LeadNextLocomotive();
				//if (SelectedTrain.LeadLocomotive != null) { PlayerLocomotive = SelectedTrain.LeadLocomotive; PlayerLocomotiveViewer = World.Trains.GetViewer(Simulator.PlayerLocomotive); }
				
			}
			catch (Exception e) 
			{
				SelectedTrain = PlayerTrain;
			}
			if (Camera is PassengerCamera) //passenger camera may jump to a train without passenger view
			{
				if (!Camera.IsAvailable)
				{
					SelectedTrain = old;
				}
			}
			if (old != SelectedTrain) Camera.Activate();
		}
        bool isFullScreen = false;

        /// <summary>
        /// The user has left clicked with U pressed.   
        /// If the mouse was over a coupler, then uncouple the car.
        /// </summary>
        void TryUncoupleAt()
        {
            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = UserInput.FarPoint - UserInput.NearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(UserInput.NearPoint, direction);

            // check each car
            Traveller traveller = new Traveller(PlayerTrain.FrontTDBTraveller, Traveller.TravellerDirection.Backward);
            foreach (TrainCar car in PlayerTrain.Cars)
            {
                float d = (car.CouplerSlackM + car.GetCouplerZeroLengthM()) / 2;
                traveller.Move(car.Length + d);

                Vector3 xnaCenter = Camera.XNALocation(traveller.WorldLocation);
                float radius = 2f;  // 2 meter click range
                BoundingSphere boundingSphere = new BoundingSphere(xnaCenter, radius);

                if (null != pickRay.Intersects(boundingSphere))
                {
                    Simulator.UncoupleBehind(car);
                    break;
                }
                traveller.Move(d);
            }
        }

        /// <summary>
        /// The user has left clicked with U pressed.   
        /// If the mouse was over a coupler, then uncouple the car.
        /// </summary>
        void TryThrowSwitchAt()
        {
            TrJunctionNode bestNode = null;
            float bestD = 10;
            // check each switch
            for (int j = 0; j < Simulator.TDB.TrackDB.TrackNodes.Count(); j++)
            {
                TrackNode tn = Simulator.TDB.TrackDB.TrackNodes[j];
                if (tn != null && tn.TrJunctionNode != null)
                {

                    Vector3 xnaCenter = Camera.XNALocation(new WorldLocation(tn.UiD.TileX, tn.UiD.TileZ, tn.UiD.X, tn.UiD.Y, tn.UiD.Z));
                    float d = ORTSMath.LineSegmentDistanceSq(xnaCenter, UserInput.NearPoint, UserInput.FarPoint);
                    if (bestD > d && !Simulator.SwitchIsOccupied(j))
                    {
                        bestNode = tn.TrJunctionNode;
                        bestD = d;
                    }
                }
            }
            if (bestNode != null)
                bestNode.SelectedRoute = 1 - bestNode.SelectedRoute;
        }
    }
}
