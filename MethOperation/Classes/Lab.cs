using System;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using MethOperation.Enums;

namespace MethOperation.Classes
{
    [Serializable]
    public class Lab
    {
        #region Saved Properties
        public int Price { get; set; } = 0;
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 DeliveryPosition { get; set; } = Vector3.Zero;

        public Character Owner
        {
            get { return _owner; }

            set
            {
                _owner = value;

                if (_blip != null)
                {
                    _blip.Color = Util.GetCharacterBlipColor(value);
                    _blip.Name = value == Character.Unknown ? "Meth Lab" : $"Meth Lab ({value})";
                }
            }
        }

        public int Product
        {
            get { return _product; }

            set
            {
                _product = (value < 0) ? 0 : (value > Constants.MethBoxes.Length) ? Constants.MethBoxes.Length : value;
            }
        }

        public DateTime LastVisit { get; set; } = new DateTime(1970, 01, 01);
        public LabFlags Flags { get; set; } = LabFlags.None;
        #endregion

        #region Properties
        [XmlIgnore]
        public string Location { get; private set; }

        [XmlIgnore]
        public int ProductValue => Product * Main.ProductValue;

        [XmlIgnore]
        public string AmbientZoneName => HasFlag(LabFlags.HasEquipmentUpgrade) ? "AZ_DLC_Biker_Meth_Warehouse_Upgraded" : "AZ_DLC_Biker_Meth_Warehouse_Normal";

        private int _product = 0;
        private Character _owner = Character.Unknown;
        private Blip _blip = null;
        #endregion

        #region Methods
        public void CreateEntities()
        {
            Location = World.GetZoneName(Position);

            if (_blip == null)
            {
                _blip = World.CreateBlip(Position);
                _blip.Sprite = BlipSprite.Meth;
                _blip.IsShortRange = true;
                _blip.Color = Util.GetCharacterBlipColor(Owner);
                _blip.Name = _owner == Character.Unknown ? "Meth Lab" : $"Meth Lab ({_owner})";
            }
        }

        public void SetBlipVisible(bool visible)
        {
            if (_blip != null) _blip.Alpha = visible ? 255 : 0;
        }

        public void DestroyEntities()
        {
            _blip?.Remove();
        }

        public void AddFlag(LabFlags flag)
        {
            Flags |= flag;
        }

        public bool HasFlag(LabFlags flag)
        {
            return (Flags & flag) == flag;
        }

        public void RemoveFlag(LabFlags flag)
        {
            Flags &= ~flag;
        }

        public void Reset()
        {
            Owner = Character.Unknown;
            Product = 0;
            Flags = LabFlags.None;
        }
        #endregion
    }
}
