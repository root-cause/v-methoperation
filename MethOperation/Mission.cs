using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using MethOperation.Enums;
using MethOperation.Classes;

namespace MethOperation
{
    public class Mission : Script
    {
        public static bool IsActive = false;

        private static MissionType _missionType = MissionType.None;
        private static MissionStage _missionStage = MissionStage.None;
        private static int _missionEndTime = -1;
        private static int _missionModel = -1;
        private static int _missionLabIdx = -1;
        private static int _productAmount = 0;

        private static Vehicle _missionVehicle = null;
        private static Dictionary<string, Blip> _missionBlips = new Dictionary<string, Blip>();

        private static List<Vector3> _objectiveCoords = new List<Vector3>();
        private static string _objectiveText = string.Empty;

        private static TimerBarPool _missionBarPool = null;
        private static TextTimerBar _missionTimeBar = null;

        private Player _player = null;
        private Ped _playerPed = null;
        private int _nextCheck = 0;

        #region Methods
        public static void Start(MissionType type, int labIndex, List<Vector3> coords, float vehicleHeading, int timeMs, int productAmount = 0)
        {
            if (!IsActive)
            {
                IsActive = true;

                _missionType = type;
                _missionEndTime = Game.GameTime + timeMs;
                _missionModel = Game.Player.Character.Model.Hash;
                _missionLabIdx = labIndex;
                _productAmount = productAmount;

                _objectiveCoords = coords;

                if (_missionBarPool == null)
                {
                    _missionBarPool = new TimerBarPool();

                    _missionTimeBar = new TextTimerBar("TIME LEFT", "00:00");
                    _missionBarPool.Add(_missionTimeBar);
                }

                foreach (Vector3 pos in _objectiveCoords) Function.Call(Hash.CLEAR_AREA, pos.X, pos.Y, pos.Z, 5f, true, false, false, false);

                // spaghetti
                switch (_missionType)
                {
                    case MissionType.Setup:
                    {
                        _missionVehicle = World.CreateVehicle(VehicleHash.Pounder, _objectiveCoords[0], vehicleHeading);
                        _missionVehicle.PrimaryColor = VehicleColor.MetallicBlack;
                        _missionVehicle.SecondaryColor = VehicleColor.MetallicBlack;
                        _missionVehicle.IsPersistent = true;

                        Blip vehicleBlip = _missionVehicle.AddBlip();
                        vehicleBlip.Sprite = BlipSprite.Package2;
                        vehicleBlip.Color = BlipColor.Blue;
                        vehicleBlip.IsShortRange = false;
                        vehicleBlip.Name = "Supply Truck";

                        Blip deliveryBlip = World.CreateBlip(_objectiveCoords[1]);
                        deliveryBlip.Sprite = BlipSprite.Standard;
                        deliveryBlip.Color = BlipColor.Yellow;
                        deliveryBlip.IsShortRange = false;
                        deliveryBlip.Alpha = 0;
                        deliveryBlip.Name = "Meth Lab";

                        _missionBlips.Add("vehicle", vehicleBlip);
                        _missionBlips.Add("deliveryPoint", deliveryBlip);

                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, a gang member posted about their supply truck online. Go to {World.GetZoneName(_objectiveCoords[0])} and take it, they won't even know!", "CHAR_LJT", 1);
                        break;
                    }

                    case MissionType.Delivery:
                    {
                        _missionVehicle = World.CreateVehicle(Constants.DeliveryVehicles[ Constants.RandomGenerator.Next(0, Constants.DeliveryVehicles.Length) ], _objectiveCoords[0], vehicleHeading);
                        _missionVehicle.IsPersistent = true;

                        Blip vehicleBlip = _missionVehicle.AddBlip();
                        vehicleBlip.Sprite = BlipSprite.Package2;
                        vehicleBlip.Color = BlipColor.Blue;
                        vehicleBlip.IsShortRange = false;
                        vehicleBlip.Name = "Delivery Vehicle";

                        Blip deliveryBlip = World.CreateBlip(_objectiveCoords[1]);
                        deliveryBlip.Sprite = BlipSprite.Standard;
                        deliveryBlip.Color = BlipColor.Yellow;
                        deliveryBlip.IsShortRange = false;
                        deliveryBlip.Alpha = 0;
                        deliveryBlip.Name = "Delivery Point";

                        _missionBlips.Add("vehicle", vehicleBlip);
                        _missionBlips.Add("deliveryPoint", deliveryBlip);

                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, the buyer wants the product to be delivered to {World.GetZoneName(_objectiveCoords[1])}.", "CHAR_LJT", 1);
                        break;
                    }
                }

                foreach (Lab lab in Main.MethLabs) lab.SetBlipVisible(false);
                SetStage(MissionStage.GetInVehicle);
            }
        }

        public static void Complete()
        {
            if (IsActive)
            {
                switch (_missionType)
                {
                    case MissionType.Setup:
                        Main.MethLabs[_missionLabIdx].AddFlag(LabFlags.HasDoneSetup);
                        Main.MethLabs[_missionLabIdx].LastVisit = Util.GetGameDate();
                        Main.Save();

                        Util.NotifyWithPicture("LJT", $"Good job {Util.GetCharacterFromModel(_missionModel)}, the meth lab is now operational! Don't forget to visit it every now and then to sell the produced meth.", "CHAR_LJT", 1);
                        break;

                    case MissionType.Delivery:
                        int reward = _productAmount * Main.ProductValue;
                        Game.Player.Money += reward;

                        Util.NotifyWithPicture("LJT", $"Great! {Util.GetCharacterFromModel(_missionModel)}, you just made a sale worth ${reward:N0}!", "CHAR_LJT", 1);
                        break;
                }

                Stop();
            }
        }

        public static void Stop(MissionFailReason failReason = MissionFailReason.None)
        {
            if (IsActive)
            {
                switch (failReason)
                {
                    case MissionFailReason.MissionFlag:
                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, try focusing on your task at hand next time!", "CHAR_LJT", 1);
                        break;

                    case MissionFailReason.Arrested:
                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, be more careful with the cops next time!", "CHAR_LJT", 1);
                        break;

                    case MissionFailReason.Wasted:
                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, that was really... unprofessional.", "CHAR_LJT", 1);
                        break;

                    case MissionFailReason.CharacterChange:
                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, that was not a good time to have a personality change...", "CHAR_LJT", 1);
                        break;

                    case MissionFailReason.OutOfTime:
                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, I've seen slow, but this? This is something new...", "CHAR_LJT", 1);
                        break;

                    case MissionFailReason.VehicleDead:
                        Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(_missionModel)}, be more careful with the vehicle next time!", "CHAR_LJT", 1);
                        break;
                }

                if (_missionVehicle != null)
                {
                    if (failReason == MissionFailReason.ScriptExit)
                    {
                        _missionVehicle.Delete();
                    }
                    else
                    {
                        Function.Call((Hash)0x260BE8F09E326A20, _missionVehicle.Handle, 3.0, 1);
                        Function.Call(Hash.SET_VEHICLE_IS_CONSIDERED_BY_PLAYER, _missionVehicle.Handle, false);
                        if (Game.Player.Character.IsInVehicle(_missionVehicle)) Game.Player.Character.Task.LeaveVehicle();

                        _missionVehicle.IsPersistent = false;
                        _missionVehicle.MarkAsNoLongerNeeded();
                    }

                    _missionVehicle = null;
                }

                if (_missionType == MissionType.Delivery && failReason != MissionFailReason.ScriptExit)
                {
                    Main.MethLabs[_missionLabIdx].Product -= _productAmount;
                    Main.Save();
                }

                foreach (Blip blip in _missionBlips.Values) blip.Remove();
                foreach (Lab lab in Main.MethLabs) lab.SetBlipVisible(true);

                _missionType = MissionType.None;
                _missionStage = MissionStage.None;
                _missionEndTime = -1;
                _missionModel = -1;
                _missionLabIdx = -1;
                _productAmount = 0;

                _objectiveCoords.Clear();
                _missionBlips.Clear();

                IsActive = false;
            }
        }

        private static void SetStage(MissionStage newStage)
        {
            if (IsActive)
            {
                switch (newStage)
                {
                    case MissionStage.LoseCops:
                        _missionStage = newStage;
                        _objectiveText = "Lose the cops.";

                        foreach (Blip blip in _missionBlips.Values)
                        {
                            if (blip != null)
                            {
                                blip.ShowRoute = false;
                                blip.Alpha = 0;
                            }
                        }

                        break;

                    case MissionStage.GetInVehicle:
                        if (Game.Player.WantedLevel > 0)
                        {
                            _missionStage = MissionStage.LoseCops;
                            _objectiveText = "Lose the cops.";

                            foreach (Blip blip in _missionBlips.Values)
                            {
                                if (blip != null)
                                {
                                    blip.Alpha = 0;
                                    blip.ShowRoute = false;
                                }
                            }
                        }
                        else
                        {
                            if (_missionBlips["vehicle"] != null) _missionBlips["vehicle"].Alpha = 255;

                            _missionStage = MissionStage.GetInVehicle;
                            _objectiveText = _missionType == MissionType.Setup ? "Steal the ~b~supply truck." : "Get in the ~b~delivery vehicle.";

                            if (_missionBlips["deliveryPoint"] != null)
                            {
                                _missionBlips["deliveryPoint"].ShowRoute = false;
                                _missionBlips["deliveryPoint"].Alpha = 0;
                            }
                        }

                        break;

                    case MissionStage.DeliverVehicle:
                        if (_missionBlips["vehicle"] != null) _missionBlips["vehicle"].Alpha = 0;

                        if (Game.Player.WantedLevel > 0)
                        {
                            _missionStage = MissionStage.LoseCops;
                            _objectiveText = "Lose the cops.";

                            foreach (Blip blip in _missionBlips.Values)
                            {
                                if (blip != null)
                                {
                                    blip.ShowRoute = false;
                                    blip.Alpha = 0;
                                }
                            }
                        }
                        else
                        {
                            if (_missionBlips["deliveryPoint"] != null)
                            {
                                _missionBlips["deliveryPoint"].ShowRoute = true;
                                _missionBlips["deliveryPoint"].Alpha = 255;
                            }

                            _missionStage = MissionStage.DeliverVehicle;
                            _objectiveText = _missionType == MissionType.Setup ? "Deliver the truck to the ~y~meth lab." : "Go to the ~y~delivery point.";
                        }

                        break;
                }
            }
        }
        #endregion

        public Mission()
        {
            Tick += Mission_Tick;
            Aborted += Mission_Aborted;
        }

        #region Event: Tick
        public void Mission_Tick(object sender, EventArgs e)
        {
            if (IsActive)
            {
                // Prevent the player from switching characters
                Game.DisableControlThisFrame(2, Control.CharacterWheel);

                // Drawing
                _missionBarPool?.Draw();
                if (_missionStage == MissionStage.DeliverVehicle) World.DrawMarker(MarkerType.VerticalCylinder, _objectiveCoords[1], Vector3.Zero, Vector3.Zero, Constants.MissionMarkerScale, Constants.MissionMarkerColor);

                int gameTime = Game.GameTime;
                if (gameTime > _nextCheck)
                {
                    _nextCheck = gameTime + Constants.MissionUpdateInterval;

                    if (Game.MissionFlag)
                    {
                        Stop(MissionFailReason.MissionFlag);
                        return;
                    }

                    _player = Game.Player;
                    _playerPed = _player.Character;

                    if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, _player.Handle, false))
                    {
                        Stop(MissionFailReason.Arrested);
                        return;
                    }
                    else if (_playerPed.IsDead)
                    {
                        Stop(MissionFailReason.Wasted);
                        return;
                    }
                    else if (_playerPed.Model.Hash != _missionModel)
                    {
                        Stop(MissionFailReason.CharacterChange);
                        return;
                    }
                    else if (Game.GameTime > _missionEndTime)
                    {
                        Stop(MissionFailReason.OutOfTime);
                        return;
                    }

                    TimeSpan ts = TimeSpan.FromMilliseconds(_missionEndTime - gameTime);
                    _missionTimeBar.Text = $"{ts.Minutes.ToString("D2")}:{ts.Seconds.ToString("D2")}";

                    if (_missionVehicle != null && _missionVehicle.IsDead)
                    {
                        Stop(MissionFailReason.VehicleDead);
                        return;
                    }

                    switch (_missionStage)
                    {
                        case MissionStage.LoseCops:
                            if (_player.WantedLevel < 1) SetStage(_playerPed.IsInVehicle(_missionVehicle) ? MissionStage.DeliverVehicle : MissionStage.GetInVehicle);
                            break;

                        case MissionStage.GetInVehicle:
                            if (_player.WantedLevel > 0)
                            {
                                SetStage(MissionStage.LoseCops);
                            }
                            else
                            {
                                if (_playerPed.IsInVehicle(_missionVehicle)) SetStage(MissionStage.DeliverVehicle);
                            }
                            break;

                        case MissionStage.DeliverVehicle:
                            if (_player.WantedLevel > 0)
                            {
                                SetStage(MissionStage.LoseCops);
                            }
                            else
                            { 
                                if (!_playerPed.IsInVehicle(_missionVehicle))
                                {
                                    SetStage(MissionStage.GetInVehicle);
                                }
                                else
                                {
                                    if (_playerPed.Position.DistanceTo(_objectiveCoords[1]) <= 5f) Complete();
                                }
                            }

                            break;
                    }

                    UI.ShowSubtitle(_objectiveText, Constants.MissionUpdateInterval + 100);
                }
            }
        }
        #endregion

        #region Event: Aborted
        public void Mission_Aborted(object sender, EventArgs e)
        {
            Stop(MissionFailReason.ScriptExit);

            _missionTimeBar = null;
            _missionBarPool = null;
        }
        #endregion
    }
}
