﻿// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.Viewer3D;
using System;
using System.Linq;

namespace ORTS.Viewer3D.Popups
{
    public class CarOperationsWindow :Window
    {
        readonly Viewer Viewer;

        public int CarPosition
        {
            set;
            get;
        }

        public CarOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 12, Window.DecorationSize.Y + owner.TextFontDefault.Height * 14, Viewer.Catalog.GetString("Car Operation Menu"))
        {
            Viewer = owner.Viewer;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonHandbrake, buttonTogglePower, buttonToggleMU, buttonToggleBrakeHose, buttonToggleAngleCockA, buttonToggleAngleCockB, buttonToggleBleedOffValve, buttonClose;           

            var vbox = base.Layout(layout).AddLayoutVertical();
            var heightForLabels = 10;
			heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 8;
            var spacing = (heightForLabels - Owner.TextFontDefault.Height) / 3;
            vbox.AddSpace(0, spacing);

            vbox.Add(buttonHandbrake = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Handbrake"), LabelAlignment.Center));

            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();

            buttonTogglePower = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Power"), LabelAlignment.Center);

			vbox.AddSpace(0, spacing);
			vbox.Add(buttonTogglePower);
			
            vbox.AddSpace(0, spacing);
			vbox.AddHorizontalSeparator();
			buttonTogglePower.Click += new Action<Control, Point>(buttonTogglePower_Click);
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonToggleMU = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle MU Connection"), LabelAlignment.Center));
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonToggleBrakeHose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Brake Hose Connection"), LabelAlignment.Center));
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonToggleAngleCockA = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Front Angle Cock"), LabelAlignment.Center));
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonToggleAngleCockB = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Rear Angle Cock"), LabelAlignment.Center));
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonToggleBleedOffValve = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Bleed Off Valve"), LabelAlignment.Center));
            buttonHandbrake.Click += new Action<Control, Point>(buttonHandbrake_Click);
            buttonToggleMU.Click += new Action<Control, Point>(buttonToggleMU_Click);
            buttonToggleBrakeHose.Click += new Action<Control, Point>(buttonToggleBrakeHose_Click);
            buttonToggleAngleCockA.Click += new Action<Control, Point>(buttonToggleAngleCockA_Click);
            buttonToggleAngleCockB.Click += new Action<Control, Point>(buttonToggleAngleCockB_Click);
            buttonToggleBleedOffValve.Click += new Action<Control, Point>(buttonToggleBleedOffValve_Click);
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));
            buttonClose.Click += new Action<Control, Point>(buttonClose_Click);

            return vbox;
        }

        void buttonClose_Click(Control arg1, Point arg2)
        {
            Visible = false;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);


            //if (updateFull)
            //{
            //    if ((PlayerTrain != Owner.Viewer.PlayerTrain))
            //    {
            //        PlayerTrain = Owner.Viewer.PlayerTrain;
            //        Layout();
            //    }
            //}
        }

        

        void buttonHandbrake_Click(Control arg1, Point arg2)
        {
            new WagonHandbrakeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus());
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus())
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake set"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake off"));
            Visible = false;
        }

        void buttonTogglePower_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive)))
            {
                new PowerCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn);
                if((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No power command for this type of car!"));

            Visible = false;
        }

        void buttonToggleMU_Click(Control arg1, Point arg2)
        {
            
            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive)))
            {
                new ToggleMUCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptMUSignals);
                if((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptMUSignals)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal connected"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal disconnected"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No MU command for this type of car!"));

            Visible = false;
        }

        void buttonToggleBrakeHose_Click(Control arg1, Point arg2)
        {
            new WagonBrakeHoseConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose connected"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose disconnected"));
        }

        void buttonToggleAngleCockA_Click(Control arg1, Point arg2)
        {
            new ToggleAngleCockACommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock closed"));
        }

        void buttonToggleAngleCockB_Click(Control arg1, Point arg2)
        {
            new ToggleAngleCockBCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock closed"));
        }

        void buttonToggleBleedOffValve_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe)
                return;

            new ToggleBleedOffValveCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve closed"));
        }
    }
}

