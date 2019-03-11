using System;
using System.IO;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using MethOperation.Enums;
using MethOperation.Classes;

namespace MethOperation
{
    #region Delegates
    delegate void LabEnterEvent(int labIndex);
    delegate void LabExitEvent(int labIndex, LabExitReason reason);
    #endregion

    public class Main : Script
    {
        // Script variables
        int LabInteriorID = 0;

        public static List<Lab> MethLabs = new List<Lab>();
        List<Entity> MethLabEntities = new List<Entity>();

        Blip ManagementBlip = null;
        bool MethLabsLoaded = false;

        int InteractableLabIdx = -1;
        int InsideMethLabIdx = -1;

        Vector3 PlayerPosition = Vector3.Zero;
        float DistanceToInteractable = -1.0f;
        InteractionType CurrentInteractionType = InteractionType.None;

        int PlayerLabModel = -1;
        int NextUpdate = -1;
        int LaptopRTID = -1;
        bool IsLeaning = false;

        // Events
        event LabEnterEvent EnteredMethLab;
        event LabExitEvent LeftMethLab;

        // NativeUI variables
        MenuPool ManagementMenuPool;
        UIMenu ManagementMain;
        UIMenu UpgradesMenu;
        UIMenu SaleConfirmationMenu;
        UIMenu LabSaleConfirmationMenu;

        // Config variables
        int InteractionControl = 51;
        int MissionTime = 15;
        int ProductionTime = 6;
        int PoliceChance = 15;
        int PoliceStars = 2;
        int EquipmentUpgradePrice = 1100000;
        int StaffUpgradePrice = 331500;
        int SecurityUpgradePrice = 513000;
        public static int ProductValue = 8500;

        #region Methods
        public static void Save()
        {
            File.WriteAllText(Path.Combine("scripts", "methoperation_labs.xml"), MethLabs.Serialize());
        }
        #endregion

        public Main()
        {
            // Load settings
            try
            {
                string configFile = Path.Combine("scripts", "methoperation_config.ini");
                ScriptSettings config = ScriptSettings.Load(configFile);

                if (File.Exists(configFile))
                {
                    InteractionControl = config.GetValue("CONFIG", "INTERACTION_CONTROL", 51);
                    MissionTime = config.GetValue("CONFIG", "MISSION_TIME", 15);
                    ProductionTime = config.GetValue("CONFIG", "PRODUCTION_TIME", 6);
                    PoliceChance = config.GetValue("CONFIG", "POLICE_CHANCE", 15);
                    PoliceStars = config.GetValue("CONFIG", "POLICE_STARS", 2);
                    EquipmentUpgradePrice = config.GetValue("PRICES", "EQUIPMENT_UPGRADE", 1100000);
                    StaffUpgradePrice = config.GetValue("PRICES", "STAFF_UPGRADE", 331500);
                    SecurityUpgradePrice = config.GetValue("PRICES", "SECURITY_UPGRADE", 513000);
                    ProductValue = config.GetValue("PRICES", "PRODUCT_VALUE", 8500);
                }
                else
                {
                    config.SetValue("CONFIG", "INTERACTION_CONTROL", InteractionControl);
                    config.SetValue("CONFIG", "MISSION_TIME", MissionTime);
                    config.SetValue("CONFIG", "PRODUCTION_TIME", ProductionTime);
                    config.SetValue("CONFIG", "POLICE_CHANCE", PoliceChance);
                    config.SetValue("CONFIG", "POLICE_STARS", PoliceStars);
                    config.SetValue("PRICES", "EQUIPMENT_UPGRADE", EquipmentUpgradePrice);
                    config.SetValue("PRICES", "STAFF_UPGRADE", StaffUpgradePrice);
                    config.SetValue("PRICES", "SECURITY_UPGRADE", SecurityUpgradePrice);
                    config.SetValue("PRICES", "PRODUCT_VALUE", ProductValue);
                }

                config.Save();
            }
            catch (Exception e)
            {
                UI.Notify($"~r~MethOperation settings error: {e.Message}");
            }

            // Set up NativeUI
            ManagementMenuPool = new MenuPool();
            ManagementMain = new UIMenu("Meth Lab", string.Empty);
            UpgradesMenu = new UIMenu("Meth Lab", "~b~UPGRADES");
            SaleConfirmationMenu = new UIMenu("Meth Lab", "~b~PRODUCT SALE CONFIRMATION");
            LabSaleConfirmationMenu = new UIMenu("Meth Lab", "~b~LAB SALE CONFIRMATION");

            // Upgrades menu
            UIMenuItem tempItem = new UIMenuItem("Equipment Upgrade", "Buy better equipment to speed up the production. ~r~No refunds!");
            tempItem.SetRightLabel($"${EquipmentUpgradePrice:N0}");
            UpgradesMenu.AddItem(tempItem);

            tempItem = new UIMenuItem("Staff Upgrade", "Hire more people to speed up the production. ~r~No refunds!");
            tempItem.SetRightLabel($"${StaffUpgradePrice:N0}");
            UpgradesMenu.AddItem(tempItem);

            tempItem = new UIMenuItem("Security Upgrade", "Eyes and ears in the lab to enforce discipline. ~r~No refunds!");
            tempItem.SetRightLabel($"${SecurityUpgradePrice:N0}");
            UpgradesMenu.AddItem(tempItem);

            // Product sale confirmation menu
            SaleConfirmationMenu.AddItem(new UIMenuItem("Confirm", "Start a delivery mission to sell your product. ~r~All product will be lost if you fail!"));
            SaleConfirmationMenu.AddItem(new UIMenuItem("Cancel", "Go back to the management menu."));

            // Lab sale confirmation menu
            LabSaleConfirmationMenu.AddItem(new UIMenuItem("Confirm", "You'll get the amount displayed on right. ~r~Upgrades and all produced meth will be lost!"));
            LabSaleConfirmationMenu.AddItem(new UIMenuItem("Cancel", "Go back to the management menu."));

            ManagementMenuPool.Add(ManagementMain);
            ManagementMenuPool.Add(UpgradesMenu);
            ManagementMenuPool.Add(SaleConfirmationMenu);
            ManagementMenuPool.Add(LabSaleConfirmationMenu);

            // Set up event handlers
            EnteredMethLab += Script_EnteredMethLab;
            LeftMethLab += Script_LeftMethLab;

            ManagementMain.OnItemSelect += ManagementMain_ItemSelected;
            UpgradesMenu.OnItemSelect += UpgradesMenu_ItemSelected;
            SaleConfirmationMenu.OnItemSelect += SaleConfirmationMenu_ItemSelected;
            LabSaleConfirmationMenu.OnItemSelect += LabSaleConfirmationMenu_ItemSelected;

            Tick += Script_Tick;
            Aborted += Script_Aborted;
        }

        #region Event: Tick
        public void Script_Tick(object sender, EventArgs e)
        {
            // Load labs
            #region Lab Loading
            if (!MethLabsLoaded && !Game.IsLoading && Game.Player.CanControlCharacter)
            {
                try
                {
                    string labsFile = Path.Combine("scripts", "methoperation_labs.xml");
                    if (File.Exists(labsFile))
                    {
                        // Loading MP maps is required since the lab is a GTA Online interior
                        Function.Call(Hash._LOAD_MP_DLC_MAPS);

                        MethLabs = XmlUtil.Deserialize<List<Lab>>(File.ReadAllText(labsFile));
                        foreach (Lab lab in MethLabs) lab.CreateEntities();

                        LabInteriorID = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS_WITH_TYPE, Constants.MethLabLaptop.X, Constants.MethLabLaptop.Y, Constants.MethLabLaptop.Z, "bkr_biker_dlc_int_ware01");

                        ManagementBlip = World.CreateBlip(Constants.MethLabLaptop);
                        ManagementBlip.Alpha = 0;
                        ManagementBlip.Sprite = BlipSprite.Laptop;
                        ManagementBlip.IsShortRange = true;
                        Function.Call(Hash.SET_BLIP_DISPLAY, ManagementBlip.Handle, 8);
                    }
                    else
                    {
                        UI.Notify($"~r~MethOperation labs file not found!");
                    }
                }
                catch (Exception ex)
                {
                    UI.Notify($"~r~MethOperation loading error: {ex.Message}");
                }

                MethLabsLoaded = true;
            }
            #endregion

            // No need to go further if in a mission
            if (Mission.IsActive) return;

            // Update player position
            int gameTime = Game.GameTime;
            if (gameTime > NextUpdate)
            {
                NextUpdate = gameTime + Constants.UpdateInterval;
                PlayerPosition = Game.Player.Character.Position;

                if (InsideMethLabIdx != -1)
                {
                    int interiorID = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS_WITH_TYPE, PlayerPosition.X, PlayerPosition.Y, PlayerPosition.Z, "bkr_biker_dlc_int_ware01");
                    if (interiorID != LabInteriorID)
                    {
                        LeftMethLab.Invoke(InsideMethLabIdx, LabExitReason.Teleport);
                        InsideMethLabIdx = -1;
                    }
                    else if (Game.Player.Character.Model.Hash != PlayerLabModel)
                    {
                        LeftMethLab.Invoke(InsideMethLabIdx, LabExitReason.CharacterChange);
                        InsideMethLabIdx = -1;
                    }
                }
            }

            // Draw markers & helptexts
            #region Drawing
            if (InsideMethLabIdx != -1)
            {
                // Handle NativeUI
                ManagementMenuPool.ProcessMenus();

                // Inside, draw interactable markers
                World.DrawMarker(MarkerType.VerticalCylinder, Constants.MethLabExit, Vector3.Zero, Vector3.Zero, Constants.MarkerScale, Constants.MarkerColor);
                World.DrawMarker(MarkerType.VerticalCylinder, Constants.MethLabLaptop, Vector3.Zero, Vector3.Zero, Constants.MarkerScale, Constants.MarkerColor);

                // Laptop screen
                if (LaptopRTID != -1)
                {
                    Function.Call(Hash.SET_TEXT_RENDER_ID, LaptopRTID);
                    Function.Call(Hash._SET_SCREEN_DRAW_POSITION, 73, 73);
                    Function.Call(Hash._SET_2D_LAYER, 4);
                    Function.Call(Hash._0xC6372ECD45D73BCD, true);
                    Function.Call((Hash)0x2BC54A8188768488, "prop_screen_biker_laptop", "prop_screen_biker_laptop_2", 0.5f, 0.5f, 1f, 1f, 0f, 255, 255, 255, 255);
                    Function.Call((Hash)0xE3A3DB414A373DAB);
                    Function.Call(Hash.SET_TEXT_RENDER_ID, 1);
                }

                if (PlayerPosition.DistanceTo(Constants.MethLabExit) <= Constants.MarkerInteractionDistance)
                {
                    CurrentInteractionType = InteractionType.ExitMethLab;
                    Util.DisplayHelpText($"Press {HelpTextKeys.Get(InteractionControl)} to leave the meth lab.");
                }
                else if (PlayerPosition.DistanceTo(Constants.MethLabLaptop) <= Constants.MarkerInteractionDistance)
                {
                    CurrentInteractionType = InteractionType.ManageMethLab;
                    Util.DisplayHelpText($"Press {HelpTextKeys.Get(InteractionControl)} to manage the meth lab.");
                }
                else
                {
                    ManagementMenuPool.CloseAllMenus();

                    if (IsLeaning)
                    {
                        CurrentInteractionType = InteractionType.CancelLean;
                        Util.DisplayHelpText($"Press {HelpTextKeys.Get(InteractionControl)} to stop leaning on the rail.");
                    }
                    else
                    {
                        if (Game.Player.Character.IsInArea(Constants.LeanAreaMin, Constants.LeanAreaMax))
                        {
                            CurrentInteractionType = InteractionType.Lean;
                            Util.DisplayHelpText($"Press {HelpTextKeys.Get(InteractionControl)} to lean on the rail.");
                        }
                    }
                }

                // Disable some controls inside the interior
                for (int i = 0; i < Constants.ControlsToDisable.Length; i++) Function.Call(Hash.DISABLE_CONTROL_ACTION, 2, Constants.ControlsToDisable[i], true);
            }
            else
            {
                // Outside, draw lab markers
                for (int i = 0; i < MethLabs.Count; i++)
                {
                    DistanceToInteractable = PlayerPosition.DistanceTo(MethLabs[i].Position);

                    if (DistanceToInteractable <= Constants.MarkerDrawDistance)
                    {
                        World.DrawMarker(MarkerType.VerticalCylinder, MethLabs[i].Position, Vector3.Zero, Vector3.Zero, Constants.MarkerScale, Constants.MarkerColor);

                        if (DistanceToInteractable <= Constants.MarkerInteractionDistance)
                        {
                            InteractableLabIdx = i;

                            if (MethLabs[i].HasFlag(LabFlags.IsOwned))
                            {
                                CurrentInteractionType = InteractionType.EnterMethLab;
                                Util.DisplayHelpText($"Press {HelpTextKeys.Get(InteractionControl)} to enter the meth lab.");
                            }
                            else
                            {
                                CurrentInteractionType = InteractionType.BuyMethLab;
                                Util.DisplayHelpText($"Press {HelpTextKeys.Get(InteractionControl)} to buy the {MethLabs[i].Location} meth lab.~n~Price: ~g~${MethLabs[i].Price:N0}");
                            }

                            break;
                        }
                    }
                }
            }
            #endregion

            // Handle interactions
            #region Interaction Handling
            if (Game.IsControlJustPressed(2, (Control)InteractionControl))
            {
                int labIdx = InteractableLabIdx == -1 ? InsideMethLabIdx : InteractableLabIdx;
                if (labIdx == -1) return;

                Character currentCharacter = Util.GetCharacterFromModel(Game.Player.Character.Model.Hash);
                if (currentCharacter == Character.Unknown)
                {
                    UI.Notify("Only Michael, Franklin and Trevor can interact with meth labs.");
                    return;
                }

                switch (CurrentInteractionType)
                {
                    case InteractionType.BuyMethLab:
                        if (PlayerPosition.DistanceTo(MethLabs[labIdx].Position) > Constants.MarkerInteractionDistance) return;

                        if (Game.Player.Money < MethLabs[labIdx].Price)
                        {
                            UI.Notify("You don't have enough money to buy this meth lab.");
                            return;
                        }

                        MethLabs[labIdx].Owner = currentCharacter;

                        MethLabs[labIdx].LastVisit = Util.GetGameDate();
                        MethLabs[labIdx].Flags = LabFlags.None;
                        MethLabs[labIdx].AddFlag(LabFlags.IsOwned);

                        Game.Player.Money -= MethLabs[InteractableLabIdx].Price;
                        Save();

                        Util.NotifyWithPicture("LJT", $"{currentCharacter}, good call buying the {MethLabs[labIdx].Location} meth lab. Go inside and check the laptop to get started.", "CHAR_LJT", 1);
                        break;

                    case InteractionType.EnterMethLab:
                        if (PlayerPosition.DistanceTo(MethLabs[labIdx].Position) > Constants.MarkerInteractionDistance) return;

                        if (currentCharacter != MethLabs[labIdx].Owner)
                        {
                            UI.Notify($"Only the owner ({MethLabs[labIdx].Owner}) can enter this meth lab.");
                            return;
                        }

                        if (Game.MissionFlag)
                        {
                            UI.Notify("You can't enter meth labs while being in a mission.");
                            return;
                        }

                        if (Game.Player.WantedLevel > 0)
                        {
                            UI.Notify("You can't enter meth labs while being wanted by the police.");
                            return;
                        }

                        PlayerLabModel = Game.Player.Character.Model.Hash;
                        InsideMethLabIdx = labIdx;
                        EnteredMethLab.Invoke(labIdx);

                        Game.Player.Character.Position = Constants.MethLabExit;
                        Game.Player.Character.Heading = Constants.MethLabHeading;
                        Game.Player.Character.Weapons.Select(WeaponHash.Unarmed);
                        break;

                    case InteractionType.ExitMethLab:
                        if (PlayerPosition.DistanceTo(Constants.MethLabExit) > Constants.MarkerInteractionDistance) return;

                        Game.Player.Character.Position = MethLabs[labIdx].Position;

                        LeftMethLab.Invoke(labIdx, LabExitReason.Player);
                        InsideMethLabIdx = -1;
                        break;

                    case InteractionType.ManageMethLab:
                        if (PlayerPosition.DistanceTo(Constants.MethLabLaptop) > Constants.MarkerInteractionDistance) return;

                        ManagementMenuPool.RefreshIndex();
                        ManagementMain.Visible = true;
                        break;

                    case InteractionType.Lean:
                        if (IsLeaning || !Game.Player.Character.IsInArea(Constants.LeanAreaMin, Constants.LeanAreaMax)) return;

                        Util.RequestAnimDict("anim@amb@yacht@rail@standing@male@variant_01@");
                        using (TaskSequence tseq = new TaskSequence())
                        {
                            Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, 0, Constants.LeanPos.X, Constants.LeanPos.Y, Constants.LeanPos.Z, 1.0f, -1, Constants.LeanHeading, 0.0f);
                            tseq.AddTask.PlayAnimation("anim@amb@yacht@rail@standing@male@variant_01@", "enter");
                            tseq.AddTask.PlayAnimation("anim@amb@yacht@rail@standing@male@variant_01@", "base", 8.0f, -1, AnimationFlags.Loop);
                            tseq.Close();

                            Game.Player.Character.Task.PerformSequence(tseq);
                        }

                        IsLeaning = true;
                        break;

                    case InteractionType.CancelLean:
                        if (!IsLeaning) return;

                        Game.Player.Character.Task.PlayAnimation("anim@amb@yacht@rail@standing@male@variant_01@", "exit");
                        IsLeaning = false;
                        break;
                }
            }
            #endregion
        }
        #endregion

        #region Event: Aborted
        public void Script_Aborted(object sender, EventArgs e)
        {
            if (InsideMethLabIdx != -1) LeftMethLab.Invoke(InsideMethLabIdx, LabExitReason.ScriptExit);

            foreach (Entity ent in MethLabEntities) ent?.Delete();
            MethLabEntities.Clear();

            foreach (Lab lab in MethLabs) lab.DestroyEntities();
            MethLabs.Clear();

            ManagementBlip?.Remove();
            ManagementBlip = null;

            ManagementMain = null;
            ManagementMenuPool = null;
        }
        #endregion

        #region ScriptEvent: EnteredMethLab
        public void Script_EnteredMethLab(int labIndex)
        {
            ManagementBlip.Alpha = 255;
            ManagementMain.Clear();

            // Fancy stuff
            LaptopRTID = Util.SetupRenderTarget();
            IsLeaning = false;

            // Audio
            Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "DLC_BIKER/Interior_Meth", false, -1);
            Function.Call(Hash.START_AUDIO_SCENE, "Biker_Warehouses_Meth_Scene");

            // Interior props and menu filling
            for (int i = 0; i < Constants.InteriorProps.Length; i++) Function.Call(Hash._DISABLE_INTERIOR_PROP, LabInteriorID, Constants.InteriorProps[i]);

            if (MethLabs[labIndex].HasFlag(LabFlags.HasDoneSetup))
            {
                int labProdTime = ProductionTime;

                UIMenuItem upgradesMenuItem = new UIMenuItem("Upgrades", string.Empty);
                ManagementMain.BindMenuToItem(UpgradesMenu, upgradesMenuItem);
                ManagementMain.AddItem(upgradesMenuItem);

                Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_setup");
                Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_production");

                // Equipment upgrade item
                if (MethLabs[labIndex].HasFlag(LabFlags.HasEquipmentUpgrade))
                {
                    labProdTime -= 1;

                    Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_upgrade");
                    UpgradesMenu.MenuItems[0].SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                    UpgradesMenu.MenuItems[0].Enabled = false;
                }
                else
                {
                    Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_basic");
                    UpgradesMenu.MenuItems[0].SetLeftBadge(UIMenuItem.BadgeStyle.None);
                    UpgradesMenu.MenuItems[0].Enabled = true;
                }

                // Staff upgrade item
                if (MethLabs[labIndex].HasFlag(LabFlags.HasStaffUpgrade))
                {
                    labProdTime -= 1;

                    UpgradesMenu.MenuItems[1].SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                    UpgradesMenu.MenuItems[1].Enabled = false;
                }
                else
                {
                    UpgradesMenu.MenuItems[1].SetLeftBadge(UIMenuItem.BadgeStyle.None);
                    UpgradesMenu.MenuItems[1].Enabled = true;
                }

                // Security upgrade item
                if (MethLabs[labIndex].HasFlag(LabFlags.HasSecurityUpgrade))
                {
                    Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_security_high");
                    MethLabEntities.Add(Util.CreateSecurityPed());

                    UpgradesMenu.MenuItems[2].SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                    UpgradesMenu.MenuItems[2].Enabled = false;
                }
                else
                {
                    UpgradesMenu.MenuItems[2].SetLeftBadge(UIMenuItem.BadgeStyle.None);
                    UpgradesMenu.MenuItems[2].Enabled = true;
                }

                TimeSpan diff = (Util.GetGameDate() - MethLabs[labIndex].LastVisit).Duration();
                int produced = (int)Math.Floor(diff.TotalHours / labProdTime);

                MethLabs[labIndex].LastVisit = Util.GetGameDate();
                MethLabs[labIndex].Product += produced;
                Save();

                UIMenuItem sellStockItem = new UIMenuItem("Sell Stock", $"Sell the produced meth. ({MethLabs[labIndex].Product} bins)");
                SaleConfirmationMenu.MenuItems[0].SetRightLabel($"${MethLabs[labIndex].ProductValue:N0}");
                ManagementMain.BindMenuToItem(SaleConfirmationMenu, sellStockItem);
                ManagementMain.AddItem(sellStockItem);

                // Add lab workers
                for (int i = 0; i < Constants.LabWorkerPositions.Length; i++)
                {
                    Ped worker = World.CreatePed(PedHash.MethMale01, Constants.LabWorkerPositions[i].Item1, Constants.LabWorkerPositions[i].Item2);
                    worker.AlwaysKeepTask = true;
                    worker.BlockPermanentEvents = true;
                    worker.CanRagdoll = false;
                    worker.SetDefaultClothes();

                    Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, worker.Handle, "WORLD_HUMAN_CLIPBOARD", -1, false);
                    MethLabEntities.Add(worker);
                }

                // Add product props
                for (int i = 0; i < MethLabs[labIndex].Product; i++) MethLabEntities.Add(World.CreateProp("bkr_prop_meth_bigbag_01a", Constants.MethBoxes[i].Item1, new Vector3(0f, 0f, Constants.MethBoxes[i].Item2), false, false));

                // Lab ambient audio
                Function.Call(Hash.SET_AMBIENT_ZONE_STATE, MethLabs[labIndex].AmbientZoneName, true, true);
            }
            else
            {
                Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_empty");
                ManagementMain.AddItem(new UIMenuItem("Set Up", "Prepare the lab for production."));
            }

            Function.Call(Hash.REFRESH_INTERIOR, LabInteriorID);

            UIMenuItem sellLabItem = new UIMenuItem("Sell Lab", "Sell the meth lab. Will take you to the confirmation menu.");
            LabSaleConfirmationMenu.MenuItems[0].SetRightLabel($"${Math.Round(MethLabs[labIndex].Price * 0.8):N0}");
            ManagementMain.BindMenuToItem(LabSaleConfirmationMenu, sellLabItem);
            ManagementMain.AddItem(sellLabItem);

            ManagementMenuPool.RefreshIndex();
        }
        #endregion

        #region ScriptEvent: LeftMethLab
        public void Script_LeftMethLab(int labIndex, LabExitReason reason)
        {
            ManagementBlip.Alpha = 0;
            ManagementMenuPool.CloseAllMenus();

            Util.ReleaseRenderTarget();
            Function.Call(Hash.SET_AMBIENT_ZONE_STATE, MethLabs[labIndex].AmbientZoneName, false, true);
            Function.Call(Hash.STOP_AUDIO_SCENE, "Biker_Warehouses_Meth_Scene");
            Function.Call(Hash.RELEASE_NAMED_SCRIPT_AUDIO_BANK, "DLC_BIKER/Interior_Meth");

            if (reason == LabExitReason.CharacterChange || reason == LabExitReason.ScriptExit)
            {
                Game.Player.Character.Position = MethLabs[labIndex].Position;
            }

            LaptopRTID = -1;
            IsLeaning = false;

            foreach (Entity ent in MethLabEntities) ent?.Delete();
            MethLabEntities.Clear();
        }
        #endregion

        #region MenuEvent: ManagementMain_ItemSelected
        public void ManagementMain_ItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
        {
            if (InsideMethLabIdx == -1) return;

            switch (selectedItem.Text)
            {
                case "Set Up":
                    if (MethLabs[InsideMethLabIdx].HasFlag(LabFlags.HasDoneSetup))
                    {
                        UI.Notify("Set up mission is already done for this meth lab.");
                        return;
                    }

                    int truckPosIdx = Constants.RandomGenerator.Next(0, Constants.SetupVehicleSpawns.Length);
                    Mission.Start(
                        MissionType.Setup,
                        InsideMethLabIdx,

                        new List<Vector3>
                        {
                            Constants.SetupVehicleSpawns[truckPosIdx].Item1,
                            MethLabs[InsideMethLabIdx].DeliveryPosition
                        },

                        Constants.SetupVehicleSpawns[truckPosIdx].Item2,
                        MissionTime * 60 * 1000
                    );

                    Game.Player.Character.Position = MethLabs[InsideMethLabIdx].Position;

                    LeftMethLab.Invoke(InsideMethLabIdx, LabExitReason.Mission);
                    InsideMethLabIdx = -1;
                    break;
            }
        }
        #endregion

        #region MenuEvent: UpgradesMenu_ItemSelected
        public void UpgradesMenu_ItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
        {
            if (InsideMethLabIdx == -1) return;

            switch (index)
            {
                case 0:
                    if (Game.Player.Money < EquipmentUpgradePrice)
                    {
                        UI.Notify("You don't have enough money to buy the equipment upgrade.");
                        return;
                    }

                    if (MethLabs[InsideMethLabIdx].HasFlag(LabFlags.HasEquipmentUpgrade))
                    {
                        UI.Notify("This meth lab already has the equipment upgrade.");
                        return;
                    }

                    MethLabs[InsideMethLabIdx].AddFlag(LabFlags.HasEquipmentUpgrade);
                    Save();

                    Function.Call(Hash._DISABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_basic");
                    Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_upgrade");
                    Function.Call(Hash.REFRESH_INTERIOR, LabInteriorID);

                    Function.Call(Hash.SET_AMBIENT_ZONE_STATE, "AZ_DLC_Biker_Meth_Warehouse_Normal", false, true);
                    Function.Call(Hash.SET_AMBIENT_ZONE_STATE, "AZ_DLC_Biker_Meth_Warehouse_Upgraded", true, true);

                    selectedItem.SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                    selectedItem.Enabled = false;

                    Game.Player.Money -= EquipmentUpgradePrice;
                    Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(Game.Player.Character.Model.Hash)}, good call getting the equipment upgrade. This will surely boost the lab's performance.", "CHAR_LJT", 1);
                    break;

                case 1:
                    if (Game.Player.Money < StaffUpgradePrice)
                    {
                        UI.Notify("You don't have enough money to buy the staff upgrade.");
                        return;
                    }

                    if (MethLabs[InsideMethLabIdx].HasFlag(LabFlags.HasStaffUpgrade))
                    {
                        UI.Notify("This meth lab already has the staff upgrade.");
                        return;
                    }

                    MethLabs[InsideMethLabIdx].AddFlag(LabFlags.HasStaffUpgrade);
                    Save();

                    selectedItem.SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                    selectedItem.Enabled = false;

                    Game.Player.Money -= StaffUpgradePrice;
                    Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(Game.Player.Character.Model.Hash)}, good call getting the staff upgrade. This will surely boost the lab's performance.", "CHAR_LJT", 1);
                    break;

                case 2:
                    if (Game.Player.Money < SecurityUpgradePrice)
                    {
                        UI.Notify("You don't have enough money to buy the security upgrade.");
                        return;
                    }

                    if (MethLabs[InsideMethLabIdx].HasFlag(LabFlags.HasSecurityUpgrade))
                    {
                        UI.Notify("This meth lab already has the security upgrade.");
                        return;
                    }

                    MethLabs[InsideMethLabIdx].AddFlag(LabFlags.HasSecurityUpgrade);
                    Save();

                    MethLabEntities.Add(Util.CreateSecurityPed());
                    Function.Call(Hash._ENABLE_INTERIOR_PROP, LabInteriorID, "meth_lab_security_high");
                    Function.Call(Hash.REFRESH_INTERIOR, LabInteriorID);

                    selectedItem.SetLeftBadge(UIMenuItem.BadgeStyle.Tick);
                    selectedItem.Enabled = false;

                    Game.Player.Money -= SecurityUpgradePrice;
                    Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(Game.Player.Character.Model.Hash)}, good call getting the security upgrade. This will surely set the workers with doubts straight.", "CHAR_LJT", 1);
                    break;
            }
        }

        public void SaleConfirmationMenu_ItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
        {
            if (InsideMethLabIdx == -1) return;

            if (index == 0)
            {
                if (MethLabs[InsideMethLabIdx].Product < 1)
                {
                    UI.Notify("This lab doesn't have any product.");
                    return;
                }

                Vector3 position = MethLabs[InsideMethLabIdx].Position;
                int zoneHash = Function.Call<int>(Hash.GET_HASH_OF_MAP_AREA_AT_COORDS, position.X, position.Y, position.Z);

                if (zoneHash == Game.GenerateHash("city"))
                {
                    position = Constants.CountrysideDeliveryPositions[ Constants.RandomGenerator.Next(0, Constants.CountrysideDeliveryPositions.Length) ];
                }
                else
                {
                    position = Constants.CityDeliveryPositions[ Constants.RandomGenerator.Next(0, Constants.CityDeliveryPositions.Length) ];
                }

                Mission.Start(
                    MissionType.Delivery,
                    InsideMethLabIdx,

                    new List<Vector3>
                    {
                        MethLabs[InsideMethLabIdx].DeliveryPosition,
                        position
                    },

                    0.0f,
                    MissionTime * 60 * 1000,
                    MethLabs[InsideMethLabIdx].Product
                );

                Game.Player.Character.Position = MethLabs[InsideMethLabIdx].Position;

                int chance = Constants.RandomGenerator.Next(0, 100);
                int labChance = MethLabs[InsideMethLabIdx].HasFlag(LabFlags.HasSecurityUpgrade) ? (int)Math.Floor(PoliceChance / 2.0) : PoliceChance;
                if (chance <= labChance)
                {
                    Game.Player.WantedLevel += PoliceStars;
                    Util.NotifyWithPicture("LJT", $"{Util.GetCharacterFromModel(Game.Player.Character.Model.Hash)}, the cops know about the delivery!", "CHAR_LJT", 1);
                }

                LeftMethLab.Invoke(InsideMethLabIdx, LabExitReason.Mission);
                InsideMethLabIdx = -1;
            }
            else
            {
                menu.GoBack();
            }
        }

        public void LabSaleConfirmationMenu_ItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
        {
            if (InsideMethLabIdx == -1) return;

            if (index == 0)
            {
                int saleValue = (int)Math.Round(MethLabs[InsideMethLabIdx].Price * 0.8);
                Game.Player.Money += saleValue;

                Util.NotifyWithPicture("LJT", $"The meth lab has been sold for ${saleValue:N0}.", "CHAR_LJT", 1);
                MethLabs[InsideMethLabIdx].Reset();
                Save();

                Game.Player.Character.Position = MethLabs[InsideMethLabIdx].Position;
            }
            else
            {
                menu.GoBack();
            }
        }
        #endregion
    }
}
