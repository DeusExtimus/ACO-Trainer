using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ACOverlay
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct SPageFilePhysics
    {
        public int   packetId;
        public float gas;
        public float brake;
        public float fuel;
        public int   gear;
        public int   rpms;
        public float steerAngle;
        public float speedKmh;
        public float velocityX, velocityY, velocityZ;
        public float accG_X, accG_Y, accG_Z;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] wheelSlip;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] wheelLoad;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] wheelPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] wheelAngularSpeed;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreWear;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreDirtyLevel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreCoreTemperature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] camberRAD;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] suspensionTravel;
        public float drs, tc, heading, pitch, roll, cgHeight;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] public float[] carDamage;
        public int   numberOfTyresOut, pitLimiterOn;
        public float abs, kersCharge, kersInput;
        public int   autoShifterOn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public float[] rideHeight;
        public float turboBoost, ballast, airDensity, airTemp, roadTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public float[] localAngularVelocity;
        public float finalFF, performanceMeter;
        public int   engineBrake, ersRecoveryLevel, ersPowerLevel, ersHeatCharging, ersIsCharging;
        public float kersCurrentKJ;
        public int   drsAvailable, drsEnabled;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] brakeTemp;
        public float clutch;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreTempI;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreTempM;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreTempO;
        public int   isAIControlled;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactPoint_X;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactPoint_Y;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactPoint_Z;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactNormal_X;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactNormal_Y;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactNormal_Z;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactHeading_X;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactHeading_Y;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreContactHeading_Z;
        public float brakeBias;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] localVelocity;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct SPageFileGraphic
    {
        public int   packetId;
        public int   status;
        public int   session;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)] public string currentTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)] public string lastTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)] public string bestTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)] public string split;
        public int   completedLaps;
        public int   position;
        public int   iCurrentTime;
        public int   iLastTime;
        public int   iBestTime;
        public float sessionTimeLeft;
        public float distanceTraveled;
        public int   isInPit;
        public int   currentSectorIndex;
        public int   lastSectorTime;
        public int   numberOfLaps;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string tyreCompound;
        public float replayTimeMultiplier;
        public float normalizedCarPosition;
        public float carCoordinates_X;
        public float carCoordinates_Y;
        public float carCoordinates_Z;
        public float penaltyTime;
        public int   flag, idealLineOn, isInPitLane;
        public float surfaceGrip;
        public int   mandatoryPitDone;
        public float windSpeed, windDirection;
        public int   isSetupMenuVisible, mainDisplayIndex, secondaryDisplayIndex;
        public int   TC, TCCut, engineMap, ABS, fuelXLap;
        public int   rainLights, flashingLights, lightsStage;
        public float exhaustTemperature;
        public int   wiperLV, driverStintTotalTimeLeft, driverStintTimeLeft, rainTyres;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct SPageFileStatic
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)] public string smVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)] public string acVersion;
        public int numberOfSessions;
        public int numCars;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string carModel;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string track;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string playerName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string playerSurname;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string playerNick;
        public int sectorCount;
        public float maxTorque;
        public float maxPower;
        public int maxRpm;
        public float maxFuel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] suspensionMaxTravel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] tyreRadius;
    }

    public class ACSharedMemoryReader : IDisposable
    {
        private MemoryMappedFile? _mmfPhysics;
        private MemoryMappedFile? _mmfGraphics;
        private MemoryMappedFile? _mmfStatic;

        public bool Open()
        {
            try
            {
                _mmfPhysics  = MemoryMappedFile.OpenExisting("Local\\acpmf_physics");
                _mmfGraphics = MemoryMappedFile.OpenExisting("Local\\acpmf_graphics");
                _mmfStatic   = MemoryMappedFile.OpenExisting("Local\\acpmf_static");
                return true;
            }
            catch { return false; }
        }

        public bool IsOpen => _mmfPhysics != null && _mmfGraphics != null;

        public void TryReopen()
        {
            _mmfPhysics?.Dispose();  _mmfPhysics  = null;
            _mmfGraphics?.Dispose(); _mmfGraphics = null;
            _mmfStatic?.Dispose();   _mmfStatic   = null;
            Open();
        }

        public SPageFilePhysics ReadPhysics()  => ReadStruct<SPageFilePhysics>(_mmfPhysics!);
        public SPageFileGraphic ReadGraphics() => ReadStruct<SPageFileGraphic>(_mmfGraphics!);
        public SPageFileStatic  ReadStatic()   => _mmfStatic != null
            ? ReadStruct<SPageFileStatic>(_mmfStatic)
            : default;

        private static T ReadStruct<T>(MemoryMappedFile mmf) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buf = new byte[size];
            using var s = mmf.CreateViewStream(0, size);
            s.Read(buf, 0, size);
            var h = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try   { return Marshal.PtrToStructure<T>(h.AddrOfPinnedObject()); }
            finally { h.Free(); }
        }

        public void Dispose()
        {
            _mmfPhysics?.Dispose();
            _mmfGraphics?.Dispose();
            _mmfStatic?.Dispose();
        }
    }
}
