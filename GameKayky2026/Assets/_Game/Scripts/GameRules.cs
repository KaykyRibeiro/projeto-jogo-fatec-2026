using System;
using UnityEngine;

namespace ValeDasReliquias
{
    public enum ItemKind { Coin, Diamond, Teapot, Key, Health, Bomb }
    public enum ScreenMode { Menu, Playing, Paused, Options, Defeat, Victory }

    [Serializable]
    public class CollectionGoals
    {
        [Min(0)] public int coins = 2;
        [Min(0)] public int diamonds = 2;
        [Min(0)] public int teapots = 2;
        public int Coins { get; private set; }
        public int Diamonds { get; private set; }
        public int Teapots { get; private set; }
        public bool Complete => Coins >= coins && Diamonds >= diamonds && Teapots >= teapots;
        public void Reset() { Coins = Diamonds = Teapots = 0; }
        public bool TryCollect(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Coin: if (Coins >= coins) return false; Coins++; return true;
                case ItemKind.Diamond: if (Diamonds >= diamonds) return false; Diamonds++; return true;
                case ItemKind.Teapot: if (Teapots >= teapots) return false; Teapots++; return true;
                default: return false;
            }
        }
    }
}
