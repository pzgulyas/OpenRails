﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace ORTS
{
    public static class RollingStock
    {
        public static TrainCar Load(string wagFilePath)
        {
            GenericWAGFile wagFile = SharedGenericWAGFileManager.Get(wagFilePath);  
            TrainCar car;
            if( wagFile.OpenRails != null 
               && wagFile.OpenRails.DLL != null)
            {  // wag file specifies an external DLL
                try
                {
                    // TODO search the path list
                    string wagFolder = Path.GetDirectoryName(wagFilePath);
                    string dllPath = ORTSPaths.FindTrainCarPlugin(wagFolder, wagFile.OpenRails.DLL);
                    Assembly customDLL = Assembly.LoadFrom(dllPath);
                    object[] args = new object[] { wagFilePath };
                    car = (TrainCar)customDLL.CreateInstance("ORTS.CustomCar", true, BindingFlags.CreateInstance, null, args,
    null, null);
                    return car;
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine(error.Message);
                }
            }
            if (!wagFile.IsEngine)
            {   // its an ordinary MSTS wagon
                car = new MSTSWagon(wagFilePath);
            }
            else
            {   // its an ordinary MSTS engine of some type.
                if (wagFile.Engine.Type == null)
                    throw new System.Exception(wagFilePath + "\r\n\r\nEngine type missing");

                switch (wagFile.Engine.Type.ToLower())
                {
                        // TODO complete parsing of proper car types
                    case "electric": //car = new ElectricLocomotive(wagFile); break;
                        car = new MSTSElectricLocomotive(wagFilePath);
                        break;
                    case "steam": //car = new SteamLocomotive(wagFile); break;
                    case "diesel": //car = new DieselLocomotive(wagFile); break;
                        car = new MSTSLocomotive(wagFilePath);
                        break;
                    default: throw new System.Exception(wagFilePath + "\r\n\r\nUnknown engine type: " + wagFile.Engine.Type);
                }
            }
            return car;
        }


        /// <summary>
        /// Utility class to avoid loading multiple copies of the same file.
        /// </summary>
        public class SharedGenericWAGFileManager
        {
            private static Dictionary<string, GenericWAGFile> SharedWAGFiles = new Dictionary<string, GenericWAGFile>();

            public static GenericWAGFile Get(string path)
            {
                if (!SharedWAGFiles.ContainsKey(path))
                {
                    GenericWAGFile wagFile = new GenericWAGFile(path);
                    SharedWAGFiles.Add(path, wagFile);
                    return wagFile;
                }
                else
                {
                    return SharedWAGFiles[path];
                }
            }
        }

        /// <summary>
        /// This is an abbreviated parse to determine where to direct the file.
        /// </summary>
        public class GenericWAGFile
        {
            public bool IsEngine { get { return Engine != null; } }
            public EngineClass Engine = null;
            public OpenRailsData OpenRails = null;

            public GenericWAGFile(string filenamewithpath)
            {
                WagFile(filenamewithpath);
            }

            public void WagFile(string filenamewithpath)
            {
                STFReader f = new STFReader(filenamewithpath);
                while (!f.EndOfBlock())
                {
                    string token = f.ReadToken();
                    switch (token.ToLower())
                    {
                        case "engine": Engine = new EngineClass(f); break;
                        case "_openrails": OpenRails = new OpenRailsData(f); break;
                        default: f.SkipBlock(); break;
                    }
                }
                f.Close();
            }

            public class EngineClass
            {
                public string Type = null;

                public EngineClass(STFReader f)
                {
                    f.VerifyStartOfBlock();
                    f.ReadToken();
                    while (!f.EndOfBlock())
                    {
                        string token = f.ReadToken();
                        switch (token.ToLower())
                        {
                            case "type": Type = f.ReadStringBlock(); break;
                            default: f.SkipBlock(); break; // TODO complete parse and replace with f.SkipUnknownBlock ...
                        }
                    }
                }
            } // class WAGFile.Engine

            public class OpenRailsData
            {
                public string DLL = null;

                public OpenRailsData(STFReader f)
                {
                    f.VerifyStartOfBlock();
                    while (!f.EndOfBlock())
                    {
                        string token = f.ReadToken();
                        switch (token.ToLower())
                        {
                            case "dll": DLL = f.ReadStringBlock(); break;
                            default: f.SkipBlock(); break; // TODO complete parse and replace with f.SkipUnknownBlock ...
                        }
                    }
                }
            } // class WAGFile.Engine

        }// class WAGFile


    }
}
