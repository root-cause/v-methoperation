using System;
using GTA;
using GTA.Math;
using GTA.Native;
using MethOperation.Enums;

namespace MethOperation
{
    public static class Util
    {
        public static void DisplayHelpText(string message)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
            Function.Call(Hash._0x238FFE5C7B0498A6, 0, 0, 1, -1);
        }

        // Credits to SHVDN devs
        public static void NotifyWithPicture(string sender, string message, string picName, int icon, bool playSound = true)
        {
            Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "CELL_EMAIL_BCON");

            for (int i = 0; i < message.Length; i += Constants.MaxStringLength)
            {
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message.Substring(i, System.Math.Min(Constants.MaxStringLength, message.Length - i)));
            }

            Function.Call(Hash._SET_NOTIFICATION_MESSAGE, picName, picName, false, icon, sender, string.Empty);
            Function.Call(Hash._DRAW_NOTIFICATION, false, true);

            if (playSound)
            {
                Character currentCharacter = GetCharacterFromModel(Game.Player.Character.Model.Hash);
                Game.PlaySound("Text_Arrive_Tone", $"Phone_SoundSet_{(currentCharacter == Character.Unknown ? "Default" : currentCharacter.ToString())}");
            }
        }

        public static Character GetCharacterFromModel(int modelHash)
        {
            switch ((PedHash)modelHash)
            {
                case PedHash.Michael:
                    return Character.Michael;

                case PedHash.Franklin:
                    return Character.Franklin;

                case PedHash.Trevor:
                    return Character.Trevor;

                default:
                    return Character.Unknown;
            }
        }

        public static BlipColor GetCharacterBlipColor(Character character)
        {
            switch (character)
            {
                case Character.Michael:
                    return (BlipColor)42;

                case Character.Franklin:
                    return (BlipColor)43;

                case Character.Trevor:
                    return (BlipColor)44;

                default:
                    return BlipColor.White;
            }
        }

        public static DateTime GetGameDate()
        {
            return new DateTime(
                Function.Call<int>(Hash.GET_CLOCK_YEAR),
                Function.Call<int>(Hash.GET_CLOCK_MONTH) + 1,
                Function.Call<int>(Hash.GET_CLOCK_DAY_OF_MONTH),
                Function.Call<int>(Hash.GET_CLOCK_HOURS),
                Function.Call<int>(Hash.GET_CLOCK_MINUTES),
                Function.Call<int>(Hash.GET_CLOCK_SECONDS)
            );
        }

        public static Ped CreateSecurityPed()
        {
            Ped security = World.CreatePed(PedHash.MethMale01, new Vector3(997.04f, -3199.17f, -36.39f), 270.0f);
            security.AlwaysKeepTask = true;
            security.BlockPermanentEvents = true;
            security.CanRagdoll = false;
            security.SetDefaultClothes();

            int handle = security.Handle;
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, handle, 0, 2, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, handle, 2, 2, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, handle, 3, 2, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, handle, 4, 1, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, handle, 8, 2, 0);
            Function.Call(Hash.SET_PED_PROP_INDEX, handle, 1, 1, 0, true);

            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, handle, "WORLD_HUMAN_GUARD_STAND", -1, false);
            return security;
        }
    }
}
